"""
prerender_optimizer.py

This module provides advanced optimization techniques for ASCII art animations
by pre-rendering characters into a texture atlas and using GPU-accelerated composition.
"""

import numpy as np
import cv2
import torch
import os
from PIL import Image, ImageDraw, ImageFont

# Cache for rendered characters - global to persist between frames
CHAR_CACHE = {}
MEMORY_PRESSURE = False  # Flag to indicate memory pressure

class CharacterAtlas:
    """
    Creates and manages a pre-rendered atlas of ASCII characters for fast rendering.
    This significantly speeds up frame generation by avoiding repeated text rendering calls.
    """
    
    def __init__(self, characters, font_face=cv2.FONT_HERSHEY_SIMPLEX, font_scale=1.0, 
                 font_thickness=1, char_size=32, device='cuda'):
        """Initialize a character atlas for fast rendering."""
        self.characters = characters
        self.char_size = char_size
        self.device = device
        self.font_face = font_face
        self.font_scale = font_scale
        self.font_thickness = font_thickness
        
        # Create character map for quick lookups
        self.char_map = {char: idx for idx, char in enumerate(characters)}
        
        # Generate the texture atlas
        self._generate_atlas()
        
        # Move atlas to GPU if available
        if torch.cuda.is_available() and device == 'cuda':
            self.atlas_tensor = torch.tensor(self.atlas, device=device)
        else:
            self.atlas_tensor = torch.tensor(self.atlas)
            
    def _generate_atlas(self):
        """Generate texture atlas with all characters."""
        # Create a blank atlas image
        atlas_width = self.char_size * len(self.characters)
        self.atlas = np.zeros((self.char_size, atlas_width, 4), dtype=np.uint8)
        
        # Render each character into the atlas
        for idx, char in enumerate(self.characters):
            # Create a blank image for this character
            char_img = np.zeros((self.char_size, self.char_size, 4), dtype=np.uint8)
            
            # Calculate text position to center in the cell
            (text_width, text_height), baseline = cv2.getTextSize(
                char, self.font_face, self.font_scale, self.font_thickness)
            
            text_x = (self.char_size - text_width) // 2
            text_y = (self.char_size + text_height) // 2
            
            # Render the character in white
            cv2.putText(
                char_img, char, 
                (text_x, text_y), 
                self.font_face, self.font_scale, 
                (255, 255, 255, 255), self.font_thickness, 
                cv2.LINE_AA
            )
            
            # Copy to atlas
            x_offset = idx * self.char_size
            self.atlas[:, x_offset:x_offset+self.char_size] = char_img
    
    def render_text_fast(self, text, img_shape, positions, colors=None):
        """
        Render text characters at given positions using the pre-rendered atlas.
        
        Args:
            text: String of characters to render
            img_shape: Tuple (height, width, channels) for output image
            positions: List of (x, y) positions for each character
            colors: List of (r, g, b) colors for each character, or None for white
        
        Returns:
            Rendered image as numpy array
        """
        # Create output image
        output = np.zeros(img_shape, dtype=np.uint8)
        
        # Default color if none provided
        if colors is None:
            colors = [(255, 255, 255)] * len(text)
            
        # For each character in the text
        for i, char in enumerate(text):
            if char == ' ' or char not in self.char_map:
                continue
                
            # Get character position in the atlas
            atlas_idx = self.char_map[char]
            x_offset = atlas_idx * self.char_size
            
            # Get position where to render
            x, y = positions[i]
            
            # Calculate source and destination rectangles
            src_rect = self.atlas[:, x_offset:x_offset+self.char_size]
            
            # Create a colored version of the character
            colored_char = np.zeros((self.char_size, self.char_size, 3), dtype=np.uint8)
            for c in range(3):
                colored_char[:,:,c] = src_rect[:,:,0] * colors[i][c] // 255
            
            # Calculate target position
            y1 = max(0, y)
            y2 = min(img_shape[0], y + self.char_size)
            x1 = max(0, x)
            x2 = min(img_shape[1], x + self.char_size)
            
            # Calculate source position based on clipping
            src_y1 = 0 if y >= 0 else -y
            src_y2 = self.char_size if y2 - y == self.char_size else y2 - y
            src_x1 = 0 if x >= 0 else -x
            src_x2 = self.char_size if x2 - x == self.char_size else x2 - x
            
            # Copy to output with alpha blending
            alpha = src_rect[src_y1:src_y2, src_x1:src_x2, 3] / 255.0
            for c in range(3):
                output[y1:y2, x1:x2, c] = (
                    output[y1:y2, x1:x2, c] * (1 - alpha) + 
                    colored_char[src_y1:src_y2, src_x1:src_x2, c] * alpha
                ).astype(np.uint8)
        
        return output

