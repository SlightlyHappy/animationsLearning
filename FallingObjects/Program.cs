using System;
using System.CommandLine;
using System.IO;
using System.Collections.Generic;
using SkiaSharp;
using tainicom.Aether.Physics2D;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Dynamics.Joints;
using tainicom.Aether.Physics2D.Common;
using tainicom.Aether.Physics2D.Collision;
using tainicom.Aether.Physics2D.Collision.Shapes;

namespace FallingObjects
{   
    /// <summary>
    /// Helper class to replace missing FixtureFactory
    /// </summary>
    public static class FixtureFactory
    {
        public static Fixture CreateRectangle(float width, float height, float density, Vector2 offset, Body body)
        {
            // Create rectangle vertices
            var vertices = new Vertices(4);
            vertices.Add(new Vector2(-width/2, -height/2) + offset);
            vertices.Add(new Vector2(width/2, -height/2) + offset);
            vertices.Add(new Vector2(width/2, height/2) + offset);
            vertices.Add(new Vector2(-width/2, height/2) + offset);
            
            // Create polygon shape
            var rectangle = new PolygonShape(vertices, density);
            
            // Create fixture
            var fixture = body.CreateFixture(rectangle);
            
            return fixture;
        }
        
        public static Fixture CreateCircle(float radius, float density, Vector2 position, Body body)
        {
            // Create circle shape
            var circle = new CircleShape(radius, density);
            
            // Create fixture
            var fixture = body.CreateFixture(circle);
            
            return fixture;
        }
    }
    class Program
    {
        // Physics world constants
        private const float TimeStep = 1.0f / 60.0f; // 60 fps
        private const int VelocityIterations = 8;
        private const int PositionIterations = 3;
        private const float PixelMetersRatio = 100.0f; // 100px = 1m in physics world

        // Constants for TikTok portrait video
        private const int FrameWidth = 1080;
        private const int FrameHeight = 1920;        // Output directory
        public static string OutputDir = "frames";

