"""
ascii_generator.py

Generates trippy ASCII art from an image and creates a TikTok-friendly animated video.
CUDA-accelerated version for NVIDIA GPUs.
"""

import os
from PIL import Image, ImageDraw, ImageFont
import numpy as np
import cv2
import shutil
import random
import math
from datetime import datetime, timedelta
import argparse
import warnings
from tqdm import tqdm

# For CUDA acceleration - force GPU usage for RTX 4060 with 8GB VRAM
import torch
import torch.cuda
import torchvision
import torchvision.transforms as transforms

# Force CUDA usage - we know the GPU is available
CUDA_AVAILABLE = True
DEVICE = torch.device("cuda:0")

# Configure GPU for maximum performance
torch.backends.cudnn.benchmark = True
torch.backends.cuda.matmul.allow_tf32 = True  # Enable TensorFloat-32 for RTX 40 series
torch.backends.cudnn.allow_tf32 = True  # Enable TF32 for cuDNN

# Set up automatic mixed precision for faster processing
amp_enabled = hasattr(torch, 'amp') and hasattr(torch.amp, 'autocast')

# Get GPU memory info
gpu_props = torch.cuda.get_device_properties(DEVICE)
print(f"CUDA is available. Using GPU: {torch.cuda.get_device_name(0)}")
print(f"GPU Memory: {gpu_props.total_memory / (1024**3):.2f} GB")
print(f"CUDA Capability: {gpu_props.major}.{gpu_props.minor}")

# Use 95% of available VRAM to maximize performance on 8GB RTX 4060
torch.cuda.set_per_process_memory_fraction(0.95)

# For color clustering
try:
    from sklearn.cluster import KMeans
except ImportError:
    print("Warning: scikit-learn not found. Installing...")
    import subprocess
    subprocess.check_call(['pip', 'install', 'scikit-learn'])
    from sklearn.cluster import KMeans

# ================= CONFIGURATION =================

# Import the prerender optimizer when available
try:
    from prerender_optimizer import optimize_ascii_frame_generation, signal_memory_pressure, clear_cache
    PRERENDER_AVAILABLE = True
except ImportError:
    PRERENDER_AVAILABLE = False

# Import memory management system
try:
    import memory_manager as mm
    MEMORY_MANAGER_AVAILABLE = True
except ImportError:
    MEMORY_MANAGER_AVAILABLE = False

INPUT_DIR = "input_images"
OUTPUT_FRAMES_DIR = "output_frames"
FINAL_VIDEO_PATH = "ascii_video.mp4"

# TikTok video dimensions (9:16 aspect ratio)
TIKTOK_RESOLUTION = (1080, 1920)  # Standard TikTok resolution (width, height)

ASCII_CHARS = "@#S%?*+;:,.01$€¢¥£§©®™°•·…±÷×≠√∞∆∫≈≤≥≡≠∑∏∂∇∈∉∋∌∍∎∏∐∑∓∔⊕⊖⊗⊘⊙⊚⊛⊜⊝⊞⊟⊠⊡⊢⊣⊤⊥⊦⊧⊨⊩⊪⊫⊬⊭⊮⊯"
DEFAULT_IMAGE_SIZE = (150, 270)  # Adjusted for 9:16 aspect ratio, increased for more detail
CHARACTER_FONT_SIZE = 7  # Reduced to fit more characters on screen
FPS = 60
VIDEO_DURATION_SECONDS = 15  # Adjust based on how fast you want it to animate
HOLD_FINAL_FRAME_SECONDS = 1.5  # How long to hold the final frame
BACKGROUND_COLOR = (10, 10, 10)  # Slightly darker background
TEXT_COLOR = (255, 255, 255)
USE_COLOR = True  # Use colors from the original image
ANIMATION_STYLE = "ants"  # Options: "line", "matrix", "ants", "random"

# =================================================

def resize_image(image, new_size):
    """Resize image while preserving aspect ratio."""
    return image.resize(new_size)

def grayify(image):
    """Convert image to grayscale."""
    return image.convert("L")

def get_color_palette(image, num_colors=32):
    """Extract a color palette from the image using GPU acceleration."""
    # Resize image to make processing faster
    small_img = image.copy()
    small_img.thumbnail((200, 200))  # Larger thumbnail for better color extraction on 8GB GPU
    
    # Convert to numpy array for processing
    img_array = np.array(small_img)
    pixels = img_array.reshape(-1, 3)
    
    # Use K-means clustering to find dominant colors
    from sklearn.cluster import KMeans
    import warnings
    
    # Move data to GPU
    pixels_tensor = torch.tensor(pixels, dtype=torch.float32).to(DEVICE)
      # Pre-process on GPU with normalization for better kmeans results
    with torch.amp.autocast(device_type='cuda', enabled=True):
        pixels_tensor = pixels_tensor / 255.0  # Normalize to 0-1 range
        
        # Optionally reduce dimensionality if needed
        if pixels_tensor.shape[0] > 100000:
            # Sample a subset of pixels for faster processing
            indices = torch.randperm(pixels_tensor.shape[0], device=DEVICE)[:100000]
            pixels_tensor = pixels_tensor[indices]
    
    # Transfer back to CPU for scikit-learn kmeans
    pixels_numpy = pixels_tensor.cpu().numpy()
    
    # Suppress specific sklearn warnings
    with warnings.catch_warnings():
        warnings.simplefilter("ignore")
        kmeans = KMeans(n_clusters=num_colors, n_init=10, random_state=42)
        kmeans.fit(pixels_numpy)
    
    # Convert the centers to RGB tuples
    colors = [(int(r*255), int(g*255), int(b*255)) for r, g, b in kmeans.cluster_centers_]
    
    # Clear GPU memory
    torch.cuda.empty_cache()
    
    return colors

