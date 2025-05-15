# ASCII Art Animation Generator for TikTok

This tool converts images into trippy ASCII art animations with a character-by-character reveal effect, perfect for creating engaging TikTok content. The ASCII art fills the entire TikTok screen and can use colors from the original image.

**NEW: GPU-Accelerated & Optimized Rendering**

This version includes significant performance improvements:
- CUDA acceleration for NVIDIA GPUs
- Multiple optimization levels for 10-100x faster rendering
- Pre-rendered character atlas for maximum performance
- Progress tracking with estimated completion time

## Features

- Full-screen ASCII art optimized for TikTok's 9:16 aspect ratio
- Multiple animation styles (matrix, ants, random, line-by-line)
- Color preservation from the original image
- Customizable animation duration and frame rate
- Holds the final complete frame for better viewing

## Setup

1. Install the required packages:

```
pip install -r requirements.txt
```

2. Place your images in the `input_images` folder.

## Usage

Basic usage:

```
python ascii_generator.py
```

This will create animation frames in the `output_frames` folder and compile them into a video called `ascii_video.mp4`.

## Command Line Options

- `--style` - Animation style (choices: "line", "matrix", "ants", "random", default: "matrix")
- `--color` - Use colors from the original image (flag)
- `--duration` - Animation duration in seconds (default: 30)
- `--hold` - Duration to hold the final frame in seconds (default: 1.5)
- `--fps` - Frames per second (default: 60)
- `--input-dir` - Directory containing input images (default: "input_images")
- `--output-dir` - Directory for output frames (default: "output_frames")
- `--output` - Path for the final video file (default: "ascii_video.mp4")
- `--optimization-level` - Rendering optimization level (0-3, default: 2)
- `--font-scale` - Font scale multiplier for OpenCV rendering (default: 0.04)

### Optimization Levels

- **Level 0**: No optimization (original PIL implementation, slowest)
- **Level 1**: Basic OpenCV rendering (medium speed)
- **Level 2**: Batch line rendering with OpenCV (fast, default)
- **Level 3**: Pre-rendered texture atlas (fastest)

## Examples

Generate a Matrix-style animation with color:

```
python ascii_generator.py --style matrix --color
```

Create an "ants" style animation that runs for 15 seconds:

```
python ascii_generator.py --style ants --duration 15
```

Generate a completely random pattern at a higher frame rate:

```
python ascii_generator.py --style random --fps 90
```

## Animation Styles

1. **Matrix** - Characters appear in vertical columns from top to bottom, like the digital rain in "The Matrix"
2. **Ants** - Characters appear as if being drawn by tiny ants leaving trails across the image
3. **Random** - Characters appear in a completely random order across the entire image
4. **Line** - Traditional line-by-line reveal from top to bottom

## Tips for TikTok Videos

1. Use portrait-oriented images for better TikTok fit
2. The "random" reveal mode often creates more interesting visual effects
3. Keep animations between 5-15 seconds for optimal engagement
4. Add music or sound effects to the final video using a video editor

## Performance Optimization Tips

1. Use the highest optimization level your system supports:
   ```
   python ascii_generator.py --optimization-level 3
   ```

2. For testing/prototyping, reduce duration and frame count:
   ```
   python ascii_generator.py --duration 5 --fps 30
   ```

3. GPU-related tips:
   - Close other GPU-intensive applications while rendering
   - If you experience CUDA out-of-memory errors, try a lower optimization level
   - For maximum performance, use a dedicated GPU with at least 8GB VRAM

4. Compare speed with different optimization levels:
   ```
   # Test each optimization level with a short duration
   python ascii_generator.py --optimization-level 0 --duration 3 --output level0.mp4
   python ascii_generator.py --optimization-level 1 --duration 3 --output level1.mp4
   python ascii_generator.py --optimization-level 2 --duration 3 --output level2.mp4
   python ascii_generator.py --optimization-level 3 --duration 3 --output level3.mp4
   ```