        static async Task<int> Main(string[] args)
        {
            // Setup command line options
            var rootCommand = new RootCommand("Generates a physics animation of falling objects.");
            
            var numObjectsOption = new Option<int>(
                "--objects",
                getDefaultValue: () => 50,
                description: "Number of objects to create");
            
            var framesOption = new Option<int>(
                "--frames",
                getDefaultValue: () => 300,
                description: "Number of frames to render");

            var outputDirOption = new Option<string>(
                "--output",
                getDefaultValue: () => "frames",
                description: "Output directory for frames");

            var shapeOption = new Option<string>(
                "--shape",
                getDefaultValue: () => "mixed",
                description: "Shape of objects (circle, box, mixed)");
                
            rootCommand.AddOption(numObjectsOption);
            rootCommand.AddOption(framesOption);
            rootCommand.AddOption(outputDirOption);
            rootCommand.AddOption(shapeOption);

            rootCommand.SetHandler(
                (int numObjects, int frames, string output, string shape) =>
                {
                    OutputDir = output;
                    // Ensure output directory exists
                    Directory.CreateDirectory(OutputDir);
                    
                    Console.WriteLine($"Creating animation with {numObjects} objects over {frames} frames");
                    Console.WriteLine($"Output directory: {OutputDir}");
                    
                    // Run the simulation
                    RunSimulation(numObjects, frames, shape);
                },
                numObjectsOption, framesOption, outputDirOption, shapeOption);

            return await rootCommand.InvokeAsync(args);
        }        static void RunSimulation(int numObjects, int frames, string shape)
        {
            // Create an Aether Physics world with gravity
            var world = new World();
            world.Gravity = new Vector2(0, 10.0f);

            // Create the ground box
            CreateBoundaries(world);

            // Create dynamic falling objects
            var objects = CreateObjects(world, numObjects, shape);

            // Create the renderer
            using var renderer = new PhysicsRenderer(FrameWidth, FrameHeight, PixelMetersRatio);

            // Run the physics simulation and render each frame
            Console.WriteLine($"Starting simulation and rendering {frames} frames...");
            for (int i = 0; i < frames; i++)
            {            // Step the physics simulation
            world.Step(TimeStep);

                // Render the current state of the world
                renderer.RenderWorld(world, objects, i);
                
                // Progress feedback
                if (i % 10 == 0 || i == frames - 1)
                {
                    Console.WriteLine($"Frame {i+1}/{frames} rendered");
                }
            }
            
            Console.WriteLine($"Simulation completed. {frames} frames saved to {OutputDir}/");
            Console.WriteLine("To create a video from these frames, you can use FFmpeg:");
            Console.WriteLine($"ffmpeg -framerate 60 -i {OutputDir}/frame_%04d.png -c:v libx264 -pix_fmt yuv420p output.mp4");
        }        static void CreateBoundaries(World world)
        {
            // Create ground
            var groundBody = world.CreateBody(new Vector2(FrameWidth / (2 * PixelMetersRatio), FrameHeight / PixelMetersRatio));
            groundBody.BodyType = BodyType.Static;
            
            var groundBox = FixtureFactory.CreateRectangle(
                FrameWidth / PixelMetersRatio, 2.0f, 
                1.0f, 
                Vector2.Zero, 
                groundBody);
            groundBox.Restitution = 0.3f;
            groundBox.Friction = 0.5f;
            
            // Create left wall
            var leftWall = world.CreateBody(new Vector2(0, FrameHeight / (2 * PixelMetersRatio)));
            leftWall.BodyType = BodyType.Static;
            var leftBox = FixtureFactory.CreateRectangle(
                2.0f, FrameHeight / PixelMetersRatio, 
                1.0f, 
                Vector2.Zero, 
                leftWall);
            
            // Create right wall
            var rightWall = world.CreateBody(new Vector2(FrameWidth / PixelMetersRatio, FrameHeight / (2 * PixelMetersRatio)));
            rightWall.BodyType = BodyType.Static;
            var rightBox = FixtureFactory.CreateRectangle(
                2.0f, FrameHeight / PixelMetersRatio, 
                1.0f, 
                Vector2.Zero, 
                rightWall);
        }        static List<(Body body, object shapeData, SKColor color)> CreateObjects(World world, int count, string shapeType)
        {
            var random = new Random();
            var objects = new List<(Body, object, SKColor)>();
            var colors = new SKColor[]
            {
                SKColors.Red,
                SKColors.Blue,
                SKColors.Green,
                SKColors.Yellow,
                SKColors.Purple,
                SKColors.Orange,
                SKColors.Cyan
            };

            for (int i = 0; i < count; i++)
            {
                // Random position at the top of the screen
                float x = (float)random.NextDouble() * (FrameWidth - 200) / PixelMetersRatio + 100 / PixelMetersRatio;
                float y = (float)random.NextDouble() * -10 - 2; // Start above the screen
                
                // Create body
                var body = world.CreateBody(new Vector2(x, y));
                body.BodyType = BodyType.Dynamic;
                
                // Add some initial rotational velocity
                body.AngularVelocity = (float)(random.NextDouble() * 2 - 1) * 3.0f;
                
                // Add some initial linear velocity (small variation)
                body.LinearVelocity = new Vector2(
                    (float)(random.NextDouble() * 2 - 1) * 0.5f, // small x velocity
                    (float)random.NextDouble() * 0.5f);          // small downward velocity
                
                // Pick a random color
                SKColor color = colors[random.Next(colors.Length)];
                
                // Create shape based on the shapeType parameter
                object shapeData = null;
                
                if (shapeType == "mixed")
                {
                    shapeType = random.Next(2) == 0 ? "circle" : "box";
                }
                
                if (shapeType == "circle")
                {
                    float radius = (float)(random.NextDouble() * 0.5f + 0.3f); // 0.3m to 0.8m radius
                    var fixture = FixtureFactory.CreateCircle(
                        radius, 
                        1.0f, // density
                        body.Position,
                        body);
                    
                    // Setting some properties for more interesting physics
                    fixture.Friction = 0.3f;
                    fixture.Restitution = 0.4f + (float)random.NextDouble() * 0.4f; // Different bounciness
                    
                    // Store the radius for rendering
                    shapeData = radius;
                }
                else // "box"
                {
                    float width = (float)(random.NextDouble() * 0.8f + 0.3f); // Width in meters
                    float height = (float)(random.NextDouble() * 0.8f + 0.3f); // Height in meters
                    
                    var fixture = FixtureFactory.CreateRectangle(
                        width, height,
                        1.0f, // density
                        Vector2.Zero, // offset from center
                        body);
                    
                    // Setting some properties for more interesting physics
                    fixture.Friction = 0.3f;
                    fixture.Restitution = 0.1f + (float)random.NextDouble() * 0.3f; // Less bouncy than circles
                    
                    // Store vertices for rendering
                    shapeData = new Vector2[]
                    {
                        new Vector2(-width/2, -height/2),
                        new Vector2(width/2, -height/2),
                        new Vector2(width/2, height/2),
                        new Vector2(-width/2, height/2)
                    };
                }
                
                objects.Add((body, shapeData, color));
            }
            
            return objects;
        }
    }    /// <summary>
    /// Renders the physics world to PNG frames
    /// </summary>
    class PhysicsRenderer : IDisposable
    {
        private readonly int _width;
        private readonly int _height;
        private readonly float _pixelMetersRatio;
        private readonly SKBitmap _bitmap;
        