def pixels_to_ascii_with_color(image):
    """Convert image to ASCII characters with color information using GPU acceleration."""
    # Get grayscale version for ASCII mapping
    gray_img = grayify(image)
    width = image.width
    
    # Prepare data structures to hold the characters and their colors
    ascii_chars = []
    ascii_colors = []
    
    # Process the entire image at once using tensors
    # Convert PIL images to PyTorch tensors with optimized dtype
    gray_tensor = torch.tensor(np.array(gray_img), dtype=torch.float16).to(DEVICE)  # Use float16 for memory savings
    color_tensor = torch.tensor(np.array(image), dtype=torch.uint8).to(DEVICE)  # Use uint8 for colors
    
    # Process grayscale values in parallel on GPU
    # Divide by 25 to get character index, using optimized operations
    with torch.amp.autocast(device_type='cuda', enabled=True):  # Use mixed precision for faster processing
        char_indices = (gray_tensor / 25.0).clamp(0, len(ASCII_CHARS) - 1).long()
    
    # Calculate total pixels and number of newlines
    total_pixels = char_indices.numel()
    rows = gray_img.height
    
    # Pre-allocate arrays on CPU at once (more efficient than dynamic lists)
    # Pre-calculate the exact size including newlines
    total_with_newlines = total_pixels + rows - 1  # -1 because the last row doesn't need a newline
    
    # Use numpy arrays for better performance
    ascii_chars = np.empty(total_with_newlines, dtype=object)
    ascii_colors = np.empty(total_with_newlines, dtype=object)
    
    # Create a tensor to track where newlines should go
    # This is much faster than checking % width for each pixel
    newline_positions = torch.tensor([(i+1) % width == 0 for i in range(total_pixels)], device=DEVICE)
    
    # Use larger batch size for improved throughput
    batch_size = 100000  # Increased batch size for better GPU utilization
    
    # Track two indices: one for the input (i) and one for the output with newlines (char_index)
    char_index = 0
    
    # Process in batches to avoid GPU memory pressure
    for i in range(0, total_pixels, batch_size):
        end_idx = min(i + batch_size, total_pixels)
        
        # Process indices and colors in chunks
        batch_indices = char_indices.flatten()[i:end_idx].cpu().numpy()
        batch_colors = color_tensor.reshape(-1, 3)[i:end_idx].cpu().numpy()
        batch_newlines = newline_positions[i:end_idx].cpu().numpy()
        
        # Single loop for processing characters and colors together
        for b_idx in range(len(batch_indices)):
            idx = i + b_idx
            
            # Get character and color
            ascii_chars[char_index] = ASCII_CHARS[int(batch_indices[b_idx])]
            # Store color as tuple directly (more efficient than tuple comprehension)
            ascii_colors[char_index] = (
                int(batch_colors[b_idx][0]),
                int(batch_colors[b_idx][1]),
                int(batch_colors[b_idx][2])
            )
            char_index += 1
            
            # Add newline if needed and if not at the end of all pixels
            if batch_newlines[b_idx] and idx < total_pixels - 1:
                ascii_chars[char_index] = '\n'
                ascii_colors[char_index] = None  # No color for newlines
                char_index += 1
    
    # Convert numpy arrays to lists for compatibility with the rest of the code
    ascii_chars = ascii_chars[:char_index].tolist()
    ascii_colors = ascii_colors[:char_index].tolist()
    
    # Clear GPU memory
    torch.cuda.empty_cache()
    
    return ascii_chars, ascii_colors

def pixels_to_ascii(image):
    """Convert grayscale image to ASCII characters using GPU acceleration."""
    if USE_COLOR:
        ascii_chars, _ = pixels_to_ascii_with_color(image)
        return ''.join(ascii_chars)
    
    # GPU-accelerated version for non-color conversion
    gray_img = grayify(image)
    width = image.width
    
    # Convert to GPU tensor for faster processing
    gray_tensor = torch.tensor(np.array(gray_img), dtype=torch.float16).to(DEVICE)
      # Process in GPU with mixed precision
    with torch.amp.autocast(device_type='cuda', enabled=True):
        char_indices = (gray_tensor / 25.0).clamp(0, len(ASCII_CHARS) - 1).long()
    
    # Process in batches for better GPU utilization on 8GB VRAM
    batch_size = 50000  # Large batch size for 8GB VRAM
    total_pixels = char_indices.numel()
    
    # Pre-allocate string for better performance
    ascii_chars = []
    
    for i in range(0, total_pixels, batch_size):
        end_idx = min(i + batch_size, total_pixels)
        # Process data in chunks
        batch_indices = char_indices.flatten()[i:end_idx].cpu().numpy()
        
        # Process the batch
        for b_idx, char_idx in enumerate(batch_indices):
            idx = i + b_idx
            ascii_chars.append(ASCII_CHARS[int(char_idx)])
            
            # Add newline if at end of row
            if ((idx + 1) % width == 0) and (idx < total_pixels - 1):
                ascii_chars.append('\n')
    
    # Clear GPU memory
    torch.cuda.empty_cache()
    
    return ''.join(ascii_chars)

