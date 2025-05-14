using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using SkiaSharp;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Common;

namespace HypnoticPatterns
{
    class Program
    {
        private const int DefaultWidth = 1080;
        private const int DefaultHeight = 1920;
        private const int DefaultFrameRate = 60;
        private const int DefaultDuration = 10;

        static async Task<int> Main(string[] args)
        {
            // Command line options
            var rootCommand = new RootCommand("Hypnotic Patterns Animation Generator");

            var widthOption = new Option<int>(
                "--width",
                () => DefaultWidth,
                "Width of the output video in pixels");
            rootCommand.AddOption(widthOption);

            var heightOption = new Option<int>(
                "--height",
                () => DefaultHeight,
                "Height of the output video in pixels");
            rootCommand.AddOption(heightOption);

            var frameRateOption = new Option<int>(
                "--frame-rate",
                () => DefaultFrameRate,
                "Frame rate of the output video");
            rootCommand.AddOption(frameRateOption);

            var durationOption = new Option<int>(
                "--duration",
                () => DefaultDuration,
                "Duration of the animation in seconds");
            rootCommand.AddOption(durationOption);

            var outputOption = new Option<FileInfo>(
                "--output",
                () => new FileInfo("output.webm"),
                "Output file path");
            rootCommand.AddOption(outputOption);

            rootCommand.SetHandler(
                (width, height, frameRate, duration, output) =>
                {
                    GenerateAnimation(width, height, frameRate, duration, output);
                },
                widthOption, heightOption, frameRateOption, durationOption, outputOption);

            return await rootCommand.InvokeAsync(args);
        }

        private static void GenerateAnimation(int width, int height, int frameRate, int duration, FileInfo output)
        {
            Console.WriteLine($"Generating {duration}s animation at {width}x{height} @ {frameRate}fps to {output.FullName}");

            // Create a directory for the frames
            string framesDirectory = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(output.FullName)!, "frames");
            Directory.CreateDirectory(framesDirectory);

            // Initialize physics world
            World world = new World(new Vector2(0, 0)); // No gravity initially
            
            // Animation parameters
            int totalFrames = duration * frameRate;
            int frameCount = 0;
            
            // Initialize the balls
            List<Ball> balls = new List<Ball>();
            
            // Add initial ball
            float initialRadius = Math.Min(width, height) * 0.05f; // 5% of the smallest dimension
            float containmentRadius = Math.Min(width, height) * 0.45f; // 45% of the smallest dimension
            
            // Create an initial ball
            balls.Add(new Ball(
                position: new Vector2(width / 2, height / 2),
                velocity: new Vector2(
                    (float)(new Random().NextDouble() * 10 - 5),
                    (float)(new Random().NextDouble() * 10 - 5)
                ),
                radius: initialRadius,
                color: RandomColor()
            ));

            // Animation state tracking
            int splitCounter = 0;
            int colorChangeCounter = 0;
            float pulsePhase = 0;
            float rotationAngle = 0;
            
