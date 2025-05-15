"""
Example script demonstrating how to use the ASCII art generator with a sample image.
"""

import os
import sys
import urllib.request
from ascii_generator import ASCIIArtGenerator

def download_sample_image():
    """Download a sample image if not available"""
    sample_image_dir = "input_images"
    sample_image_path = os.path.join(sample_image_dir, "sample.jpg")
    
    # Check if sample image exists
    if os.path.exists(sample_image_path):
        print(f"Sample image already exists at: {sample_image_path}")
        return sample_image_path
    
    # Create directory if it doesn't exist
    os.makedirs(sample_image_dir, exist_ok=True)
    
    # Sample image URL (a public domain image)
    image_url = "https://images.pexels.com/photos/3874034/pexels-photo-3874034.jpeg"
    
    print(f"Downloading sample image from: {image_url}")
    try:
        urllib.request.urlretrieve(image_url, sample_image_path)
        print(f"Sample image downloaded to: {sample_image_path}")
        return sample_image_path
    except Exception as e:
        print(f"Error downloading sample image: {e}")
        print("Please place an image manually in the input_images folder.")
        return None

def main():
    # Download sample image if needed
    sample_image_path = download_sample_image()
    if not sample_image_path:
        print("Sample image not available. Please provide your own image.")
        return
    
    # Create output directory
    output_dir = "output_frames"
    os.makedirs(output_dir, exist_ok=True)
    
    # Initialize the ASCII art generator
    generator = ASCIIArtGenerator(
        width=80,  # ASCII width in characters
        font_size=18,
        trippy_mode=True
    )
    
    # Generate ASCII art
    print("Converting image to ASCII...")
    ascii_rows, gray_img = generator.image_to_ascii(sample_image_path)
    
    # Create animation frames
    print("Generating animation frames...")
    frames_count = generator.create_animation_frames(
        ascii_rows,
        gray_img,
        output_dir,
        reveal_mode="random",
        fps=30,
        total_duration=8.0  # 8 second animation
    )
    
    # Create video
    print("Creating video...")
    generator.create_video(output_dir, "ascii_video.mp4", fps=30)
    
    print("\nDone! Your ASCII art animation has been created.")
    print("- Frames are saved in the 'output_frames' folder")
    print("- Final video is saved as 'ascii_video.mp4'")
    print("\nTip: For best TikTok results, add some music to the video using a video editor!")

if __name__ == "__main__":
    main()