def create_random_indices(total_chars, seed=None):
    """Create a randomized order of character indices."""
    import random
    if seed is not None:
        random.seed(seed)
    
    indices = list(range(total_chars))
    random.shuffle(indices)
    return indices

def create_matrix_indices(ascii_str, cols, rows):
    """Create indices for a matrix-style pattern (top-to-bottom)."""
    indices = []
    for x in range(cols):
        for y in range(rows):
            idx = y * (cols + 1) + x  # +1 for newline
            if idx < len(ascii_str) and ascii_str[idx] != '\n':
                indices.append(idx)
    return indices

def create_ants_indices(ascii_str, cols, rows, num_ants=10):
    """Create indices for an 'ants' pattern where characters appear in trails."""
    import random
    random.seed(42)  # For reproducibility
    
    indices = []
    visited = set()
    
    # Initialize random ant positions
    ants = []
    for _ in range(num_ants):
        x = random.randint(0, cols - 1)
        y = random.randint(0, rows - 1)
        ants.append((x, y))
    
    # Let ants wander and drop characters
    steps = cols * rows * 2  # Ensure we can get enough characters
    for _ in range(steps):
        for i in range(len(ants)):
            x, y = ants[i]
            idx = y * (cols + 1) + x  # +1 for newline
            
            if 0 <= x < cols and 0 <= y < rows and idx < len(ascii_str) and ascii_str[idx] != '\n' and idx not in visited:
                indices.append(idx)
                visited.add(idx)
            
            # Move the ant in a random direction
            dx = random.choice([-1, 0, 1])
            dy = random.choice([-1, 0, 1])
            new_x = max(0, min(cols - 1, x + dx))
            new_y = max(0, min(rows - 1, y + dy))
            ants[i] = (new_x, new_y)
    
    # Add any remaining characters that weren't visited
    remaining = [i for i in range(len(ascii_str)) if ascii_str[i] != '\n' and i not in visited]
    random.shuffle(remaining)
    indices.extend(remaining)
    
    return indices[:len(ascii_str.replace('\n', ''))]  # Limit to actual character count

