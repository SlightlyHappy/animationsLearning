# The Infinite Marble Run

A procedurally generated marble race simulation that creates unique tracks for each run. This project simulates 3-10 marbles with different physical properties racing through a chaotic procedurally generated course.

## Features

- **Procedural Course Generation**: Each run creates a completely new and unique track
- **Physics-Based Simulation**: Realistic marble physics with the Aether.Physics2D engine
- **Multiple Marble Types**: Various marble types with unique properties:
  - **Glass**: Low friction, high bounce
  - **Steel**: Medium friction, higher mass
  - **Rubber**: High friction, maximum bounce
  - **Wood**: High friction, low bounce
  - **Ice**: Extremely low friction
  - **Lead**: High mass, low bounce
  - **Gold**: Heavy and flashy
  - **Cosmic**: Special visual effects
  - **Neon**: Glowing effects
- **Dynamic Camera System**: Auto-focuses on race highlights and dramatic moments
- **Particle Effects**: Visual effects for collisions, trails and special track sections
- **Special Track Elements**:
  - **Ramps**: Fast sections with low friction
  - **Funnels**: Constricting sections
  - **Spinners**: Add rotational forces
  - **Boosters**: Speed up marbles
  - **Slow Fields**: Reduce marble velocity
  - **Bumpers**: Chaotic bouncy sections
- **Race Results Screen**: Showcases winners with a podium and rankings
- **Social Media Optimized**: Designed for TikTok/Reels with portrait aspect ratio

## Requirements

- .NET 9.0 SDK
- Dependencies (automatically installed via NuGet):
  - SkiaSharp (for rendering)
  - Aether.Physics2D (for physics simulation)
  - System.CommandLine (for command-line options)
  - OpenTK (for additional 3D math utilities)
  - SixLabors.ImageSharp (for additional image processing)

## Usage

```
InfiniteMarbleRun [options]

Options:
  --width <width>            Width of the output video in pixels [default: 1080]
  --height <height>          Height of the output video in pixels [default: 1920]
  --frame-rate <frame-rate>  Frame rate of the output video [default: 60]
  --duration <duration>      Duration of the animation in seconds [default: 30]
  --output <output>          Output directory for frames [default: frames]
  --seed <seed>              Random seed for procedural generation [default: random]
  --marbles <marbles>        Number of marbles to race (3-10) [default: 5]
  --complexity <complexity>  Complexity of the generated course (1-10) [default: 5]
  --help                     Show help and usage information
```

## Examples

Generate a 10-second race with 5 marbles:
```
dotnet run -- --duration 10 --marbles 5
```

Generate a complex course with 8 marbles and specific seed:
```
dotnet run -- --complexity 8 --marbles 8 --seed 12345
```

## Creating Videos

After generating the frames, use FFmpeg to create a video:

```
ffmpeg -framerate 60 -i frames/frame_%06d.png -c:v libx264 -pix_fmt yuv420p -crf 18 output.mp4
```

## To-Do List

- [ ] Implement 3D rendering option using OpenTK for more immersive visuals
- [ ] Add configurable marble properties for custom marble types
- [ ] Create a "Championship Mode" that runs multiple races with the same marbles
- [ ] Add obstacle course elements (loop-the-loops, jumps, etc.)
- [ ] Implement a betting/prediction system to engage viewers
- [ ] Add sound effects and background music
- [ ] Create an interactive mode where viewers can control marble properties
- [ ] Optimize rendering for longer races
- [ ] Create a WebGL version for in-browser viewing
- [ ] Add real-time streaming option for live viewing

## Technical Details

The project is structured into several components:

1. **Core**: Basic data structures and interfaces
2. **Generation**: Procedural course generation
3. **Physics**: Physics simulation and collision handling
4. **Marbles**: Various marble types and properties
5. **Rendering**: Visual output and effects

The simulation uses a combination of 2D physics for accurate behavior and advanced rendering techniques for visual appeal.

## License

MIT License