def optimize_ascii_frame_generation(ascii_str, char_mask, font_scale, img_width, img_height, 
                                   background_color, use_color=True, color_data=None):
    """
    Generate a frame using pre-rendered character optimization.
    
    Args:
        ascii_str: The ASCII art string
        char_mask: Dictionary of character indices to display
        font_scale: Font scale factor
        img_width: Width of output image
        img_height: Height of output image
        background_color: Background color tuple (r,g,b)
        use_color: Whether to use color from the original image
        color_data: Character colors if use_color=True
    
    Returns:
        Rendered image as numpy array
    """
    global CHAR_CACHE, MEMORY_PRESSURE
    
    # Initialize the image
    img_np = np.zeros((img_height, img_width, 3), dtype=np.uint8)
    img_np[:,:] = background_color  # Fill the background
    
    # Get dimensions
    lines = ascii_str.split('\n')
    rows = len(lines)
    cols = max(len(line) for line in lines)
    
    # Calculate character size to fill the screen
    char_width = img_width / cols
    char_height = img_height / rows
    
    # Configure OpenCV font and scale parameters
    font_face = cv2.FONT_HERSHEY_SIMPLEX
    thickness = max(1, int(font_scale * 1.5))
    line_type = cv2.LINE_AA
    
    # Adaptive memory management - reduce cache if under pressure
    if MEMORY_PRESSURE and len(CHAR_CACHE) > 50:
        # Keep only most common characters under memory pressure
        # Count character frequencies
        char_freq = {}
        for char in ascii_str:
            if char != '\n':
                char_freq[char] = char_freq.get(char, 0) + 1
                
        # Sort by frequency and keep top characters
        most_common = sorted(char_freq.items(), key=lambda x: x[1], reverse=True)[:40]
        common_chars = set(char for char, _ in most_common)
        
        # Remove less used characters from cache
        for char in list(CHAR_CACHE.keys()):
            if char not in common_chars:
                del CHAR_CACHE[char]
        
        # Free GPU memory
        torch.cuda.empty_cache()
        MEMORY_PRESSURE = False
    
    # Build the cache of pre-rendered characters (if not already built)
    unique_chars = set(char for i, char in enumerate(ascii_str) if char != '\n' and i in char_mask)
    for char in unique_chars:
        if char not in CHAR_CACHE:
            # Render this character once at the appropriate size
            text_size, baseline = cv2.getTextSize(char, font_face, font_scale, thickness)
            char_img = np.zeros((text_size[1] + baseline + 4, text_size[0] + 4, 4), dtype=np.uint8)
            
            # Draw the character onto a transparent background
            cv2.putText(char_img, char, 
                       (2, text_size[1] + 2),  # Position with small margin
                       font_face, font_scale, (255, 255, 255, 255), thickness, line_type)
            
            # Store in cache with alpha channel
            CHAR_CACHE[char] = {
                'image': char_img,
                'size': (text_size[0], text_size[1]),
                'baseline': baseline
            }
    
    # Draw characters from the cache
    char_index = 0
    line_num = 0
    
    for line in lines:
        x_pos = 0
        for char in line:
            if char_index in char_mask:
                if char in CHAR_CACHE:
                    # Get cached character data
                    char_data = CHAR_CACHE[char]
                    char_img = char_data['image'].copy()
                      # Apply the correct color
                    if use_color and color_data is not None:
                        # Calculate color index more efficiently
                        # Build this mapping once for better performance
                        if not hasattr(optimize_ascii_frame_generation, 'color_mapping'):
                            optimize_ascii_frame_generation.color_mapping = {}
                            non_newline_count = 0
                            
                            for idx, ch in enumerate(ascii_str):
                                if ch != '\n':
                                    optimize_ascii_frame_generation.color_mapping[idx] = non_newline_count
                                    non_newline_count += 1
                        
                        # Use the pre-calculated mapping
                        if char_index in optimize_ascii_frame_generation.color_mapping:
                            color_idx = optimize_ascii_frame_generation.color_mapping[char_index]
                            if 0 <= color_idx < len(color_data):
                                color = color_data[color_idx]
                            else:
                                color = (255, 255, 255)  # Default white
                        else:
                            color = (255, 255, 255)  # Default white
                    else:
                        color = (255, 255, 255)  # Default white
                    
                    # Apply color to the alpha channel of the character
                    for c in range(3):  # RGB channels
                        char_img[:,:,c] = char_img[:,:,3] * color[c] // 255
                    
                    # Calculate position with proper centering
                    text_width, text_height = char_data['size']
                    target_x = int(x_pos + (char_width - text_width) / 2)
                    target_y = int(line_num * char_height + (char_height - text_height) / 2)
                    
                    # Place the character onto the image using alpha blending
                    try:
                        # Ensure we're not going outside the image boundaries
                        roi_height, roi_width = char_img.shape[:2]
                        if (target_y >= 0 and target_x >= 0 and 
                            target_y + roi_height <= img_height and 
                            target_x + roi_width <= img_width):
                            
                            # Get region of interest in the output image
                            roi = img_np[target_y:target_y+roi_height, target_x:target_x+roi_width]
                            
                            # Create alpha mask
                            alpha = char_img[:,:,3].astype(float) / 255.0
                            alpha = np.repeat(alpha[:,:,np.newaxis], 3, axis=2)
                            
                            # Blend using alpha
                            blended = ((1-alpha) * roi + alpha * char_img[:,:,:3]).astype(np.uint8)
                            img_np[target_y:target_y+roi_height, target_x:target_x+roi_width] = blended
                    except Exception as e:
                        # If any error in blending, fallback to direct rendering
                        cv2.putText(img_np, char, 
                                  (int(x_pos + char_width/2), int((line_num + 0.8) * char_height)),
                                  font_face, font_scale, color, thickness, line_type)
            x_pos += char_width
            char_index += 1
        
        line_num += 1
        char_index += 1  # for newline
    
    return img_np