def create_ascii_frame(ascii_str, frame_index, total_frames, font, output_path, color_data=None):
    """Create a single frame with partial ASCII rendering using optimized rendering techniques."""
    # Calculate the total number of displayable characters (excluding newlines)
    total_chars = len(ascii_str.replace('\n', ''))
    
    # Get dimensions
    lines = ascii_str.split('\n')
    rows = len(lines)
    cols = max(len(line) for line in lines)
    
    # Generate character indices based on the animation style - calculate once and cache
    if not hasattr(create_ascii_frame, 'indices'):
        if ANIMATION_STYLE == 'line':
            # Traditional line-by-line animation
            create_ascii_frame.indices = list(range(total_chars))
        elif ANIMATION_STYLE == 'matrix':
            # Matrix-style (vertical columns)
            create_ascii_frame.indices = create_matrix_indices(ascii_str, cols, rows)
        elif ANIMATION_STYLE == 'ants':
            # Ant-trail pattern
            create_ascii_frame.indices = create_ants_indices(ascii_str, cols, rows)
        else:  # 'random' or fallback
            # Completely random pattern
            create_ascii_frame.indices = create_random_indices(total_chars)
        
        # Convert to PyTorch tensor for faster lookup if CUDA is available
        if CUDA_AVAILABLE:
            create_ascii_frame.indices_tensor = torch.tensor(create_ascii_frame.indices, 
                                                           device=DEVICE, 
                                                           dtype=torch.long)
    
    # Calculate how many characters to display in this frame
    if frame_index >= total_frames:
        # Final frame shows everything
        chars_to_display = total_chars
    else:
        # Progressive revelation
        chars_per_frame = total_chars / total_frames
        chars_to_display = min(int(round((frame_index + 1) * chars_per_frame)), total_chars)
    
    # Get the indices we need for this frame
    indices_to_use = create_ascii_frame.indices_tensor[:min(chars_to_display, len(create_ascii_frame.indices))]
    
    # Pre-calculate mapping from indices to positions in ascii_str (just once)
    if not hasattr(create_ascii_frame, 'index_mapping'):
        # Calculate the mapping once and cache it
        create_ascii_frame.index_mapping = {}
        char_count = 0
        for idx, char in enumerate(ascii_str):
            if char != '\n':
                create_ascii_frame.index_mapping[char_count] = idx
                char_count += 1
        
        # Convert to tensors for faster lookup
        if len(create_ascii_frame.index_mapping) > 0:
            keys = list(create_ascii_frame.index_mapping.keys())
            values = list(create_ascii_frame.index_mapping.values())
            create_ascii_frame.index_keys = torch.tensor(keys, device=DEVICE, dtype=torch.long)
            create_ascii_frame.index_values = torch.tensor(values, device=DEVICE, dtype=torch.long)
    
    # Pre-calculate color mapping to avoid expensive calculations per character (just once)
    if USE_COLOR and color_data is not None and not hasattr(create_ascii_frame, 'color_mapping'):
        # Create a mapping from ascii_str index to color_data index
        create_ascii_frame.color_mapping = {}
        char_count = 0
        
        # For each character in ascii_str
        for idx, char in enumerate(ascii_str):
            if char != '\n':
                # Map the ascii_str index to the color_data index
                create_ascii_frame.color_mapping[idx] = char_count
                char_count += 1
    
    # Create the character mask using GPU acceleration
    char_mask = {}
    indices_numpy = indices_to_use.cpu().numpy()
    
    # Try to use a cached mask if available and we're in a memory-intensive segment
    reuse_mask = False
    if MEMORY_MANAGER_AVAILABLE and frame_index > total_frames * 0.5:  # Only in second half of frames
        cached_mask = mm.get_cached_mask(frame_index, total_frames)
        if cached_mask is not None:
            char_mask = cached_mask
            reuse_mask = True
    
    if not reuse_mask:
        # Process in batch for better GPU utilization
        batch_size = 10000  # Increased from 5000 for better throughput
        for i in range(0, len(indices_numpy), batch_size):
            batch_indices = indices_numpy[i:i+batch_size]
            for idx in batch_indices:
                if idx in create_ascii_frame.index_mapping:
                    char_mask[create_ascii_frame.index_mapping[idx]] = True
        
        # Cache the mask for later reuse at strategic points
        if MEMORY_MANAGER_AVAILABLE:
            if (frame_index % 15 == 0 or  # Periodic caching
                frame_index == total_frames or  # Cache the final frame
                frame_index > total_frames * 0.8):  # Cache more in final segment
                mm.cache_mask(frame_index, char_mask, total_frames)
    
    # Create blank canvas with dimensions to fit the entire TikTok screen
    img_width = int(TIKTOK_RESOLUTION[0])
    img_height = int(TIKTOK_RESOLUTION[1])
    
    # Calculate character size to fill the screen
    char_width = img_width / cols
    char_height = img_height / rows
    
    # Font scale based on character size
    font_scale = min(char_width, char_height) * FONT_SCALE_MULTIPLIER
    
    # Check optimization level and use the appropriate rendering method
    if OPTIMIZATION_LEVEL == 0:
        # LEVEL 0: Original PIL-based rendering (slowest)
        img = Image.new('RGB', (img_width, img_height), color=BACKGROUND_COLOR)
        draw = ImageDraw.Draw(img)
        
        # Try to find a monospace font
        custom_font = font  # Use the provided font as a fallback
        
        y_pos = 0
        char_index = 0
        
        for line_num, line in enumerate(ascii_str.split('\n')):
            x_pos = 0
            for char in line:
                if char_index in char_mask:
                    if USE_COLOR and color_data is not None:
                        # Use pre-calculated color mapping
                        if hasattr(create_ascii_frame, 'color_mapping') and char_index in create_ascii_frame.color_mapping:
                            color_idx = create_ascii_frame.color_mapping[char_index]
                            if 0 <= color_idx < len(color_data):
                                text_color = color_data[color_idx]
                            else:
                                text_color = TEXT_COLOR
                        else:
                            text_color = TEXT_COLOR
                    else:
                        text_color = TEXT_COLOR
                    
                    # Draw the character using PIL (slow)
                    draw.text((x_pos, y_pos), char, fill=text_color, font=custom_font)
                
                x_pos += char_width
                char_index += 1
            
            y_pos += char_height
            char_index += 1  # for the newline
        
        img.save(output_path)
    
    elif OPTIMIZATION_LEVEL == 3 and PRERENDER_AVAILABLE:
        # LEVEL 3: Pre-rendered texture atlas (fastest)
        print(f"Using prerender optimization for frame {frame_index}") if frame_index == 0 else None
        
        # Use the optimize_ascii_frame_generation function from prerender_optimizer
        img_np = optimize_ascii_frame_generation(
            ascii_str=ascii_str,
            char_mask=char_mask,
            font_scale=font_scale,
            img_width=img_width,
            img_height=img_height,
            background_color=BACKGROUND_COLOR,
            use_color=USE_COLOR,
            color_data=color_data
        )
        
        # Save the frame
        cv2.imwrite(output_path, img_np)
        
    else:
        # LEVEL 1 or 2: OpenCV-based rendering with additional optimizations
        # Create a blank numpy array for our canvas
        img_np = np.zeros((img_height, img_width, 3), dtype=np.uint8)
        img_np[:,:] = BACKGROUND_COLOR  # Fill the background
        
        # Configure OpenCV font and scale parameters
        font_face = cv2.FONT_HERSHEY_SIMPLEX  # Standard font, can be changed
        thickness = max(1, int(font_scale * 1.5))  # Adjust thickness based on font size
        line_type = cv2.LINE_AA  # Anti-aliased lines for smoother text
        
        # Precompute text sizes and baselines for optimization (once per unique character)
        if not hasattr(create_ascii_frame, 'char_sizes'):
            create_ascii_frame.char_sizes = {}
            for char in set(ascii_str.replace('\n', '')):
                text_size, baseline = cv2.getTextSize(char, font_face, font_scale, thickness)
                create_ascii_frame.char_sizes[char] = (text_size[0], text_size[1], baseline)
        
        if OPTIMIZATION_LEVEL == 1:
            # LEVEL 1: OpenCV character-by-character (medium speed)
            y_pos = 0
            char_index = 0
            
            for line_num, line in enumerate(ascii_str.split('\n')):
                x_pos = 0
                for char in line:
                    if char_index in char_mask:
                        if USE_COLOR and color_data is not None:
                            # Use pre-calculated color mapping
                            if hasattr(create_ascii_frame, 'color_mapping') and char_index in create_ascii_frame.color_mapping:
                                color_idx = create_ascii_frame.color_mapping[char_index]
                                if 0 <= color_idx < len(color_data):
                                    color = color_data[color_idx]
                                else:
                                    color = TEXT_COLOR
                            else:
                                color = TEXT_COLOR
                        else:
                            color = TEXT_COLOR
                        
                        # Draw the character
                        char_size = create_ascii_frame.char_sizes.get(char, (char_width*0.7, char_height*0.7, 0))
                        x_offset = (char_width - char_size[0]) / 2
                        y_offset = (char_height + char_size[1]) / 2
                        
                        # Draw text with OpenCV - faster than PIL
                        cv2.putText(img_np, char, 
                                   (int(x_pos + x_offset), int(y_pos + y_offset)),
                                   font_face, font_scale, color, thickness, line_type)
                    
                    x_pos += char_width
                    char_index += 1
                
                y_pos += char_height
                char_index += 1  # for the newline
        else:
            # LEVEL 2: OpenCV with batched line rendering (fast)
            # Group characters by line for batch rendering
            line_texts = {}  # Maps line number to visible text for that line
            line_colors = {}  # Maps line number to colors for that line
            
            # Group characters by line for batch rendering
            char_index = 0
            line_num = 0
            
            for line in lines:
                current_line_text = ""
                current_line_colors = []
                x_pos = 0
                
                for char in line:
                    visible = char_index in char_mask
                    
                    # Add to the current line (either the character or a space placeholder)
                    if visible:
                        current_line_text += char
                        
                        if USE_COLOR and color_data is not None:
                            # Use pre-calculated color mapping
                            if hasattr(create_ascii_frame, 'color_mapping') and char_index in create_ascii_frame.color_mapping:
                                color_idx = create_ascii_frame.color_mapping[char_index]
                                if 0 <= color_idx < len(color_data):
                                    current_line_colors.append(color_data[color_idx])
                                else:
                                    current_line_colors.append(TEXT_COLOR)
                            else:
                                current_line_colors.append(TEXT_COLOR)
                        else:
                            current_line_colors.append(TEXT_COLOR)
                    else:
                        current_line_text += " "  # Invisible character as space
                        current_line_colors.append(None)  # No color needed
                    
                    x_pos += char_width
                    char_index += 1
                
                # Store the completed line
                if current_line_text.strip():  # Only store non-empty lines
                    line_texts[line_num] = current_line_text
                    line_colors[line_num] = current_line_colors
                
                char_index += 1  # for the newline
                line_num += 1
            
            # Render text line by line using OpenCV
            for line_num, text in line_texts.items():
                y_pos = int((line_num + 0.8) * char_height)  # Add baseline offset
                
                # Process characters in batches for better performance
                x_pos = 0
                for i, char in enumerate(text):
                    if char == ' ':
                        x_pos += char_width
                        continue
                        
                    # Get the character color
                    color = line_colors[line_num][i] if i < len(line_colors[line_num]) else TEXT_COLOR
                    if color is None:
                        x_pos += char_width
                        continue
                        
                    # Draw the character
                    char_size = create_ascii_frame.char_sizes.get(char, (char_width*0.7, char_height*0.7, 0))
                    x_offset = (char_width - char_size[0]) / 2  # Center horizontally
                    y_offset = (char_height + char_size[1]) / 2  # Adjust vertical position
                    
                    # Draw text with OpenCV
                    cv2.putText(img_np, char, 
                               (int(x_pos + x_offset), int(y_pos + y_offset)),
                               font_face, font_scale, color, thickness, line_type)
                    
                    x_pos += char_width
        
        # Save the frame
        cv2.imwrite(output_path, img_np)
        
        # Free memory
        del img_np
    
    # As we progress through frames, we need less frequent memory clearing
    # Early frames need more frequent clearing, later frames need less
    if frame_index < total_frames * 0.3:
        # Clear memory every 5 frames at the beginning
        if frame_index % 5 == 0:
            torch.cuda.empty_cache()
    elif frame_index < total_frames * 0.7:
        # Clear memory every 20 frames in the middle
        if frame_index % 20 == 0:
            torch.cuda.empty_cache()
    else:
        # Clear memory every 50 frames near the end
        if frame_index % 50 == 0:
            torch.cuda.empty_cache()
    
    # Final frame cleanup for late frames
    if frame_index > total_frames * 0.85:
        # For very late frames, we want to cleanup non-essential data to prevent memory bloat
        # Clear any extra large data structures that aren't needed for rendering future frames
        if hasattr(create_ascii_frame, 'index_values') and frame_index > total_frames * 0.95:
            # In the final 5% of frames, we probably don't need these tensors anymore
            # since all characters are likely to be visible
            if torch.cuda.is_available():
                torch.cuda.empty_cache()