            // Render each frame
            for (int i = 0; i < totalFrames; i++)
            {
                using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
                {
                    var canvas = surface.Canvas;
                    canvas.Clear(SKColors.Black);

                    // Draw a containing circle
                    using (var paint = new SKPaint
                    {
                        Style = SKPaintStyle.Stroke,
                        Color = SKColors.White,
                        StrokeWidth = 4,
                        IsAntialias = true
                    })
                    {
                        // Pulsating effect for the container
                        pulsePhase += 0.02f;
                        float pulseAmount = (float)Math.Sin(pulsePhase) * 10;
                        float currentContainmentRadius = containmentRadius + pulseAmount;
                        
                        // Rotating gradients for the container
                        rotationAngle += 0.5f;
                        using (var shader = SKShader.CreateSweepGradient(
                            new SKPoint(width / 2, height / 2),
                            new SKColor[] { 
                                new SKColor(255, 0, 128), 
                                new SKColor(128, 0, 255),
                                new SKColor(0, 128, 255),
                                new SKColor(0, 255, 128),
                                new SKColor(255, 255, 0),
                                new SKColor(255, 0, 128) 
                            },
                            null,
                            SKMatrix.CreateRotation(rotationAngle * (float)Math.PI / 180, width / 2, height / 2)))
                        {
                            paint.Shader = shader;
                            canvas.DrawCircle(width / 2, height / 2, currentContainmentRadius, paint);
                        }
                    }

                    // Update and draw all balls
                    List<Ball> newBalls = new List<Ball>();
                    List<Ball> ballsToRemove = new List<Ball>();

                    foreach (var ball in balls)
                    {
                        // Update position
                        ball.Position += ball.Velocity;

                        // Check for collision with the container
                        Vector2 centerToPosition = ball.Position - new Vector2(width / 2, height / 2);
                        float distanceFromCenter = centerToPosition.Length();

                        if (distanceFromCenter + ball.Radius > containmentRadius)
                        {
                            // Collision detected, normalize the direction vector
                            Vector2 normal = centerToPosition / distanceFromCenter;
                            
                            // Calculate reflection vector
                            float dotProduct;
                            Vector2 currentVelocity = ball.Velocity; // Create a local copy
                            Vector2 normalCopy = normal; // Create a local copy
                            Vector2.Dot(ref currentVelocity, ref normalCopy, out dotProduct);
                            ball.Velocity = currentVelocity - 2 * dotProduct * normalCopy;
                            
                            // Move the ball back inside the container
                            float correctionDistance = (distanceFromCenter + ball.Radius) - containmentRadius;
                            ball.Position -= normal * correctionDistance;
                            
                            // Split the ball if it's large enough
                            splitCounter++;
                            if (ball.Radius > 5 && splitCounter % 3 == 0)
                            {
                                // Create two smaller balls
                                float newRadius = ball.Radius * 0.7f;
                                
                                // Calculate perpendicular vector for the split directions
                                Vector2 perpendicular = new Vector2(-normal.Y, normal.X);
                                
                                // Create two new balls with velocities in different directions
                                newBalls.Add(new Ball(
                                    position: ball.Position,
                                    velocity: VectorHelper.ReflectVector(ball.Velocity, perpendicular) * 1.1f,
                                    radius: newRadius,
                                    color: ball.Color
                                ));
                                
                                newBalls.Add(new Ball(
                                    position: ball.Position,
                                    velocity: VectorHelper.ReflectVector(ball.Velocity, -perpendicular) * 1.1f,
                                    radius: newRadius,
                                    color: ball.Color
                                ));
                                
                                // Mark the original ball for removal
                                ballsToRemove.Add(ball);
                            }
                            
                            // Change color periodically on bounce
                            colorChangeCounter++;
                            if (colorChangeCounter % 5 == 0)
                            {
                                ball.Color = RandomColor();
                            }
                        }

                        // Draw the ball with a glow effect
                        using (var paint = new SKPaint
                        {
                            Style = SKPaintStyle.Fill,
                            Color = ball.Color,
                            IsAntialias = true
                        })
                        {
                            // Inner glow
                            using (var glowPaint = new SKPaint
                            {
                                Style = SKPaintStyle.Fill,
                                Color = ball.Color.WithAlpha(80),
                                IsAntialias = true,
                                ImageFilter = SKImageFilter.CreateBlur(ball.Radius * 0.5f, ball.Radius * 0.5f)
                            })
                            {
                                canvas.DrawCircle(ball.Position.X, ball.Position.Y, ball.Radius * 1.2f, glowPaint);
                            }
                            
                            // Main ball
                            canvas.DrawCircle(ball.Position.X, ball.Position.Y, ball.Radius, paint);
                        }
                    }

                    // Remove split balls
                    foreach (var ball in ballsToRemove)
                    {
                        balls.Remove(ball);
                    }

                    // Add new balls
                    balls.AddRange(newBalls);
                    
                    // Limit the number of balls to prevent performance issues
                    if (balls.Count > 30)
                    {
                        // Remove smallest balls
                        balls.Sort((a, b) => a.Radius.CompareTo(b.Radius));
                        balls.RemoveRange(0, balls.Count - 30);
                    }

                    // Add a subtle oscillating gravity effect
                    float t = i / (float)totalFrames;
                    world.Gravity = new Vector2(
                        (float)Math.Sin(t * Math.PI * 4) * 0.1f,
                        (float)Math.Cos(t * Math.PI * 3) * 0.1f
                    );
                    
                    // Apply gravity to all balls
                    foreach (var ball in balls)
                    {
                        ball.Velocity += world.Gravity;
                    }

                    // Save the frame
                    using (var image = surface.Snapshot())
                    using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                    {
                        string framePath = System.IO.Path.Combine(framesDirectory, $"frame_{frameCount:D6}.png");
                        using (var stream = File.OpenWrite(framePath))
                        {
                            data.SaveTo(stream);
                        }
                    }

                    frameCount++;
                    
                    // Occasionally introduce a new ball to keep things interesting
                    if (new Random().Next(100) < 2 && balls.Count < 20)
                    {
                        float angle = (float)(new Random().NextDouble() * Math.PI * 2);
                        float spawnRadius = containmentRadius * 0.8f;
                        balls.Add(new Ball(
                            position: new Vector2(
                                width / 2 + spawnRadius * (float)Math.Cos(angle),
                                height / 2 + spawnRadius * (float)Math.Sin(angle)
                            ),
                            velocity: new Vector2(
                                (float)(new Random().NextDouble() * 6 - 3),
                                (float)(new Random().NextDouble() * 6 - 3)
                            ),
                            radius: initialRadius * 0.7f,
                            color: RandomColor()
                        ));
                    }

                    // Progress indicator
                    if (i % frameRate == 0)
                    {
                        Console.WriteLine($"Rendered {i / frameRate}s / {duration}s");
                    }
                }
            }

            Console.WriteLine("Rendering complete! Use FFmpeg to convert the frames to a video.");
            Console.WriteLine($"Example command: ffmpeg -framerate {frameRate} -i {framesDirectory}/frame_%06d.png -c:v libvpx-vp9 -b:v 2M {output.FullName}");
        }

        private static SKColor RandomColor()
        {
            var random = new Random();
            return new SKColor(
                (byte)random.Next(100, 255),
                (byte)random.Next(100, 255),
                (byte)random.Next(100, 255),
                (byte)255
            );
        }
    }

    class Ball
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Radius { get; set; }
        public SKColor Color { get; set; }

        public Ball(Vector2 position, Vector2 velocity, float radius, SKColor color)
        {
            Position = position;
            Velocity = velocity;
            Radius = radius;
            Color = color;
        }
    }

    // Helper method for vector reflection
    static class VectorHelper
    {
        public static Vector2 ReflectVector(Vector2 vector, Vector2 normal)
        {
            float dotProduct;
            Vector2.Dot(ref vector, ref normal, out dotProduct);
            return vector - 2 * dotProduct * normal;
        }
    }
}
