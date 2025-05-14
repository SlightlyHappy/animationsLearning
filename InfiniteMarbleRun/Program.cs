using System;
using System.CommandLine;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using SkiaSharp;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Common;
using InfiniteMarbleRun.Core;
using InfiniteMarbleRun.Generation;
using InfiniteMarbleRun.Rendering;
using InfiniteMarbleRun.Physics;
using InfiniteMarbleRun.Marbles;

namespace InfiniteMarbleRun
{
    class Program
    {
        // Constants for TikTok/Reels portrait video
        private const int DefaultFrameWidth = 1080;
        private const int DefaultFrameHeight = 1920;
        private const int DefaultFrameRate = 60;
        private const int DefaultDuration = 30; // 30 seconds
        private const string DefaultOutputDir = "frames";
        
        static async Task<int> Main(string[] args)
        {
            // Setup command line options
            var rootCommand = new RootCommand("The Infinite Marble Run - Procedurally generated marble race simulator");
            
            var widthOption = new Option<int>(
                "--width",
                getDefaultValue: () => DefaultFrameWidth,
                description: "Width of the output video in pixels");
                
            var heightOption = new Option<int>(
                "--height",
                getDefaultValue: () => DefaultFrameHeight,
                description: "Height of the output video in pixels");
                
            var frameRateOption = new Option<int>(
                "--frame-rate",
                getDefaultValue: () => DefaultFrameRate,
                description: "Frame rate of the output video");
                
            var durationOption = new Option<int>(
                "--duration",
                getDefaultValue: () => DefaultDuration,
                description: "Duration of the animation in seconds");
                
            var outputDirOption = new Option<string>(
                "--output",
                getDefaultValue: () => DefaultOutputDir,
                description: "Output directory for frames");
                
            var seedOption = new Option<int?>(
                "--seed",
                getDefaultValue: () => null,
                description: "Random seed for the procedural generation (null for random)");
                
            var marbleCountOption = new Option<int>(
                "--marbles",
                getDefaultValue: () => 5,
                description: "Number of marbles to race (3-10)");
                
            var complexityOption = new Option<int>(
                "--complexity",
                getDefaultValue: () => 5,
                description: "Complexity of the generated course (1-10)");
                
            rootCommand.AddOption(widthOption);
            rootCommand.AddOption(heightOption);
            rootCommand.AddOption(frameRateOption);
            rootCommand.AddOption(durationOption);
            rootCommand.AddOption(outputDirOption);
            rootCommand.AddOption(seedOption);
            rootCommand.AddOption(marbleCountOption);
            rootCommand.AddOption(complexityOption);
            
            rootCommand.SetHandler(
                (int width, int height, int frameRate, int duration, string outputDir, int? seed, int marbles, int complexity) =>
                {
                    // Ensure output directory exists
                    Directory.CreateDirectory(outputDir);
                    
                    // Validate inputs
                    marbles = Math.Clamp(marbles, 3, 10);
                    complexity = Math.Clamp(complexity, 1, 10);
                    
                    // Initialize random seed
                    int actualSeed = seed ?? new Random().Next();
                    Console.WriteLine($"Using random seed: {actualSeed}");
                    
                    // Run the simulation
                    RunMarbleRun(
                        width, height, frameRate, duration, outputDir, 
                        actualSeed, marbles, complexity);
                },
                widthOption, heightOption, frameRateOption, durationOption, 
                outputDirOption, seedOption, marbleCountOption, complexityOption);
                
            return await rootCommand.InvokeAsync(args);
        }
        
        static void RunMarbleRun(
            int width, int height, int frameRate, int duration, string outputDir,
            int seed, int marbleCount, int complexity)
        {
            Console.WriteLine($"Creating The Infinite Marble Run");
            Console.WriteLine($"Resolution: {width}x{height} @ {frameRate}fps");
            Console.WriteLine($"Duration: {duration} seconds");
            Console.WriteLine($"Output: {outputDir}");
            Console.WriteLine($"Marbles: {marbleCount}");
            Console.WriteLine($"Complexity: {complexity}");
            
            try
            {
                // Create the simulation
                var random = new Random(seed);
                  // Generate fixed course instead of procedural one
                Console.WriteLine("Creating fixed course...");
                var courseGenerator = new FixedCourseGenerator(random, width, height);
                var course = courseGenerator.Generate();
                
                // Setup the simulation
                Console.WriteLine("Setting up physics simulation...");
                var simulation = new MarbleRunSimulation(course, width, height);
                
                // Create marbles
                Console.WriteLine($"Creating {marbleCount} marbles...");
                var marbleFactory = new MarbleFactory(random);
                var marbles = new List<Marble>();
                
                for (int i = 0; i < marbleCount; i++)
                {
                    var marble = marbleFactory.CreateRandomMarble(i, course.StartPosition);
                    marbles.Add(marble);
                    simulation.AddMarble(marble);
                }
                
                // Create renderer
                Console.WriteLine("Initializing renderer...");
                using var renderer = new MarbleRunRenderer(width, height, course, marbles);
                
                // Setup camera controller
                var cameraController = new DynamicCameraController(marbles, course, width, height);
                
                // Calculate total frames
                int totalFrames = duration * frameRate;
                
                // Main simulation and rendering loop
                Console.WriteLine($"Running simulation for {totalFrames} frames...");
                for (int i = 0; i < totalFrames; i++)
                {
                    // Update physics
                    simulation.Step(1.0f / frameRate);
                    
                    // Update camera
                    cameraController.Update(i, totalFrames);
                    var cameraState = cameraController.GetCameraState();
                      // Render frame
                    string framePath = System.IO.Path.Combine(outputDir, $"frame_{i:D6}.png");
                    renderer.RenderFrame(framePath, cameraState);
                    
                    // Progress reporting
                    if (i % frameRate == 0 || i == totalFrames - 1)
                    {
                        int seconds = i / frameRate;
                        Console.WriteLine($"Rendered {seconds}s/{duration}s ({i}/{totalFrames} frames)");
                    }
                }
                
                // Render final results screen
                string resultsPath = System.IO.Path.Combine(outputDir, $"frame_{totalFrames:D6}.png");
                renderer.RenderResultsScreen(resultsPath, simulation.GetRankings());
                
                Console.WriteLine($"Simulation completed successfully.");
                Console.WriteLine($"Frames saved to: {System.IO.Path.GetFullPath(outputDir)}");
                Console.WriteLine($"To create video: ffmpeg -framerate {frameRate} -i {outputDir}/frame_%06d.png -c:v libx264 -pix_fmt yuv420p -crf 18 output.mp4");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