        // Background gradient colors
        private readonly SKColor _bgTop = new SKColor(20, 20, 40); 
        private readonly SKColor _bgBottom = new SKColor(60, 30, 80);

        public PhysicsRenderer(int width, int height, float pixelMetersRatio)
        {
            _width = width;
            _height = height;
            _pixelMetersRatio = pixelMetersRatio;
            _bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        }
        
        public void RenderWorld(World world, List<(Body body, object shapeData, SKColor color)> objects, int frameNumber)
        {
            using var canvas = new SKCanvas(_bitmap);
            
            // Draw gradient background
            using var bgPaint = new SKPaint();
            using var bgShader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(0, _height),
                new[] { _bgTop, _bgBottom },
                null,
                SKShaderTileMode.Clamp);
            
            bgPaint.Shader = bgShader;
            canvas.DrawRect(0, 0, _width, _height, bgPaint);

            // Draw physics objects
            foreach (var (body, shapeData, color) in objects)
            {
                // Convert physics position (meters) to screen position (pixels)
                var position = body.Position;
                var angle = body.Rotation;
                
                var screenX = position.X * _pixelMetersRatio;
                var screenY = position.Y * _pixelMetersRatio;
                
                // Prepare paint for drawing
                using var paint = new SKPaint
                {
                    Color = color,
                    IsAntialias = true
                };
                
                // Save canvas state to restore after rotation
                canvas.Save();
                  // Translate to body position and rotate
                canvas.Translate(screenX, screenY);
                canvas.RotateDegrees(angle * 180 / (float)Math.PI);
                
                // Draw shape
                if (shapeData is float radius) // Circle
                {
                    float screenRadius = radius * _pixelMetersRatio;
                    canvas.DrawCircle(0, 0, screenRadius, paint);
                    
                    // Draw a line to show rotation
                    using var linePaint = new SKPaint 
                    { 
                        Color = SKColors.White, 
                        StrokeWidth = 2,
                        IsAntialias = true
                    };
                    
                    canvas.DrawLine(0, 0, screenRadius, 0, linePaint);
                }
                else if (shapeData is Vector2[] points) // Box or polygon
                {
                    using var path = new SKPath();
                    
                    // First point
                    var firstPoint = points[0] * _pixelMetersRatio;
                    path.MoveTo(firstPoint.X, firstPoint.Y);
                    
                    // Remaining points
                    for (int i = 1; i < points.Length; i++)
                    {
                        var point = points[i] * _pixelMetersRatio;
                        path.LineTo(point.X, point.Y);
                    }
                    
                    // Close the path
                    path.Close();
                    
                    // Draw the path
                    canvas.DrawPath(path, paint);
                }
                
                // Restore canvas to original state
                canvas.Restore();
            }
              // Save the frame
            string outputPath = System.IO.Path.Combine(Program.OutputDir, $"frame_{frameNumber:D4}.png");
            using (var image = SKImage.FromBitmap(_bitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            using (var stream = File.OpenWrite(outputPath))
            {
                data.SaveTo(stream);
            }
        }
        
        public void Dispose()
        {
            _bitmap.Dispose();
        }
    }
}
