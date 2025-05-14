# 🏃‍♂️ Maze Escape Animation Generator 🎮

A procedurally generated maze escape challenge with physics-based animations designed for TikTok-style content.

![Maze Escape Demo](resources/maze_escape_demo.png)

## 📝 Overview

This project creates short, entertaining videos showing different characters with unique abilities racing to escape a procedurally generated maze. The simulation uses real-time physics for smooth, reactive animations that are perfect for TikTok-style brain rot videos!

## ✨ Features

- Procedurally generated mazes with customizable complexity
- Multiple character types with unique abilities:
  - 🏃 **Runner**: Moves faster but has less control. Special ability: Speed boost!
  - 💪 **Smasher**: Can break through certain walls. Special ability: Smash breakable walls!
  - 🧗 **Climber**: Can climb over obstacles. Special ability: Scale walls!
  - ✨ **Teleporter**: Can teleport short distances. Special ability: Teleport ahead!
- Physics-based movement and interaction
- Particle effects and visual feedback
- Video output optimized for TikTok format (9:16 aspect ratio)

## 🛠️ Dependencies

- SDL2: Graphics and window management
- Chipmunk2D: Physics engine
- FFmpeg: Video encoding (external dependency)

## 🚀 Getting Started

### Windows

1. Install [Git](https://git-scm.com/download/win)
2. Install [CMake](https://cmake.org/download/)
3. Install [Visual Studio](https://visualstudio.microsoft.com/) with C++ development tools
4. Install [FFmpeg](https://ffmpeg.org/download.html) and add it to your PATH
5. Download [SDL2 development libraries](https://www.libsdl.org/download-2.0.php) for Windows
6. Run the setup script:
   ```
   setup.bat
   ```

### macOS/Linux

1. Install dependencies:
   ```bash
   # Ubuntu
   sudo apt install git cmake gcc libsdl2-dev ffmpeg
   
   # macOS
   brew install git cmake sdl2 ffmpeg
   ```
2. Run the setup script:
   ```bash
   chmod +x setup.sh
   ./setup.sh
   ```

## 🎮 Usage

```
./maze_escape [options]
```

Options:
- `--width <value>`: Maze width (default: 20)
- `--height <value>`: Maze height (default: 30)
- `--characters <types>`: Character types to include (e.g., "runner,smasher,climber,teleporter")
- `--duration <seconds>`: Maximum simulation duration (default: 30s)
- `--seed <value>`: Random seed (default: current time)
- `--output <filename>`: Output video file (default: "maze_escape.mp4")
- `--debug`: Enable debug display

## 🎬 Creating TikTok Videos

1. Generate a maze escape video:
   ```
   ./maze_escape --output my_video.mp4
   ```
2. Add music and effects using your favorite video editor
3. Upload to TikTok and watch your brain rot content go viral!

## 🧠 How It Works

The application uses:
- A recursive backtracking algorithm to generate random mazes
- Chipmunk2D physics for realistic movement and collisions
- Custom AI for each character type
- FFmpeg for encoding the final video

## 📜 License

This project is open source and available under the MIT License.
