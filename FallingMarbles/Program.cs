using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Binding;
using System.IO;
using System.Collections.Generic;
using SkiaSharp;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Common;
using tainicom.Aether.Physics2D.Collision.Shapes;

namespace FallingMarbles
{
    /// <summary>
    /// Helper class for creating fixtures
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
            var polygon = new PolygonShape(vertices, density);
            
            // Create fixture
            var fixture = body.CreateFixture(polygon);
            
            return fixture;
        }
        
        public static Fixture CreateCircle(float radius, float density, Vector2 offset, Body body)
        {
            // Create circle shape
            var circle = new CircleShape(radius, density);
            circle.Position = offset;
            
            // Create fixture
            var fixture = body.CreateFixture(circle);
            
            return fixture;
        }
        
        public static Fixture CreatePolygon(Vertices vertices, float density, Body body)
        {
            // Create polygon shape
            var polygon = new PolygonShape(vertices, density);
            
            // Create fixture
            var fixture = body.CreateFixture(polygon);
            
            return fixture;
        }
    }
    
    /// <summary>
    /// Represents a marble in the simulation
    /// </summary>
    public class Marble
    {
        public Body Body { get; private set; }
        public float Radius { get; private set; }
        public SKColor Color { get; set; }

        public Marble(World world, Vector2 position, float radius, float density, SKColor color)
        {
            Radius = radius;
            Color = color;
            
            // Create the body
            Body = world.CreateBody();
            Body.BodyType = BodyType.Dynamic;
            Body.Position = position;
            
            // Add the circular fixture
            var fixture = FixtureFactory.CreateCircle(radius, density, Vector2.Zero, Body);
            
            // Set properties for better simulation
            fixture.Restitution = 0.8f; // Bounciness
            fixture.Friction = 0.3f;
        }
        
        public void Draw(SKCanvas canvas, float pixelRatio)
        {
            // Convert physics position to screen position
            var position = Body.Position * pixelRatio;
            var screenRadius = Radius * pixelRatio;
            
            using var paint = new SKPaint
            {
                Color = Color,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            
            canvas.DrawCircle(position.X, position.Y, screenRadius, paint);
            
            // Add a highlight effect
            using var highlightPaint = new SKPaint
            {
                Color = SKColors.White.WithAlpha(100),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            
            canvas.DrawCircle(position.X - screenRadius * 0.3f, position.Y - screenRadius * 0.3f, screenRadius * 0.4f, highlightPaint);
        }
    }
      /// <summary>
    /// Map element base class
    /// </summary>
    public abstract class MapElement
    {
        public Body Body { get; protected set; }
        public SKColor Color { get; set; }
        
        protected MapElement()
        {
            // Register this element with the global tracker
            MapElementTracker.RegisterElement(this);
        }
        
        public abstract void Draw(SKCanvas canvas, float pixelRatio);
    }
    
    /// <summary>
    /// Represents a wall or platform in the map
    /// </summary>
    public class Wall : MapElement
    {
        public Vector2 Start { get; private set; }
        public Vector2 End { get; private set; }
        public float Width { get; private set; }
        
        public Wall(World world, Vector2 start, Vector2 end, float width, SKColor color)
        {
            Start = start;
            End = end;
            Width = width;
            Color = color;
            
            // Calculate properties
            Vector2 direction = end - start;
            float length = direction.Length();
            float angle = (float)Math.Atan2(direction.Y, direction.X);
            
            // Create the body
            Body = world.CreateBody();
            Body.BodyType = BodyType.Static;
            Body.Position = (start + end) / 2; // Center position
            Body.Rotation = angle;
            
            // Create fixture
            var fixture = FixtureFactory.CreateRectangle(length, width, 1f, Vector2.Zero, Body);
            
            // Set properties
            fixture.Friction = 0.3f;
        }
        
        public override void Draw(SKCanvas canvas, float pixelRatio)
        {
            Vector2 screenStart = Start * pixelRatio;
            Vector2 screenEnd = End * pixelRatio;
            float screenWidth = Width * pixelRatio;
            
            using var path = new SKPath();
            
            // Calculate the perpendicular vector to create a rectangle
            Vector2 direction = screenEnd - screenStart;
            Vector2 perpendicular = new Vector2(-direction.Y, direction.X);
            perpendicular.Normalize();
            perpendicular *= screenWidth / 2;
            
            // Create the four corners
            var p1 = screenStart + perpendicular;
            var p2 = screenStart - perpendicular;
            var p3 = screenEnd - perpendicular;
            var p4 = screenEnd + perpendicular;
            
            // Create the path
            path.MoveTo(p1.X, p1.Y);
            path.LineTo(p2.X, p2.Y);
            path.LineTo(p3.X, p3.Y);
            path.LineTo(p4.X, p4.Y);
            path.Close();
            
            using var paint = new SKPaint
            {
                Color = Color,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            
            canvas.DrawPath(path, paint);
            
            // Draw outline
            using var outlinePaint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };
            
            canvas.DrawPath(path, outlinePaint);
        }
    }
    
    /// <summary>
    /// Represents a curved ramp in the map
    /// </summary>
    public class Ramp : MapElement
    {
        public Vector2 Start { get; private set; }
        public Vector2 End { get; private set; }
        public Vector2 ControlPoint { get; private set; }
        public float Width { get; private set; }
        
        public Ramp(World world, Vector2 start, Vector2 end, Vector2 controlPoint, float width, SKColor color, int segments = 10)
        {
            Start = start;
            End = end;
            ControlPoint = controlPoint;
            Width = width;
            Color = color;
            
            // Create the body
            Body = world.CreateBody();
            Body.BodyType = BodyType.Static;
            
            // Create a series of small line segments to approximate the curve
            for (int i = 0; i < segments; i++)
            {
                float t1 = i / (float)segments;
                float t2 = (i + 1) / (float)segments;
                
                Vector2 p1 = BezierPoint(t1);
                Vector2 p2 = BezierPoint(t2);
                
                // Create a small segment
                Vector2 dir = p2 - p1;
                float len = dir.Length();
                float angle = (float)Math.Atan2(dir.Y, dir.X);
                
                // Create fixture for this segment at proper position and rotation
                Vector2 center = (p1 + p2) / 2;
                
                // Create vertices for a box
                var vertices = new Vertices(4);
                float halfLen = len / 2;
                float halfWidth = width / 2;
                
                // Calculate rotated vertices
                float cos = (float)Math.Cos(angle);
                float sin = (float)Math.Sin(angle);
                
                vertices.Add(new Vector2(center.X + (-halfLen * cos - halfWidth * sin), center.Y + (-halfLen * sin + halfWidth * cos)));
                vertices.Add(new Vector2(center.X + (halfLen * cos - halfWidth * sin), center.Y + (halfLen * sin + halfWidth * cos)));
                vertices.Add(new Vector2(center.X + (halfLen * cos + halfWidth * sin), center.Y + (halfLen * sin - halfWidth * cos)));
                vertices.Add(new Vector2(center.X + (-halfLen * cos + halfWidth * sin), center.Y + (-halfLen * sin - halfWidth * cos)));
                
                // Create a fixture from these vertices
                var fixture = FixtureFactory.CreatePolygon(vertices, 1f, Body);
                
                // Set properties
                fixture.Friction = 0.1f; // Make it a bit slippery
            }
        }
        
        private Vector2 BezierPoint(float t)
        {
            // Quadratic Bezier curve formula
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            
            // B(t) = (1-t)²P₀ + 2(1-t)tP₁ + t²P₂
            Vector2 point = uu * Start + 2 * u * t * ControlPoint + tt * End;
            
            return point;
        }
        
        public override void Draw(SKCanvas canvas, float pixelRatio)
        {
            // Convert physics positions to screen positions
            var screenStart = Start * pixelRatio;
            var screenEnd = End * pixelRatio;
            var screenControl = ControlPoint * pixelRatio;
            float screenWidth = Width * pixelRatio;
            
            using var path = new SKPath();
            
            // Create a path for the curve
            path.MoveTo(screenStart.X, screenStart.Y);
            path.QuadTo(screenControl.X, screenControl.Y, screenEnd.X, screenEnd.Y);
            
            // Draw the path with a stroke width
            using var paint = new SKPaint
            {
                Color = Color,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = screenWidth,
                StrokeCap = SKStrokeCap.Round
            };
            
            canvas.DrawPath(path, paint);
        }
    }
    
    /// <summary>
    /// Represents a funnel/pinball-style obstacle
    /// </summary>
    public class PinballObstacle : MapElement
    {
        public Vector2 Position { get; private set; }
        public float Radius { get; private set; }
        
        public PinballObstacle(World world, Vector2 position, float radius, SKColor color)
        {
            Position = position;
            Radius = radius;
            Color = color;
            
            // Create the body
            Body = world.CreateBody();
            Body.BodyType = BodyType.Static;
            Body.Position = position;
            
            // Add the circular fixture
            var fixture = FixtureFactory.CreateCircle(radius, 1f, Vector2.Zero, Body);
            
            // Set properties for better simulation
            fixture.Restitution = 0.7f; // Make it bouncy
        }
        
        public override void Draw(SKCanvas canvas, float pixelRatio)
        {
            // Convert physics position to screen position
            var screenPos = Position * pixelRatio;
            float screenRadius = Radius * pixelRatio;
            
            using var paint = new SKPaint
            {
                Color = Color,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            
            canvas.DrawCircle(screenPos.X, screenPos.Y, screenRadius, paint);
            
            // Add a highlight effect
            using var highlightPaint = new SKPaint
            {
                Color = SKColors.White.WithAlpha(100),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            
            canvas.DrawCircle(screenPos.X - screenRadius * 0.3f, screenPos.Y - screenRadius * 0.3f, screenRadius * 0.3f, highlightPaint);
            
            // Add an outline
            using var outlinePaint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };
            
            canvas.DrawCircle(screenPos.X, screenPos.Y, screenRadius, outlinePaint);
        }
    }
    
    /// <summary>
    /// Represents the entire map with all its elements
    /// </summary>
    public class MarbleMap
    {
        private World _world;
        private List<MapElement> _elements;
        private List<Marble> _marbles;
        private int _width;
        private int _height;
        private float _pixelRatio;
          public MarbleMap(World world, int width, int height, float pixelRatio)
        {
            _world = world;
            _elements = new List<MapElement>();
            _marbles = new List<Marble>();
            _width = width;
            _height = height;
            _pixelRatio = pixelRatio;
            
            InitializeMap();
        }
        
        /// <summary>
        /// Create a predefined map layout
        /// </summary>
        private void InitializeMap()
        {
            // Using the new modular map system
            ModularMapBuilder builder = new ModularMapBuilder(_world, _width, _height, _pixelRatio);
            
            // Create a grid layout with 5 columns and 10 rows (just like in the image)
            var elements = builder.CreateGridLayout(5, 10);
            _elements.AddRange(elements);
        }
        
        /// <summary>
        /// Create a map using a specific layout of section IDs
        /// </summary>
        /// <param name="layout">2D array of section IDs</param>
        public void CreateCustomMap(string[,] layout)
        {
            _elements.Clear();
            
            ModularMapBuilder builder = new ModularMapBuilder(_world, _width, _height, _pixelRatio);
            var elements = builder.CreateCustomLayout(layout);
            _elements.AddRange(elements);
        }
        
        /// <summary>
        /// Create a completely random map
        /// </summary>
        /// <param name="columns">Number of columns</param>
        /// <param name="rows">Number of rows</param>
        /// <param name="complexity">Complexity level from 0 (simple) to 1 (complex)</param>
        public void CreateRandomMap(int columns = 5, int rows = 10, float complexity = 0.5f)
        {
            _elements.Clear();
            
            ModularMapBuilder builder = new ModularMapBuilder(_world, _width, _height, _pixelRatio);
            var elements = builder.CreateRandomLayout(columns, rows, complexity);
            _elements.AddRange(elements);
        }
        
        public void AddMarble(Vector2 position, float radius, SKColor color)
        {
            _marbles.Add(new Marble(_world, position, radius, 1f, color));
        }
        
        public void AddRandomMarbles(int count, Random random)
        {
            float worldWidth = _width / _pixelRatio;
            float marbleRadius = worldWidth * 0.015f; // Small marbles
            
            // Define a spawn area at the top center
            float spawnWidth = worldWidth * 0.4f;
            float spawnCenterX = worldWidth / 2;
            float spawnY = marbleRadius * 2; // Just below top edge
            
            for (int i = 0; i < count; i++)
            {
                float x = spawnCenterX + (random.NextSingle() * 2 - 1) * spawnWidth / 2;
                float y = spawnY;
                
                // Create a random color
                byte r = (byte)random.Next(50, 250);
                byte g = (byte)random.Next(50, 250);
                byte b = (byte)random.Next(50, 250);
                
                AddMarble(new Vector2(x, y), marbleRadius, new SKColor(r, g, b));
            }
        }
        
        public void Draw(SKCanvas canvas)
        {
            // Draw background
            canvas.Clear(SKColors.LightBlue);
            
            // Draw all map elements
            foreach (var element in _elements)
            {
                element.Draw(canvas, _pixelRatio);
            }
            
            // Draw all marbles
            foreach (var marble in _marbles)
            {
                marble.Draw(canvas, _pixelRatio);
            }
        }
    }    class Program
    {
        // Physics world constants
        private const float TimeStep = 1.0f / 60.0f; // 60 fps
        private const int VelocityIterations = 8;
        private const int PositionIterations = 3;
        private const float PixelMetersRatio = 100.0f; // 100px = 1m in physics world

        // Constants for TikTok portrait video
        private const int FrameWidth = 1080;
        private const int FrameHeight = 1920;
          static int Main(string[] args)
        {
            // Manual argument parsing due to issues with System.CommandLine version
            if (args.Length > 0) {
                Dictionary<string, string> argDict = new Dictionary<string, string>();
                for (int i = 0; i < args.Length; i++) {
                    if (args[i].StartsWith("--") && i + 1 < args.Length && !args[i + 1].StartsWith("--")) {
                        argDict[args[i].Substring(2)] = args[i + 1];
                        i++;
                    }
                }
                
                // Parse arguments with default values
                int marbleCount = argDict.ContainsKey("marbles") ? int.Parse(argDict["marbles"]) : 50;
                int frames = argDict.ContainsKey("frames") ? int.Parse(argDict["frames"]) : 300; 
                string outputDir = argDict.ContainsKey("output") ? argDict["output"] : "frames";
                string mapType = argDict.ContainsKey("map-type") ? argDict["map-type"] : "random";
                int columns = argDict.ContainsKey("columns") ? int.Parse(argDict["columns"]) : 3;
                int rows = argDict.ContainsKey("rows") ? int.Parse(argDict["rows"]) : 5;
                float complexity = argDict.ContainsKey("complexity") ? float.Parse(argDict["complexity"]) : 0.5f;
                string layoutFile = argDict.ContainsKey("layout-file") ? argDict["layout-file"] : "";
                string saveLayout = argDict.ContainsKey("save-layout") ? argDict["save-layout"] : "";
                
                // Run the animation
                GenerateAnimation(marbleCount, frames, outputDir, mapType, columns, rows, complexity, layoutFile, saveLayout);
                return 0;
            }
            
            // Setup command line options if no arguments provided
            var rootCommand = new RootCommand("Generates a marble run animation.");
            
            var marbleCountOption = new Option<int>(
                "--marbles",
                getDefaultValue: () => 50,
                description: "Number of marbles to simulate");
            
            var framesOption = new Option<int>(
                "--frames",
                getDefaultValue: () => 300,
                description: "Number of frames to render");            var outputDirOption = new Option<string>(
                "--output",
                getDefaultValue: () => "frames",
                description: "Output directory for frames");
                  var mapTypeOption = new Option<string>(
                "--map-type",
                getDefaultValue: () => "grid",
                description: "Type of map to generate: 'grid', 'random', or 'custom'");
                
            var columnsOption = new Option<int>(
                "--columns",
                getDefaultValue: () => 5,
                description: "Number of columns in the map grid");
                
            var rowsOption = new Option<int>(
                "--rows",
                getDefaultValue: () => 10,
                description: "Number of rows in the map grid");
                
            var complexityOption = new Option<float>(
                "--complexity",
                getDefaultValue: () => 0.5f,
                description: "Complexity of the map (0.0 to 1.0)");
                
            var layoutFileOption = new Option<string>(
                "--layout-file",
                description: "Path to a layout file for custom maps (if map-type=custom)");
                
            var saveLayoutOption = new Option<string>(
                "--save-layout",
                description: "Save the generated layout to this file");
                  rootCommand.AddOption(marbleCountOption);
            rootCommand.AddOption(framesOption);
            rootCommand.AddOption(outputDirOption);
            rootCommand.AddOption(mapTypeOption);
            rootCommand.AddOption(columnsOption);
            rootCommand.AddOption(rowsOption);
            rootCommand.AddOption(complexityOption);
            rootCommand.AddOption(layoutFileOption);
            rootCommand.AddOption(saveLayoutOption);            // Let's be more careful with the command handler
            return rootCommand.Invoke(args);
            // Parsing is broken in this version. We'll implement command parsing manually at the beginning of Main method

            return rootCommand.Invoke(args);
        }
        
        /// <summary>
        /// Creates a random layout with the given dimensions and section IDs
        /// </summary>
        private static string[,] CreateRandomLayout(int rows, int columns, List<string> sectionIds)
        {
            string[,] layout = new string[rows, columns];
            Random layoutRandom = new Random();
            
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    layout[row, col] = sectionIds[layoutRandom.Next(sectionIds.Count)];
                }
            }
            
            return layout;
        }
        
        private static void GenerateAnimation(int marbleCount, int frames, string outputDir, string mapType, int columns, int rows, float complexity, string layoutFile, string saveLayout)
        {
            Console.WriteLine($"Creating marble run animation with {marbleCount} marbles over {frames} frames");
            Console.WriteLine($"Map type: {mapType} with {columns}x{rows} grid");
            Console.WriteLine($"Output directory: {outputDir}");
            
            if (mapType.ToLower() == "custom" && !string.IsNullOrEmpty(layoutFile))
            {
                Console.WriteLine($"Using layout from: {layoutFile}");
            }
            
            if (!string.IsNullOrEmpty(saveLayout))
            {
                Console.WriteLine($"Will save layout to: {saveLayout}");
            }
              // Ensure output directory exists
            Directory.CreateDirectory(outputDir);
            
            // Reset element tracker to clear any previous elements
            MapElementTracker.ClearElements();
            
            // Initialize physics world
            World world = new World(new Vector2(0, 9.8f)); // Normal gravity
            
            // Create the map
            MarbleMap map = new MarbleMap(world, FrameWidth, FrameHeight, PixelMetersRatio);
            
            // Configure the map based on the selected type
            switch (mapType.ToLower())
            {
                case "random":
                    map.CreateRandomMap(columns, rows, complexity);
                    break;                case "custom":
                    string[,] layout;
                    
                    // Initialize section factory to get all available sections
                    MapSectionFactory.Initialize();
                    var allSections = MapSectionFactory.GetAllSections();
                    List<string> sectionIds = new List<string>();
                    
                    // Collect all section IDs
                    foreach (var section in allSections)
                    {
                        sectionIds.Add(section.Id);
                    }
                    
                    // Check if we should load from a file
                    if (!string.IsNullOrEmpty(layoutFile) && File.Exists(layoutFile))
                    {
                        try
                        {
                            // Load layout from file
                            string[] lines = File.ReadAllLines(layoutFile);
                            int fileRows = lines.Length;
                            int fileCols = lines[0].Split(',').Length;
                            
                            layout = new string[fileRows, fileCols];
                            
                            for (int r = 0; r < fileRows; r++)
                            {
                                string[] parts = lines[r].Split(',');
                                for (int c = 0; c < fileCols && c < parts.Length; c++)
                                {
                                    layout[r, c] = parts[c].Trim();
                                }
                            }
                            
                            // Update rows and columns to match file
                            rows = fileRows;
                            columns = fileCols;
                            
                            Console.WriteLine($"Loaded layout from file: {fileRows}x{fileCols}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error loading layout: {ex.Message}");
                            Console.WriteLine("Falling back to random layout");
                            layout = CreateRandomLayout(rows, columns, sectionIds);
                        }
                    }
                    else
                    {
                        // Create a random layout
                        layout = CreateRandomLayout(rows, columns, sectionIds);
                    }
                    
                    // Save the layout if requested
                    if (!string.IsNullOrEmpty(saveLayout))
                    {
                        try
                        {
                            using (StreamWriter writer = new StreamWriter(saveLayout))
                            {
                                for (int r = 0; r < rows; r++)
                                {
                                    for (int c = 0; c < columns; c++)
                                    {
                                        writer.Write(layout[r, c]);
                                        if (c < columns - 1) writer.Write(",");
                                    }
                                    writer.WriteLine();
                                }
                            }
                            Console.WriteLine($"Layout saved to {saveLayout}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error saving layout: {ex.Message}");
                        }
                    }
                    
                    map.CreateCustomMap(layout);
                    break;
                    
                case "grid":
                default:
                    // Grid layout is already created by default in the MarbleMap constructor
                    break;
            }
            
            // Add marbles
            Random random = new Random();
            map.AddRandomMarbles(marbleCount, random);
            
            // Main simulation and rendering loop
            for (int frame = 0; frame < frames; frame++)
            {
                Console.WriteLine($"Rendering frame {frame + 1}/{frames}");
                
                // Step the physics simulation
                world.Step(TimeStep);
                
                // Create a new SKBitmap for this frame
                using SKBitmap bitmap = new SKBitmap(FrameWidth, FrameHeight);
                using SKCanvas canvas = new SKCanvas(bitmap);
                
                // Draw the map and all objects
                map.Draw(canvas);
                
                // Save the frame
                string framePath = System.IO.Path.Combine(outputDir, $"frame_{frame:D6}.png");
                using FileStream stream = System.IO.File.OpenWrite(framePath);
                
                // Encode and save the bitmap as PNG
                bitmap.Encode(SKEncodedImageFormat.Png, 100).SaveTo(stream);
            }
            
            Console.WriteLine("Animation complete!");
        }
    }
}