# Additional helper functions
def create_character_lookup_table(ascii_chars, size=32, font_face=cv2.FONT_HERSHEY_SIMPLEX):
    """
    Pre-compute a lookup table of rendered characters for even faster rendering.
    
    Args:
        ascii_chars: String of characters to include in the lookup
        size: Size of each character cell
        font_face: OpenCV font to use
    
    Returns:
        Dictionary mapping characters to pre-rendered images
    """
    char_images = {}
    font_scale = size / 32.0  # Scale based on size
    thickness = max(1, int(font_scale * 1.5))
    
    for char in ascii_chars:
        if char == '\n':
            continue
            
        # Create blank image for character
        char_img = np.zeros((size, size, 4), dtype=np.uint8)
        
        # Calculate position to center text
        (text_width, text_height), baseline = cv2.getTextSize(
            char, font_face, font_scale, thickness)
            
        text_x = (size - text_width) // 2
        text_y = (size + text_height) // 2
        
        # Render character
        cv2.putText(
            char_img, char,
            (text_x, text_y),
            font_face, font_scale,
            (255, 255, 255, 255), thickness,
            cv2.LINE_AA
        )
        
        char_images[char] = char_img
        
    return char_images

def signal_memory_pressure():
    """Signal that the system is under memory pressure and should clean up cache"""
    global MEMORY_PRESSURE
    MEMORY_PRESSURE = True
    
    # If we're in extreme memory pressure, clear the entire cache immediately
    if torch.cuda.is_available():
        memory_info = torch.cuda.memory_stats()
        if memory_info.get('allocated_bytes.all.current', 0) > 6 * (1024**3):  # Over 6GB used
            clear_cache()
            torch.cuda.empty_cache()

def clear_cache():
    """Clear the character cache to free memory"""
    global CHAR_CACHE
    CHAR_CACHE.clear()
