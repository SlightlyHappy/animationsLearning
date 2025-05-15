# FallingMarbles Animation

This C# project creates an animation of marbles falling and interacting with various obstacles in a pinball-style map.

## Features

- Physics-based simulation of marbles using Aether.Physics2D
- Customizable map with walls, ramps, and pinball-style obstacles
- Command line interface for configuring the animation parameters
- Outputs PNG frames that can be combined into a video

## Usage

```bash
dotnet run -- --marbles 50 --frames 300 --output frames
```

### Options

- `--marbles`: Number of marbles to simulate (default: 50)
- `--frames`: Number of frames to render (default: 300)
- `--output`: Output directory for frames (default: "frames")

## Requirements

- .NET 9.0
- SkiaSharp
- Aether.Physics2D
- System.CommandLine

## Generating a Video

After generating the frames, you can use FFmpeg to create a video:

```bash
ffmpeg -framerate 60 -i frames/frame_%06d.png -c:v libvpx-vp9 -pix_fmt yuv420p output.webm
```

## Customizing the Map

The map design is implemented in the `MarbleMap` class. You can customize it by modifying the `InitializeMap` method to create different layouts of walls, ramps, and obstacles.
