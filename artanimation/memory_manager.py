"""
Memory management system for ASCII art generator.
Provides utilities to optimize memory usage in frame generation.
"""

import numpy as np
import torch
import cv2
import os
from collections import defaultdict

# Global cache for masks
MASK_CACHE = {}
FRAME_SIMILARITY_THRESHOLD = 0.95  # 95% similarity threshold for frame reuse
MASK_CACHE_SIZE_LIMIT = 20  # Maximum number of masks to keep in cache

# Global flag for memory pressure
MEMORY_PRESSURE = False

def get_cached_mask(frame_index, total_frames):
    """
    Try to get a cached mask for a frame similar to the current one
    Args:
        frame_index: Current frame index
        total_frames: Total number of frames
    Returns:
        Cached mask if available, else None
    """
    global MASK_CACHE
    
    if len(MASK_CACHE) == 0:
        return None
    
    # Calculate frames that are close to our target frame
    target_progress = frame_index / total_frames
    
    # Find the closest cached frame by progress ratio
    closest_frame = None
    closest_distance = float('inf')
    
    for cached_index in MASK_CACHE:
        cached_progress = cached_index / total_frames
        distance = abs(cached_progress - target_progress)
        
        if distance < closest_distance:
            closest_distance = distance
            closest_frame = cached_index
    
    # Only use cache if it's close enough
    if closest_distance < 0.03:  # Within 3% progress
        return MASK_CACHE[closest_frame].copy()
    
    return None

def cache_mask(frame_index, mask, total_frames):
    """
    Cache a mask for future use
    Args:
        frame_index: Current frame index
        mask: The mask to cache
        total_frames: Total number of frames
    """
    global MASK_CACHE
    
    # Only cache if we haven't exceeded the limit
    if len(MASK_CACHE) >= MASK_CACHE_SIZE_LIMIT:
        # If cache is full, remove the least useful mask
        # Strategy: keep evenly spaced masks across the timeline
        if frame_index in MASK_CACHE:
            return  # Already cached this frame
            
        # Calculate how many frames to keep in each segment
        frames_per_segment = total_frames / MASK_CACHE_SIZE_LIMIT
        
        # Keep masks that are at segment boundaries
        to_keep = set([int(i * frames_per_segment) for i in range(MASK_CACHE_SIZE_LIMIT)])
        
        # If this frame is at a segment boundary, remove furthest frame from any boundary
        if frame_index in to_keep:
            max_distance = 0
            frame_to_remove = None
            
            for cached_frame in MASK_CACHE:
                # Find distance to closest segment boundary
                distances = [abs(cached_frame - boundary) for boundary in to_keep]
                min_distance = min(distances)
                
                if min_distance > max_distance:
                    max_distance = min_distance
                    frame_to_remove = cached_frame
            
            if frame_to_remove is not None:
                del MASK_CACHE[frame_to_remove]
        else:
            # Not a special frame, don't cache
            return
    
    # Cache the mask
    MASK_CACHE[frame_index] = mask.copy()

def clear_cache():
    """Clear all caches to free memory"""
    global MASK_CACHE
    MASK_CACHE.clear()
    
    # Force GPU memory release
    if torch.cuda.is_available():
        torch.cuda.empty_cache()

def signal_memory_pressure():
    """Signal that memory is under pressure"""
    global MEMORY_PRESSURE
    MEMORY_PRESSURE = True
    
    # Immediately reduce cache size
    if len(MASK_CACHE) > MASK_CACHE_SIZE_LIMIT // 2:
        # Keep only evenly spaced items
        keys = sorted(list(MASK_CACHE.keys()))
        to_keep = keys[::2]  # Keep every other key
        
        for k in list(MASK_CACHE.keys()):
            if k not in to_keep:
                del MASK_CACHE[k]
                
    # Force GPU memory release
    if torch.cuda.is_available():
        torch.cuda.empty_cache()

def reset_memory_pressure():
    """Reset memory pressure flag"""
    global MEMORY_PRESSURE
    MEMORY_PRESSURE = False