def process_frame_batch(ascii_art, frame_indices, total_frames, font, output_dir, color_data=None):
    """Process a batch of frames using GPU acceleration in a single process"""
    # This function is kept for API compatibility but we're now using direct frame generation
    # since it's more efficient to avoid the process creation overhead
    for frame_index in frame_indices:
        output_path = os.path.join(output_dir, f"frame_{frame_index:04d}.png")
        create_ascii_frame(ascii_art, frame_index, total_frames, font, output_path, color_data)
    
    return len(frame_indices)

def generate_frames(ascii_art, output_dir, total_frames=100, color_data=None):
    """Generate all frames of the ASCII animation using optimized GPU processing."""
    if os.path.exists(output_dir):
        shutil.rmtree(output_dir)
    os.makedirs(output_dir)

    # Find a better font that supports monospace characters
    try:
        # Try to find a good monospace font on the system
        font_paths = [
            "C:/Windows/Fonts/consola.ttf",  # Windows Consolas
            "C:/Windows/Fonts/lucon.ttf",    # Windows Lucida Console
            "/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf",  # Linux
            "/System/Library/Fonts/Monaco.ttf"  # macOS
        ]
        
        font = None
        for path in font_paths:
            if os.path.exists(path):
                font = ImageFont.truetype(path, CHARACTER_FONT_SIZE)
                break
                
        # Fallback to default if no suitable font found
        if font is None:
            font = ImageFont.load_default()
            
    except Exception:
        # If there's any error, use the default font
        font = ImageFont.load_default()    # Calculate adjusted number of frames to ensure all content is displayed
    total_display_chars = len(ascii_art.replace('\n', ''))
    
    print(f"Total ASCII characters to render: {total_display_chars}")
    print(f"Animation style: {ANIMATION_STYLE}")
      # Configure GPU for maximum performance - single process approach    torch.cuda.set_per_process_memory_fraction(0.95)  # Use 95% of available VRAM for 8GB RTX 4060
    torch.cuda.empty_cache()  # Clear any existing allocations
    
    # Enable TensorFloat-32 (TF32) for Ampere/Ada architecture
    torch.backends.cuda.matmul.allow_tf32 = True
    torch.backends.cudnn.allow_tf32 = True
    
    # Calculate optimal batch size for frame generation for 8GB VRAM
    # Use adaptive batch sizing to prevent slowdown in later frames
    batch_size = 48  # Base batch size - will be adjusted dynamically
    
    # Process frames in batches for better GPU utilization
    print(f"Generating {total_frames} frames using pure GPU acceleration with adaptive batch sizing...")
    
    # Pre-cache indices and other data structures to avoid redundant calculations
    # This preparation will make the frame generation much faster
      # Initialize static data used by create_ascii_frame for all frames
    dummy_frame = os.path.join(output_dir, "dummy.png")
    create_ascii_frame(ascii_art, 0, total_frames, font, dummy_frame, color_data)
    if os.path.exists(dummy_frame):
        os.remove(dummy_frame)
    
    # Calculate optimization level text for display
    opt_level_text = {
        0: "None (PIL, slowest)",
        1: "Basic (OpenCV, medium)",
        2: "Batch Lines (OpenCV, fast)",
        3: "Pre-rendered Atlas (fastest)"
    }.get(OPTIMIZATION_LEVEL, "Unknown")
    
    print(f"Using optimization level {OPTIMIZATION_LEVEL}: {opt_level_text}")
    
    # Show warning if using level 3 but prerender not available
    if OPTIMIZATION_LEVEL == 3 and not PRERENDER_AVAILABLE:
        print("Warning: Prerender optimization requested but not available. Falling back to level 2.")
    
    start_time = datetime.now()
    last_update = start_time
    frames_processed = 0
    
    with tqdm(total=total_frames, desc="Generating frames") as pbar:
        # Process in batches with adaptive sizing
        batch_idx = 0
        while batch_idx < total_frames:
            # Dynamically adjust batch size based on frame progress
            # Later frames need smaller batches to prevent slowdown
            progress_ratio = batch_idx / total_frames
            if progress_ratio < 0.3:
                # First third - use full batch size
                current_batch_size = batch_size
            elif progress_ratio < 0.7:
                # Middle third - reduce batch size
                current_batch_size = max(16, batch_size // 2)
            else:
                # Last third - use smallest batch size
                current_batch_size = max(8, batch_size // 4)
                
            batch_start_time = datetime.now()
            end_idx = min(batch_idx + current_batch_size, total_frames)
            frame_indices = list(range(batch_idx, end_idx))
            
            # Generate the frames in this batch
            for frame_index in frame_indices:
                output_path = os.path.join(output_dir, f"frame_{frame_index:04d}.png")
                create_ascii_frame(ascii_art, frame_index, total_frames, font, output_path, color_data)
                pbar.update(1)
                frames_processed += 1
              # Clear CUDA cache with adaptive frequency
            if progress_ratio < 0.3:
                # Clear more often at the beginning
                torch.cuda.empty_cache()
            elif progress_ratio < 0.7:
                # Clear less often in the middle
                if batch_idx % 2 == 0:
                    torch.cuda.empty_cache()
            else:
                # Clear rarely toward the end
                if batch_idx % 4 == 0:
                    torch.cuda.empty_cache()
                    
            # Reset memory pressure flag periodically to allow normal operation
            if MEMORY_MANAGER_AVAILABLE and progress_ratio % 0.1 < 0.01:  # Every 10% of progress
                mm.reset_memory_pressure()
            
            # Move to next batch
            batch_idx = end_idx
              # Calculate and display performance metrics every few batches
            if datetime.now() - last_update > timedelta(seconds=5):
                elapsed = (datetime.now() - start_time).total_seconds()
                fps = frames_processed / elapsed if elapsed > 0 else 0
                batch_time = (datetime.now() - batch_start_time).total_seconds()
                batch_fps = len(frame_indices) / batch_time if batch_time > 0 else 0
                
                # Estimate time remaining
                frames_remaining = total_frames - frames_processed
                estimated_seconds = frames_remaining / fps if fps > 0 else 0
                estimated_time = timedelta(seconds=int(estimated_seconds))
                
                # Update progress bar description with current batch size info
                pbar.set_description(
                    f"Generating frames [{fps:.1f} fps | {batch_fps:.1f} fps | batch: {current_batch_size} | ETA: {estimated_time}]"
                )
                  # Dynamically reduce batch size if processing is slowing down
                if batch_fps < fps * 0.7 and current_batch_size > 4:
                    # If batch FPS drops significantly below overall FPS, reduce batch size
                    next_batch_size = max(4, current_batch_size // 2)
                    print(f"\nPerformance dropping - reducing batch size from {current_batch_size} to {next_batch_size}")
                    batch_size = next_batch_size  # Update base batch size for next calculations
                    
                    # Signal memory pressure to both optimizers
                    if PRERENDER_AVAILABLE:
                        signal_memory_pressure()
                    if MEMORY_MANAGER_AVAILABLE:
                        mm.signal_memory_pressure()
                    if MEMORY_MANAGER_AVAILABLE:
                        mm.signal_memory_pressure()
                
                last_update = datetime.now()
    
    # Create the final complete frame
    final_frame_path = os.path.join(output_dir, f"frame_{total_frames:04d}.png")
    create_ascii_frame(ascii_art, total_frames, total_frames, font, final_frame_path, color_data)
    print(f"Generated final complete frame")
    
    # Add additional frames to hold the final view longer
    hold_frames = int(HOLD_FINAL_FRAME_SECONDS * FPS)
    for i in range(1, hold_frames + 1):
        hold_frame_path = os.path.join(output_dir, f"frame_{total_frames+i:04d}.png")
        # Copy the final frame instead of regenerating it
        shutil.copy(final_frame_path, hold_frame_path)
    print(f"Added {hold_frames} holding frames")
    
    return total_frames + hold_frames + 1  # Return the actual number of frames generated

def create_video_from_frames(frame_dir, output_video, fps=FPS):
    """Compile frames into a video using OpenCV."""
    frame_files = sorted(
        [f for f in os.listdir(frame_dir) if f.endswith(".png")]
    )
    first_frame = cv2.imread(os.path.join(frame_dir, frame_files[0]))
    height, width, _ = first_frame.shape

    fourcc = cv2.VideoWriter_fourcc(*'mp4v')
    out = cv2.VideoWriter(output_video, fourcc, fps, (width, height))

    for frame_file in frame_files:
        frame_path = os.path.join(frame_dir, frame_file)
        frame = cv2.imread(frame_path)
        out.write(frame)

    out.release()
    print(f"Video saved at {output_video}")

def upscale_video(input_path, output_path, resolution=TIKTOK_RESOLUTION):
    """Upscale video to TikTok-friendly dimensions using ffmpeg."""
    print(f"Upscaling video to {resolution[0]}x{resolution[1]}...")
    os.system(f'ffmpeg -i "{input_path}" -vf scale={resolution[0]}:{resolution[1]}:force_original_aspect_ratio=decrease,pad={resolution[0]}:{resolution[1]}:(ow-iw)/2:(oh-ih)/2:black "{output_path}" -y')

def parse_args():
    parser = argparse.ArgumentParser(description="Generate ASCII art animation from an image")
    parser.add_argument('--style', choices=['line', 'matrix', 'ants', 'random'], default='matrix',
                        help='Animation style for character revelation')
    parser.add_argument('--color', action='store_true', help='Use colors from the original image')
    parser.add_argument('--no-color', action='store_true', help='Force black and white output (fastest rendering)')
    parser.add_argument('--duration', type=int, default=VIDEO_DURATION_SECONDS,
                        help='Duration of the video in seconds')
    parser.add_argument('--hold', type=float, default=HOLD_FINAL_FRAME_SECONDS,
                        help='Duration to hold the final frame in seconds')
    parser.add_argument('--fps', type=int, default=FPS, help='Frames per second')
    parser.add_argument('--input-dir', default=INPUT_DIR, help='Directory containing input images')
    parser.add_argument('--output-dir', default=OUTPUT_FRAMES_DIR, help='Directory for output frames')
    parser.add_argument('--output', default=FINAL_VIDEO_PATH, help='Path for the final video file')
    parser.add_argument('--optimization-level', type=int, default=2, choices=[0, 1, 2, 3], 
                        help='Optimization level: 0=None, 1=OpenCV, 2=BatchLines, 3=Prerender')
    parser.add_argument('--font-scale', type=float, default=0.04,
                        help='Font scale multiplier for OpenCV rendering (adjust for readability)')
    return parser.parse_args()

def process_single_image(image_file, input_dir, output_dir, output_path):
    """Process a single image and generate the ASCII art video."""
    input_image_path = os.path.join(input_dir, image_file)
    print(f"\nProcessing image: {input_image_path}")
    
    # Get the file name without extension to use in output naming
    file_name = os.path.splitext(image_file)[0]
    # Create a unique output path for this image
    if output_path == FINAL_VIDEO_PATH:  # Using default output path
        this_output_path = f"ascii_video_{file_name}.mp4"
    else:
        # If a custom output was specified, add the filename before the extension
        base, ext = os.path.splitext(output_path)
        this_output_path = f"{base}_{file_name}{ext}"
        
    # Create a unique temporary frames directory for this image
    this_output_dir = f"{output_dir}_{file_name}"
    
    # Process image
    try:
        image = Image.open(input_image_path)
    except Exception as e:
        print(f"Error opening image {image_file}: {e}")
        return
    
    # Calculate aspect ratio for resize
    target_aspect = TIKTOK_RESOLUTION[0] / TIKTOK_RESOLUTION[1]  # Width / Height
    img_aspect = image.width / image.height
    
    if abs(img_aspect - target_aspect) > 0.1:  # If aspect ratios differ significantly
        print(f"Warning: Image aspect ratio ({img_aspect:.2f}) differs from TikTok aspect ratio ({target_aspect:.2f})")
        print("The ASCII art will be adjusted to fit TikTok dimensions")

    # Resize the image while maintaining aspect ratio 
    # but adjust character count to match TikTok aspect ratio
    small_image = resize_image(image, DEFAULT_IMAGE_SIZE)
    
    # Process image
    if USE_COLOR:
        ascii_chars, color_data = pixels_to_ascii_with_color(small_image)
        ascii_art = ''.join(ascii_chars)
        print(f"Using colored ASCII art with {len(color_data)} color values")
    else:
        gray_image = grayify(small_image)
        ascii_art = pixels_to_ascii(gray_image)
        color_data = None
    
    # Calculate how many characters we're dealing with
    ascii_chars_count = len(ascii_art.replace('\n', ''))
    total_characters = DEFAULT_IMAGE_SIZE[0] * DEFAULT_IMAGE_SIZE[1]
    print(f"Total characters in the ASCII art: {ascii_chars_count} out of {total_characters} pixels")

    # Step 3: Generate frames
    total_frames = FPS * VIDEO_DURATION_SECONDS
    actual_frames = generate_frames(ascii_art, this_output_dir, total_frames, color_data)

    # Step 4: Compile into video
    temp_video = f"temp_output_{file_name}.mp4"
    create_video_from_frames(this_output_dir, temp_video)

    # Step 5: Upscale to TikTok format
    upscale_video(temp_video, this_output_path)

    # Clean up
    os.remove(temp_video)
    # Optionally remove the frames directory if you don't need to keep them
    # shutil.rmtree(this_output_dir)  # Uncomment to delete frame directories
    
    print(f"✅ Completed video for {image_file}!")
    print(f"   Frames: {actual_frames}")
    print(f"   Duration: approximately {actual_frames/FPS:.2f} seconds")
    print(f"   Output file: {this_output_path}")
    
    return actual_frames, this_output_path


def main():
    print("=== ASCII Art Generator ===")

    # Parse command line arguments
    args = parse_args()
      # Override global variables with arg values
    global ANIMATION_STYLE, USE_COLOR, VIDEO_DURATION_SECONDS, HOLD_FINAL_FRAME_SECONDS, FPS
    global OPTIMIZATION_LEVEL, FONT_SCALE_MULTIPLIER
    ANIMATION_STYLE = args.style
    
    # Handle color settings - no-color takes precedence
    if args.no_color:
        USE_COLOR = False
    else:
        USE_COLOR = args.color
        
    VIDEO_DURATION_SECONDS = args.duration
    HOLD_FINAL_FRAME_SECONDS = args.hold
    FPS = args.fps
    OPTIMIZATION_LEVEL = args.optimization_level
    FONT_SCALE_MULTIPLIER = args.font_scale
    
    print(f"Animation style: {ANIMATION_STYLE}")
    print(f"Use color: {USE_COLOR}")
    print(f"Optimization level: {OPTIMIZATION_LEVEL}")

    # Step 1: Get all input images
    image_files = [f for f in os.listdir(args.input_dir) if f.lower().endswith(('.png', '.jpg', '.jpeg'))]
    if not image_files:
        print(f"No images found in {args.input_dir}")
        return
    
    print(f"Found {len(image_files)} images to process:")
    for i, img in enumerate(image_files):
        print(f"{i+1}. {img}")
    
    # Process each image and create a video for each
    results = []
    for image_file in image_files:
        result = process_single_image(image_file, args.input_dir, args.output_dir, args.output)
        if result:
            results.append(result)
    
    # Summary
    if results:
        print("\n=== Processing Complete ===")
        print(f"Successfully created {len(results)} ASCII art videos:")
        for i, (frames, output_path) in enumerate(results):
            print(f"{i+1}. {output_path} ({frames/FPS:.2f} seconds)")
    else:
        print("\nNo videos were successfully created.")

if __name__ == "__main__":
    main()