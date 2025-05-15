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
    # Get all unique characters in the ASCII art
    unique_chars = set(ascii_str.replace('\n', ''))
    
    # Create a character atlas for fast rendering
    atlas = CharacterAtlas(
        ''.join(unique_chars),
        font_scale=font_scale * 1.5,
        char_size=64,  # Higher resolution for better quality
        device='cuda' if torch.cuda.is_available() else 'cpu'
    )
    
    # Create blank canvas
    img_np = np.zeros((img_height, img_width, 3), dtype=np.uint8)
    img_np[:,:] = background_color
    
    # Calculate layout
    lines = ascii_str.split('\n')
    rows = len(lines)
    cols = max(len(line) for line in lines)
    
    char_width = img_width / cols
    char_height = img_height / rows
    
    # Prepare positions and colors for all visible characters
    visible_text = []
    positions = []
    colors = []
    
    # Group characters by line for batch rendering
    char_index = 0
    for line_num, line in enumerate(lines):
        y_pos = int(line_num * char_height)
        
        for col_num, char in enumerate(line):
            x_pos = int(col_num * char_width)
            
            # If this character should be visible
            if char_index in char_mask:
                visible_text.append(char)
                positions.append((x_pos, y_pos))
                
                if use_color and color_data is not None:
                    # Use the original image color for this character
                    color_idx = char_index - sum(1 for i in range(char_index) if ascii_str[i] == '\n')
                    if 0 <= color_idx < len(color_data):
                        colors.append(color_data[color_idx])
                    else:
                        colors.append((255, 255, 255))
                else:
                    colors.append((255, 255, 255))
            
            char_index += 1
        
        char_index += 1  # for the newline
    
    # Render all characters at once using the atlas
    rendered_img = atlas.render_text_fast(
        ''.join(visible_text),
        (img_height, img_width, 3),
        positions,
        colors
    )
    
    return rendered_img

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
