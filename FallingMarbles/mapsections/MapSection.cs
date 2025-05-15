using System;
using System.Collections.Generic;
using SkiaSharp;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Common;

namespace FallingMarbles
{
    /// <summary>
    /// Represents a modular section of the marble map
    /// </summary>
    public class MapSection
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        private List<Action<World, float, float, float, float, float>> _elementFactories;
        
        public MapSection(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
            _elementFactories = new List<Action<World, float, float, float, float, float>>();
        }
        
        /// <summary>
        /// Add a function that creates a map element
        /// </summary>
        /// <param name="factory">Function that takes (world, x, y, width, height, pixelRatio) and creates elements</param>
        public void AddElementFactory(Action<World, float, float, float, float, float> factory)
        {
            _elementFactories.Add(factory);
        }
        
        /// <summary>
        /// Creates all the elements for this section and adds them to the world
        /// </summary>
        /// <param name="world">Physics world</param>
        /// <param name="x">Left position in world coordinates</param>
        /// <param name="y">Top position in world coordinates</param>
        /// <param name="width">Width in world coordinates</param>        /// <param name="height">Height in world coordinates</param>        /// <param name="pixelRatio">Conversion ratio from pixels to world units</param>
        /// <returns>List of created map elements</returns>
        public List<MapElement> CreateElements(World world, float x, float y, float width, float height, float pixelRatio)
        {
            List<MapElement> elements = new List<MapElement>();
            
            // Call all the element factories and store the returned elements
            foreach (var factory in _elementFactories)
            {
                // The factory will create the elements and register them in the world
                factory(world, x, y, width, height, pixelRatio);
                
                // Get the most recently created element and add it to our list
                if (MapElementTracker.Elements.Count > 0)
                {
                    elements.Add(MapElementTracker.Elements[MapElementTracker.Elements.Count - 1]);
                }
            }
            
            return elements;
        }
    }
    
    /// <summary>
    /// Factory for creating and managing map sections
    /// </summary>
    public static class MapSectionFactory
    {
        private static Dictionary<string, MapSection> _sections = new Dictionary<string, MapSection>();
        
        /// <summary>
        /// Initialize all map sections
        /// </summary>
        public static void Initialize()
        {
            // Create all map sections
            CreateBasicMapSections();
            CreateAdvancedMapSections();
        }
        
        /// <summary>
        /// Get a map section by its ID
        /// </summary>
        public static MapSection GetSection(string id)
        {
            if (_sections.TryGetValue(id, out MapSection section))
            {
                return section;
            }
            
            throw new ArgumentException($"Map section with ID '{id}' not found");
        }
        
        /// <summary>
        /// Get all available map sections
        /// </summary>
        public static IEnumerable<MapSection> GetAllSections()
        {
            return _sections.Values;
        }
        
        /// <summary>
        /// Register a new map section
        /// </summary>
        public static void RegisterSection(MapSection section)
        {
            _sections[section.Id] = section;
        }
        
        /// <summary>
        /// Create basic map sections (rows 1-2 from the image)
        /// </summary>
        private static void CreateBasicMapSections()
        {
            // Empty section for testing
            var emptySection = new MapSection("empty", "Empty Section", "An empty section with no obstacles");
            RegisterSection(emptySection);
            
            // Section with a single diagonal wall (top-left to bottom-right)
            var diagonalWall = new MapSection("diagonal_wall", "Diagonal Wall", "A section with a single diagonal wall");
            diagonalWall.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.5f;
                new Wall(
                    world,
                    new Vector2(x, y),
                    new Vector2(x + width, y + height),
                    wallThickness,
                    SKColors.DarkBlue
                );
            });
            RegisterSection(diagonalWall);
            
            // Section with horizontal lines
            var horizontalLines = new MapSection("horizontal_lines", "Horizontal Lines", "A section with horizontal lines");
            horizontalLines.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.3f;
                int lineCount = 4;
                
                for (int i = 0; i < lineCount; i++)
                {
                    float yPos = y + height * (i + 1) / (lineCount + 1);
                    new Wall(
                        world,
                        new Vector2(x, yPos),
                        new Vector2(x + width, yPos),
                        wallThickness,
                        SKColors.DeepPink
                    );
                }
            });
            RegisterSection(horizontalLines);
            
            // Section with vertical lines
            var verticalLines = new MapSection("vertical_lines", "Vertical Lines", "A section with vertical lines");
            verticalLines.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.3f;
                int lineCount = 4;
                
                for (int i = 0; i < lineCount; i++)
                {
                    float xPos = x + width * (i + 1) / (lineCount + 1);
                    new Wall(
                        world,
                        new Vector2(xPos, y),
                        new Vector2(xPos, y + height),
                        wallThickness,
                        SKColors.DeepPink
                    );
                }
            });
            RegisterSection(verticalLines);
            
            // Section with dots (pinball obstacles)
            var dotsSection = new MapSection("dots", "Dots", "A section with dot obstacles");
            dotsSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                int rows = 3;
                int cols = 3;
                float radius = Math.Min(width, height) * 0.08f;
                
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        float xPos = x + width * (col + 1) / (cols + 1);
                        float yPos = y + height * (row + 1) / (rows + 1);
                        
                        new PinballObstacle(
                            world,
                            new Vector2(xPos, yPos),
                            radius,
                            SKColors.MediumSeaGreen
                        );
                    }
                }
            });
            RegisterSection(dotsSection);
            
            // Section with zigzag pattern
            var zigzagSection = new MapSection("zigzag", "Zigzag", "A section with a zigzag pattern");
            zigzagSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.3f;
                int segments = 4;
                
                for (int i = 0; i < segments; i++)
                {
                    float y1 = y + height * i / segments;
                    float y2 = y + height * (i + 1) / segments;
                    
                    if (i % 2 == 0)
                    {
                        new Wall(
                            world,
                            new Vector2(x, y1),
                            new Vector2(x + width, y2),
                            wallThickness,
                            SKColors.DarkOrange
                        );
                    }
                    else
                    {
                        new Wall(
                            world,
                            new Vector2(x + width, y1),
                            new Vector2(x, y2),
                            wallThickness,
                            SKColors.DarkOrange
                        );
                    }
                }
            });
            RegisterSection(zigzagSection);
            
            // Section with spiral
            var spiralSection = new MapSection("spiral", "Spiral", "A section with a spiral pattern");
            spiralSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.3f;
                
                // Create a spiral with multiple small segments
                int turns = 2;
                int segmentsPerTurn = 12;
                float centerX = x + width / 2;
                float centerY = y + height / 2;
                float maxRadius = Math.Min(width, height) * 0.45f;
                
                Vector2 prevPoint = new Vector2();
                
                for (int i = 0; i <= turns * segmentsPerTurn; i++)
                {
                    float angle = (float)(i * 2 * Math.PI / segmentsPerTurn);
                    float radius = maxRadius * (1 - i / (float)(turns * segmentsPerTurn));
                    
                    Vector2 currentPoint = new Vector2(
                        centerX + radius * (float)Math.Cos(angle),
                        centerY + radius * (float)Math.Sin(angle)
                    );
                    
                    if (i > 0)
                    {
                        new Wall(
                            world,
                            prevPoint,
                            currentPoint,
                            wallThickness,
                            SKColors.Purple
                        );
                    }
                    
                    prevPoint = currentPoint;
                }
            });
            RegisterSection(spiralSection);
            
            // Section with sine wave
            var sineWaveSection = new MapSection("sine_wave", "Sine Wave", "A section with a sine wave pattern");
            sineWaveSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.3f;
                int segments = 20;
                
                Vector2 prevPoint = new Vector2();
                
                for (int i = 0; i <= segments; i++)
                {
                    float xPos = x + width * i / segments;
                    float yPos = y + height / 2 + (float)Math.Sin(i * 2 * Math.PI / (segments / 2)) * height / 3;
                    
                    Vector2 currentPoint = new Vector2(xPos, yPos);
                    
                    if (i > 0)
                    {
                        new Wall(
                            world,
                            prevPoint,
                            currentPoint,
                            wallThickness,
                            SKColors.RoyalBlue
                        );
                    }
                    
                    prevPoint = currentPoint;
                }
            });
            RegisterSection(sineWaveSection);
        }
        
        /// <summary>
        /// Create more complex map sections (rows 3-5 from the image)
        /// </summary>
        private static void CreateAdvancedMapSections()
        {
            // Funnel section
            var funnelSection = new MapSection("funnel", "Funnel", "A section with a funnel shape");
            funnelSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.4f;
                
                // Left side of funnel
                new Wall(
                    world,
                    new Vector2(x, y),
                    new Vector2(x + width / 2 - width * 0.1f, y + height),
                    wallThickness,
                    SKColors.MediumPurple
                );
                
                // Right side of funnel
                new Wall(
                    world,
                    new Vector2(x + width, y),
                    new Vector2(x + width / 2 + width * 0.1f, y + height),
                    wallThickness,
                    SKColors.MediumPurple
                );
            });
            RegisterSection(funnelSection);
            
            // Cross pattern
            var crossSection = new MapSection("cross", "Cross", "A section with a cross pattern");
            crossSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.3f;
                
                // Horizontal line
                new Wall(
                    world,
                    new Vector2(x, y + height / 2),
                    new Vector2(x + width, y + height / 2),
                    wallThickness,
                    SKColors.Crimson
                );
                
                // Vertical line
                new Wall(
                    world,
                    new Vector2(x + width / 2, y),
                    new Vector2(x + width / 2, y + height),
                    wallThickness,
                    SKColors.Crimson
                );
            });
            RegisterSection(crossSection);
            
            // Pinball bumpers
            var pinballSection = new MapSection("pinball", "Pinball", "A section with pinball-style bumpers");
            pinballSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                // Large center bumper
                new PinballObstacle(
                    world,
                    new Vector2(x + width / 2, y + height / 2),
                    Math.Min(width, height) * 0.2f,
                    SKColors.DarkGreen
                );
                
                // Smaller bumpers around it
                int bumperCount = 4;
                float smallRadius = Math.Min(width, height) * 0.08f;
                
                for (int i = 0; i < bumperCount; i++)
                {
                    float angle = i * 2 * (float)Math.PI / bumperCount;
                    float bumperX = x + width / 2 + (float)Math.Cos(angle) * width * 0.3f;
                    float bumperY = y + height / 2 + (float)Math.Sin(angle) * height * 0.3f;
                    
                    new PinballObstacle(
                        world,
                        new Vector2(bumperX, bumperY),
                        smallRadius,
                        SKColors.ForestGreen
                    );
                }
            });
            RegisterSection(pinballSection);
            
            // Wave ramp
            var waveRampSection = new MapSection("wave_ramp", "Wave Ramp", "A section with a wavy ramp");
            waveRampSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.3f;
                
                // Create a wave-like ramp with multiple curved segments
                int segments = 3;
                
                for (int i = 0; i < segments; i++)
                {
                    float startX = x + width * i / segments;
                    float endX = x + width * (i + 1) / segments;
                    float midX = (startX + endX) / 2;
                    
                    float startY, endY, controlY;
                    
                    if (i % 2 == 0)
                    {
                        startY = y + height * 0.25f;
                        endY = y + height * 0.25f;
                        controlY = y + height * 0.75f;
                    }
                    else
                    {
                        startY = y + height * 0.75f;
                        endY = y + height * 0.75f;
                        controlY = y + height * 0.25f;
                    }
                    
                    new Ramp(
                        world,
                        new Vector2(startX, startY),
                        new Vector2(endX, endY),
                        new Vector2(midX, controlY),
                        wallThickness,
                        SKColors.CornflowerBlue,
                        10
                    );
                }
            });
            RegisterSection(waveRampSection);
              // Stairs section
            var stairsSection = new MapSection("stairs", "Stairs", "A section with stair-like platforms");
            stairsSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.3f;
                int steps = 5;
                
                for (int i = 0; i < steps; i++)
                {
                    float stepX = x + width * i / steps;
                    float stepY = y + height * i / steps;
                    float stepWidth = width / steps;
                    
                    new Wall(
                        world,
                        new Vector2(stepX, stepY),
                        new Vector2(stepX + stepWidth, stepY),
                        wallThickness,
                        SKColors.SandyBrown
                    );
                }
            });
            RegisterSection(stairsSection);
            
            // Create more complex map sections from the remaining image patterns
            CreateMoreSections();
        }
        
        /// <summary>
        /// Create more map sections based on image patterns (rows 3-5+)
        /// </summary>
        private static void CreateMoreSections()
        {
            // Windmill pattern
            var windmillSection = new MapSection("windmill", "Windmill", "A section with a windmill pattern");
            windmillSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.3f;
                int arms = 4;
                float centerX = x + width / 2;
                float centerY = y + height / 2;
                float armLength = Math.Min(width, height) * 0.4f;
                
                for (int i = 0; i < arms; i++)
                {
                    float angle = i * 2 * (float)Math.PI / arms;
                    float endX = centerX + armLength * (float)Math.Cos(angle);
                    float endY = centerY + armLength * (float)Math.Sin(angle);
                    
                    new Wall(
                        world,
                        new Vector2(centerX, centerY),
                        new Vector2(endX, endY),
                        wallThickness,
                        SKColors.SlateBlue
                    );
                }
            });
            RegisterSection(windmillSection);
            
            // Checkerboard pattern
            var checkerboardSection = new MapSection("checkerboard", "Checkerboard", "A section with a checkerboard pattern");
            checkerboardSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                int rows = 3;
                int cols = 3;
                float boxWidth = width / cols;
                float boxHeight = height / rows;
                
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        // Only place obstacles on alternating squares
                        if ((row + col) % 2 == 0)
                        {
                            float obsX = x + boxWidth * (col + 0.5f);
                            float obsY = y + boxHeight * (row + 0.5f);
                            float radius = Math.Min(boxWidth, boxHeight) * 0.3f;
                            
                            new PinballObstacle(
                                world,
                                new Vector2(obsX, obsY),
                                radius,
                                SKColors.MediumOrchid
                            );
                        }
                    }
                }
            });
            RegisterSection(checkerboardSection);
            
            // Maze pattern
            var mazeSection = new MapSection("maze", "Maze", "A section with a simple maze pattern");
            mazeSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.25f;
                
                // Horizontal walls
                new Wall(
                    world,
                    new Vector2(x + width * 0.25f, y + height * 0.25f),
                    new Vector2(x + width * 0.75f, y + height * 0.25f),
                    wallThickness,
                    SKColors.DarkGoldenrod
                );
                
                new Wall(
                    world,
                    new Vector2(x + width * 0.25f, y + height * 0.75f),
                    new Vector2(x + width * 0.75f, y + height * 0.75f),
                    wallThickness,
                    SKColors.DarkGoldenrod
                );
                
                // Vertical walls
                new Wall(
                    world,
                    new Vector2(x + width * 0.25f, y + height * 0.25f),
                    new Vector2(x + width * 0.25f, y + height * 0.5f),
                    wallThickness,
                    SKColors.DarkGoldenrod
                );
                
                new Wall(
                    world,
                    new Vector2(x + width * 0.75f, y + height * 0.5f),
                    new Vector2(x + width * 0.75f, y + height * 0.75f),
                    wallThickness,
                    SKColors.DarkGoldenrod
                );
            });
            RegisterSection(mazeSection);
            
            // Ripples pattern
            var ripplesSection = new MapSection("ripples", "Ripples", "A section with concentric circles");
            ripplesSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.2f;
                int rings = 3;
                float maxRadius = Math.Min(width, height) * 0.45f;
                float centerX = x + width / 2;
                float centerY = y + height / 2;
                
                for (int i = 1; i <= rings; i++)
                {
                    float radius = maxRadius * i / rings;
                    int segments = 24; // More segments for smoother circles
                    Vector2 prevPoint = new Vector2();
                    
                    for (int j = 0; j <= segments; j++)
                    {
                        float angle = j * 2 * (float)Math.PI / segments;
                        Vector2 currentPoint = new Vector2(
                            centerX + radius * (float)Math.Cos(angle),
                            centerY + radius * (float)Math.Sin(angle)
                        );
                        
                        if (j > 0)
                        {
                            new Wall(
                                world,
                                prevPoint,
                                currentPoint,
                                wallThickness,
                                SKColors.DodgerBlue
                            );
                        }
                        
                        prevPoint = currentPoint;
                    }
                }
            });
            RegisterSection(ripplesSection);
            
            // ZigZag Vertical pattern
            var zigzagVerticalSection = new MapSection("zigzag_vertical", "Vertical ZigZag", "A section with a vertical zigzag pattern");
            zigzagVerticalSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.3f;
                int segments = 4;
                
                for (int i = 0; i < segments; i++)
                {
                    float x1 = x + width * i / segments;
                    float x2 = x + width * (i + 1) / segments;
                    
                    if (i % 2 == 0)
                    {
                        new Wall(
                            world,
                            new Vector2(x1, y),
                            new Vector2(x2, y + height),
                            wallThickness,
                            SKColors.OrangeRed
                        );
                    }
                    else
                    {
                        new Wall(
                            world,
                            new Vector2(x1, y + height),
                            new Vector2(x2, y),
                            wallThickness,
                            SKColors.OrangeRed
                        );
                    }
                }
            });
            RegisterSection(zigzagVerticalSection);
            
            // Hourglass pattern
            var hourglassSection = new MapSection("hourglass", "Hourglass", "A section with an hourglass pattern");
            hourglassSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.3f;
                
                // Left side
                new Wall(
                    world,
                    new Vector2(x, y),
                    new Vector2(x + width / 2, y + height / 2),
                    wallThickness,
                    SKColors.Firebrick
                );
                
                new Wall(
                    world,
                    new Vector2(x, y + height),
                    new Vector2(x + width / 2, y + height / 2),
                    wallThickness,
                    SKColors.Firebrick
                );
                
                // Right side
                new Wall(
                    world,
                    new Vector2(x + width, y),
                    new Vector2(x + width / 2, y + height / 2),
                    wallThickness,
                    SKColors.Firebrick
                );
                
                new Wall(
                    world,
                    new Vector2(x + width, y + height),
                    new Vector2(x + width / 2, y + height / 2),
                    wallThickness,
                    SKColors.Firebrick
                );
            });
            RegisterSection(hourglassSection);
            
            // Diamond pattern
            var diamondSection = new MapSection("diamond", "Diamond", "A section with a diamond pattern");
            diamondSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.3f;
                
                // Create diamond shape
                new Wall(
                    world,
                    new Vector2(x + width / 2, y),
                    new Vector2(x + width, y + height / 2),
                    wallThickness,
                    SKColors.DarkViolet
                );
                
                new Wall(
                    world,
                    new Vector2(x + width, y + height / 2),
                    new Vector2(x + width / 2, y + height),
                    wallThickness,
                    SKColors.DarkViolet
                );
                
                new Wall(
                    world,
                    new Vector2(x + width / 2, y + height),
                    new Vector2(x, y + height / 2),
                    wallThickness,
                    SKColors.DarkViolet
                );
                
                new Wall(
                    world,
                    new Vector2(x, y + height / 2),
                    new Vector2(x + width / 2, y),
                    wallThickness,
                    SKColors.DarkViolet
                );
            });
            RegisterSection(diamondSection);
            
            // Pendulum obstacles
            var pendulumSection = new MapSection("pendulum", "Pendulum", "A section with a pendulum-like obstacle pattern");
            pendulumSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.3f;
                float pendulumLength = height * 0.8f;
                float bobRadius = width * 0.15f;
                
                // Pendulum string
                new Wall(
                    world,
                    new Vector2(x + width / 2, y),
                    new Vector2(x + width / 2, y + pendulumLength),
                    wallThickness / 2,
                    SKColors.Silver
                );
                
                // Pendulum bob
                new PinballObstacle(
                    world,
                    new Vector2(x + width / 2, y + pendulumLength),
                    bobRadius,
                    SKColors.IndianRed
                );
            });
            RegisterSection(pendulumSection);
            
            // Grid Lines pattern
            var gridLinesSection = new MapSection("grid_lines", "Grid Lines", "A section with a grid of lines");
            gridLinesSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.2f;
                int horizontalLines = 3;
                int verticalLines = 3;
                
                // Horizontal lines
                for (int i = 1; i < horizontalLines; i++)
                {
                    float yPos = y + height * i / horizontalLines;
                    
                    new Wall(
                        world,
                        new Vector2(x, yPos),
                        new Vector2(x + width, yPos),
                        wallThickness,
                        SKColors.LightSeaGreen
                    );
                }
                
                // Vertical lines
                for (int i = 1; i < verticalLines; i++)
                {
                    float xPos = x + width * i / verticalLines;
                    
                    new Wall(
                        world,
                        new Vector2(xPos, y),
                        new Vector2(xPos, y + height),
                        wallThickness,
                        SKColors.LightSeaGreen
                    );
                }
            });
            RegisterSection(gridLinesSection);
            
            // Arrow pattern
            var arrowSection = new MapSection("arrow", "Arrow", "A section with an arrow pattern");
            arrowSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.3f;
                
                // Shaft
                new Wall(
                    world,
                    new Vector2(x + width / 2, y + height * 0.1f),
                    new Vector2(x + width / 2, y + height * 0.8f),
                    wallThickness,
                    SKColors.DarkOrange
                );
                
                // Left head
                new Wall(
                    world,
                    new Vector2(x + width / 2, y + height * 0.8f),
                    new Vector2(x + width * 0.2f, y + height * 0.5f),
                    wallThickness,
                    SKColors.DarkOrange
                );
                
                // Right head
                new Wall(
                    world,
                    new Vector2(x + width / 2, y + height * 0.8f),
                    new Vector2(x + width * 0.8f, y + height * 0.5f),
                    wallThickness,
                    SKColors.DarkOrange
                );
            });
            RegisterSection(arrowSection);
            
            // Wave pattern
            var wavePatternSection = new MapSection("wave_pattern", "Wave Pattern", "A section with a wave pattern");
            wavePatternSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.25f;
                int segments = 30;
                
                Vector2 prevPoint = new Vector2();
                
                for (int i = 0; i <= segments; i++)
                {
                    float xPos = x + width * i / segments;
                    float yPos = y + height / 2 + (float)Math.Sin(i * 4 * Math.PI / segments) * height / 5;
                    
                    Vector2 currentPoint = new Vector2(xPos, yPos);
                    
                    if (i > 0)
                    {
                        new Wall(
                            world,
                            prevPoint,
                            currentPoint,
                            wallThickness,
                            SKColors.MediumTurquoise
                        );
                    }
                    
                    prevPoint = currentPoint;
                }
            });
            RegisterSection(wavePatternSection);
            
            // Stepping stones 
            var steppingStonesSection = new MapSection("stepping_stones", "Stepping Stones", "A section with a series of platforms like stepping stones");
            steppingStonesSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.3f;
                int stones = 5;
                float stoneWidth = width * 0.15f;
                
                for (int i = 0; i < stones; i++)
                {
                    float stoneX = x + width * (i + 0.5f) / stones - stoneWidth / 2;
                    float stoneY = y + height * ((i % 2 == 0) ? 0.3f : 0.7f);
                    
                    new Wall(
                        world,
                        new Vector2(stoneX, stoneY),
                        new Vector2(stoneX + stoneWidth, stoneY),
                        wallThickness,
                        SKColors.LightSlateGray
                    );
                }
            });
            RegisterSection(steppingStonesSection);
            
            // Swirl section
            var swirlSection = new MapSection("swirl", "Swirl", "A section with a swirl pattern");
            swirlSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.25f;
                int segments = 40;
                float maxRadius = Math.Min(width, height) * 0.45f;
                float centerX = x + width / 2;
                float centerY = y + height / 2;
                
                Vector2 prevPoint = new Vector2();
                
                for (int i = 0; i <= segments; i++)
                {
                    float angle = i * 4 * (float)Math.PI / segments;
                    float radius = maxRadius * i / segments;
                    
                    Vector2 currentPoint = new Vector2(
                        centerX + radius * (float)Math.Cos(angle),
                        centerY + radius * (float)Math.Sin(angle)
                    );
                    
                    if (i > 0)
                    {
                        new Wall(
                            world,
                            prevPoint,
                            currentPoint,
                            wallThickness,
                            SKColors.MediumVioletRed
                        );
                    }
                    
                    prevPoint = currentPoint;
                }
            });
            RegisterSection(swirlSection);
            
            // Vortex section
            var vortexSection = new MapSection("vortex", "Vortex", "A section with a vortex-like pattern");
            vortexSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.25f;
                int segments = 60;
                float maxRadius = Math.Min(width, height) * 0.45f;
                float centerX = x + width / 2;
                float centerY = y + height / 2;
                
                Vector2 prevPoint = new Vector2();
                
                for (int i = 0; i <= segments; i++)
                {
                    float angle = i * 6 * (float)Math.PI / segments;
                    float radius = maxRadius * (1 - i / (float)segments);
                    
                    Vector2 currentPoint = new Vector2(
                        centerX + radius * (float)Math.Cos(angle),
                        centerY + radius * (float)Math.Sin(angle)
                    );
                    
                    if (i > 0)
                    {
                        new Wall(
                            world,
                            prevPoint,
                            currentPoint,
                            wallThickness,
                            SKColors.DarkCyan
                        );
                    }
                    
                    prevPoint = currentPoint;
                }
            });
            RegisterSection(vortexSection);
            
            // Parallel curves
            var parallelCurvesSection = new MapSection("parallel_curves", "Parallel Curves", "A section with parallel curved lines");
            parallelCurvesSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.2f;
                int curves = 3;
                int segments = 15;
                
                for (int c = 0; c < curves; c++)
                {
                    Vector2 prevPoint = new Vector2();
                    float offset = height * c / (curves - 1);
                    
                    for (int i = 0; i <= segments; i++)
                    {
                        float xPos = x + width * i / segments;
                        float yPos = y + offset + (float)Math.Sin(i * Math.PI / segments) * height * 0.15f;
                        
                        Vector2 currentPoint = new Vector2(xPos, yPos);
                        
                        if (i > 0)
                        {
                            new Wall(
                                world,
                                prevPoint,
                                currentPoint,
                                wallThickness,
                                SKColors.CornflowerBlue
                            );
                        }
                        
                        prevPoint = currentPoint;
                    }
                }
            });
            RegisterSection(parallelCurvesSection);
            
            // Honeycomb pattern
            var honeycombSection = new MapSection("honeycomb", "Honeycomb", "A section with a honeycomb pattern");
            honeycombSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.2f;
                float hexRadius = Math.Min(width, height) * 0.2f;
                float centerX = x + width / 2;
                float centerY = y + height / 2;
                
                // Draw a central hexagon
                int sides = 6;
                Vector2 first = new Vector2();
                Vector2 prev = new Vector2();
                
                for (int i = 0; i <= sides; i++)
                {
                    float angle = i * 2 * (float)Math.PI / sides;
                    Vector2 current = new Vector2(
                        centerX + hexRadius * (float)Math.Cos(angle),
                        centerY + hexRadius * (float)Math.Sin(angle)
                    );
                    
                    if (i == 0)
                    {
                        first = current;
                    }
                    else
                    {
                        new Wall(
                            world,
                            prev,
                            current,
                            wallThickness,
                            SKColors.Goldenrod
                        );
                    }
                    
                    prev = current;
                }
            });
            RegisterSection(honeycombSection);
            
            // S-curve section
            var sCurveSection = new MapSection("s_curve", "S-Curve", "A section with an S-shaped curve");
            sCurveSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.3f;
                
                // Create S-curve with two Bezier curves
                Vector2 start = new Vector2(x, y + height * 0.25f);
                Vector2 control1 = new Vector2(x + width * 0.5f, y);
                Vector2 mid = new Vector2(x + width * 0.5f, y + height * 0.5f);
                Vector2 control2 = new Vector2(x + width * 0.5f, y + height);
                Vector2 end = new Vector2(x + width, y + height * 0.75f);
                
                // First curve (top half)
                new Ramp(
                    world,
                    start,
                    mid,
                    control1,
                    wallThickness,
                    SKColors.SeaGreen,
                    15
                );
                
                // Second curve (bottom half)
                new Ramp(
                    world,
                    mid,
                    end,
                    control2,
                    wallThickness,
                    SKColors.SeaGreen,
                    15
                );
            });
            RegisterSection(sCurveSection);
            
            // Archway section
            var archwaySection = new MapSection("archway", "Archway", "A section with an arch");
            archwaySection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.3f;
                
                // Draw the arch (semicircle on top of "legs")
                float archWidth = width * 0.7f;
                float archHeight = height * 0.6f;
                float legHeight = height * 0.4f;
                
                // Left leg
                new Wall(
                    world,
                    new Vector2(x + (width - archWidth) / 2, y + height - legHeight),
                    new Vector2(x + (width - archWidth) / 2, y + height),
                    wallThickness,
                    SKColors.DimGray
                );
                
                // Right leg
                new Wall(
                    world,
                    new Vector2(x + (width + archWidth) / 2, y + height - legHeight),
                    new Vector2(x + (width + archWidth) / 2, y + height),
                    wallThickness,
                    SKColors.DimGray
                );
                
                // Arch (semicircle)
                int segments = 10;
                Vector2 prevPoint = new Vector2(x + (width - archWidth) / 2, y + height - legHeight);
                
                for (int i = 0; i <= segments; i++)
                {
                    float angle = (float)Math.PI * i / segments;
                    float xPos = x + width / 2 - archWidth / 2 * (float)Math.Cos(angle);
                    float yPos = y + height - legHeight - archHeight * (float)Math.Sin(angle);
                    
                    Vector2 currentPoint = new Vector2(xPos, yPos);
                    
                    new Wall(
                        world,
                        prevPoint,
                        currentPoint,
                        wallThickness,
                        SKColors.DimGray
                    );
                    
                    prevPoint = currentPoint;
                }
            });
            RegisterSection(archwaySection);
            
            // Butterfly pattern
            var butterflySection = new MapSection("butterfly", "Butterfly", "A section with a butterfly-inspired pattern");
            butterflySection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.25f;
                
                // Central body
                new Wall(
                    world,
                    new Vector2(x + width / 2, y + height * 0.3f),
                    new Vector2(x + width / 2, y + height * 0.7f),
                    wallThickness,
                    SKColors.HotPink
                );
                
                // Wings (created with Bezier curves)
                // Left wing
                new Ramp(
                    world,
                    new Vector2(x + width / 2, y + height * 0.4f),
                    new Vector2(x + width * 0.1f, y + height * 0.5f),
                    new Vector2(x + width * 0.2f, y + height * 0.2f),
                    wallThickness,
                    SKColors.HotPink,
                    15
                );
                
                new Ramp(
                    world,
                    new Vector2(x + width / 2, y + height * 0.6f),
                    new Vector2(x + width * 0.1f, y + height * 0.5f),
                    new Vector2(x + width * 0.2f, y + height * 0.8f),
                    wallThickness,
                    SKColors.HotPink,
                    15
                );
                
                // Right wing
                new Ramp(
                    world,
                    new Vector2(x + width / 2, y + height * 0.4f),
                    new Vector2(x + width * 0.9f, y + height * 0.5f),
                    new Vector2(x + width * 0.8f, y + height * 0.2f),
                    wallThickness,
                    SKColors.HotPink,
                    15
                );
                
                new Ramp(
                    world,
                    new Vector2(x + width / 2, y + height * 0.6f),
                    new Vector2(x + width * 0.9f, y + height * 0.5f),
                    new Vector2(x + width * 0.8f, y + height * 0.8f),
                    wallThickness,
                    SKColors.HotPink,
                    15
                );
            });
            RegisterSection(butterflySection);
            
            // Split section (vertical barrier in middle)
            var splitSection = new MapSection("split", "Split", "A section with a vertical barrier in the middle");
            splitSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.4f;
                float gapSize = height * 0.3f;
                float barrierHeight = (height - gapSize) / 2;
                
                // Top part of barrier
                new Wall(
                    world,
                    new Vector2(x + width / 2, y),
                    new Vector2(x + width / 2, y + barrierHeight),
                    wallThickness,
                    SKColors.Coral
                );
                
                // Bottom part of barrier
                new Wall(
                    world,
                    new Vector2(x + width / 2, y + barrierHeight + gapSize),
                    new Vector2(x + width / 2, y + height),
                    wallThickness,
                    SKColors.Coral
                );
            });
            RegisterSection(splitSection);
            
            // Pinball plunger
            var pinballPlungerSection = new MapSection("pinball_plunger", "Pinball Plunger", "A section resembling a pinball plunger");
            pinballPlungerSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.4f;
                float plungerWidth = width * 0.2f;
                
                // Plunger shaft
                new Wall(
                    world,
                    new Vector2(x + width / 2, y + height * 0.5f),
                    new Vector2(x + width / 2, y + height * 0.9f),
                    plungerWidth,
                    SKColors.Silver
                );
                
                // Plunger head
                new PinballObstacle(
                    world,
                    new Vector2(x + width / 2, y + height * 0.3f),
                    plungerWidth * 0.8f,
                    SKColors.Red
                );
                
                // Guide rails on sides
                new Wall(
                    world,
                    new Vector2(x + width * 0.2f, y),
                    new Vector2(x + width * 0.2f, y + height),
                    wallThickness,
                    SKColors.Silver
                );
                
                new Wall(
                    world,
                    new Vector2(x + width * 0.8f, y),
                    new Vector2(x + width * 0.8f, y + height),
                    wallThickness,
                    SKColors.Silver
                );
            });
            RegisterSection(pinballPlungerSection);
            
            // Flower pattern
            var flowerSection = new MapSection("flower", "Flower", "A section with a flower-like pattern");
            flowerSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float centerX = x + width / 2;
                float centerY = y + height / 2;
                float petals = 6;
                float petalSize = Math.Min(width, height) * 0.3f;
                float wallThickness = 0.2f;
                
                // Create petals
                for (int i = 0; i < petals; i++)
                {
                    float angle = i * 2 * (float)Math.PI / petals;
                    float petalX = centerX + petalSize * (float)Math.Cos(angle);
                    float petalY = centerY + petalSize * (float)Math.Sin(angle);
                    
                    // Draw a petal as a line from center to tip
                    new Wall(
                        world,
                        new Vector2(centerX, centerY),
                        new Vector2(petalX, petalY),
                        wallThickness,
                        SKColors.MediumOrchid
                    );
                }
                
                // Center of flower
                new PinballObstacle(
                    world,
                    new Vector2(centerX, centerY),
                    Math.Min(width, height) * 0.1f,
                    SKColors.Yellow
                );
            });
            RegisterSection(flowerSection);
            
            // Diamond Grid pattern
            var diamondGridSection = new MapSection("diamond_grid", "Diamond Grid", "A section with a grid of diamonds");
            diamondGridSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.2f;
                int rows = 2;
                int cols = 2;
                float diamondSize = Math.Min(width / cols, height / rows) * 0.5f;
                
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        float centerX = x + width * (col + 0.5f) / cols;
                        float centerY = y + height * (row + 0.5f) / rows;
                        
                        // Create diamond shape
                        new Wall(
                            world,
                            new Vector2(centerX, centerY - diamondSize),
                            new Vector2(centerX + diamondSize, centerY),
                            wallThickness,
                            SKColors.MediumPurple
                        );
                        
                        new Wall(
                            world,
                            new Vector2(centerX + diamondSize, centerY),
                            new Vector2(centerX, centerY + diamondSize),
                            wallThickness,
                            SKColors.MediumPurple
                        );
                        
                        new Wall(
                            world,
                            new Vector2(centerX, centerY + diamondSize),
                            new Vector2(centerX - diamondSize, centerY),
                            wallThickness,
                            SKColors.MediumPurple
                        );
                        
                        new Wall(
                            world,
                            new Vector2(centerX - diamondSize, centerY),
                            new Vector2(centerX, centerY - diamondSize),
                            wallThickness,
                            SKColors.MediumPurple
                        );
                    }
                }
            });
            RegisterSection(diamondGridSection);
            
            // Star pattern
            var starSection = new MapSection("star", "Star", "A section with a star pattern");
            starSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.25f;
                int points = 5;
                float outerRadius = Math.Min(width, height) * 0.45f;
                float innerRadius = outerRadius * 0.4f;
                float centerX = x + width / 2;
                float centerY = y + height / 2;
                
                Vector2 prevPoint = new Vector2();
                Vector2 firstPoint = new Vector2();
                
                for (int i = 0; i <= points * 2; i++)
                {
                    float radius = i % 2 == 0 ? outerRadius : innerRadius;
                    float angle = (float)Math.PI / 2 + i * (float)Math.PI / points;
                    
                    Vector2 currentPoint = new Vector2(
                        centerX + radius * (float)Math.Cos(angle),
                        centerY + radius * (float)Math.Sin(angle)
                    );
                    
                    if (i == 0)
                    {
                        firstPoint = currentPoint;
                    }
                    else
                    {
                        new Wall(
                            world,
                            prevPoint,
                            currentPoint,
                            wallThickness,
                            SKColors.Gold
                        );
                    }
                    
                    prevPoint = currentPoint;
                }
                
                // Close the star
                new Wall(
                    world,
                    prevPoint,
                    firstPoint,
                    wallThickness,
                    SKColors.Gold
                );
            });
            RegisterSection(starSection);
            
            // Lightning bolt pattern
            var lightningSection = new MapSection("lightning", "Lightning", "A section with a lightning bolt pattern");
            lightningSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.3f;
                
                // Create a zigzag line resembling lightning
                Vector2[] points = new Vector2[] {
                    new Vector2(x + width * 0.5f, y),
                    new Vector2(x + width * 0.2f, y + height * 0.3f),
                    new Vector2(x + width * 0.6f, y + height * 0.5f),
                    new Vector2(x + width * 0.3f, y + height * 0.7f),
                    new Vector2(x + width * 0.5f, y + height)
                };
                
                for (int i = 0; i < points.Length - 1; i++)
                {
                    new Wall(
                        world,
                        points[i],
                        points[i + 1],
                        wallThickness,
                        SKColors.Yellow
                    );
                }
            });
            RegisterSection(lightningSection);
            
            // Droplets pattern
            var dropletsSection = new MapSection("droplets", "Droplets", "A section with water droplet obstacles");
            dropletsSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                int droplets = 4;
                float radius = Math.Min(width, height) * 0.1f;
                
                for (int i = 0; i < droplets; i++)
                {
                    float dropX = x + width * (i + 0.5f) / droplets;
                    float dropY = y + height * ((i % 2 == 0) ? 0.3f : 0.7f);
                    
                    new PinballObstacle(
                        world,
                        new Vector2(dropX, dropY),
                        radius,
                        SKColors.SkyBlue
                    );
                }
            });
            RegisterSection(dropletsSection);
            
            // Web pattern
            var webSection = new MapSection("web", "Web", "A section with a spider web pattern");
            webSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.15f;
                int rings = 3;
                int spokes = 8;
                float centerX = x + width / 2;
                float centerY = y + height / 2;
                float maxRadius = Math.Min(width, height) * 0.45f;
                
                // Create rings
                for (int r = 1; r <= rings; r++)
                {
                    float radius = maxRadius * r / rings;
                    Vector2 prevPoint = new Vector2();
                    Vector2 firstPoint = new Vector2();
                    
                    for (int i = 0; i <= spokes; i++)
                    {
                        float angle = i * 2 * (float)Math.PI / spokes;
                        Vector2 currentPoint = new Vector2(
                            centerX + radius * (float)Math.Cos(angle),
                            centerY + radius * (float)Math.Sin(angle)
                        );
                        
                        if (i == 0)
                        {
                            firstPoint = currentPoint;
                        }
                        else
                        {
                            new Wall(
                                world,
                                prevPoint,
                                currentPoint,
                                wallThickness,
                                SKColors.Silver
                            );
                        }
                        
                        prevPoint = currentPoint;
                    }
                }
                
                // Create spokes
                for (int s = 0; s < spokes; s++)
                {
                    float angle = s * 2 * (float)Math.PI / spokes;
                    Vector2 outer = new Vector2(
                        centerX + maxRadius * (float)Math.Cos(angle),
                        centerY + maxRadius * (float)Math.Sin(angle)
                    );
                    
                    new Wall(
                        world,
                        new Vector2(centerX, centerY),
                        outer,
                        wallThickness,
                        SKColors.Silver
                    );
                }
            });
            RegisterSection(webSection);
            
            // Paw print pattern 
            var pawPrintSection = new MapSection("paw_print", "Paw Print", "A section with a paw print pattern of obstacles");
            pawPrintSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                // Main pad
                new PinballObstacle(
                    world,
                    new Vector2(x + width / 2, y + height * 0.6f),
                    Math.Min(width, height) * 0.2f,
                    SKColors.SaddleBrown
                );
                
                // Toe pads
                float toeRadius = Math.Min(width, height) * 0.08f;
                
                new PinballObstacle(
                    world,
                    new Vector2(x + width * 0.35f, y + height * 0.3f),
                    toeRadius,
                    SKColors.SaddleBrown
                );
                
                new PinballObstacle(
                    world,
                    new Vector2(x + width * 0.5f, y + height * 0.25f),
                    toeRadius,
                    SKColors.SaddleBrown
                );
                
                new PinballObstacle(
                    world,
                    new Vector2(x + width * 0.65f, y + height * 0.3f),
                    toeRadius,
                    SKColors.SaddleBrown
                );
            });
            RegisterSection(pawPrintSection);
            
            // Yin-Yang pattern
            var yinYangSection = new MapSection("yin_yang", "Yin-Yang", "A section with a yin-yang-inspired pattern");
            yinYangSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.2f;
                float radius = Math.Min(width, height) * 0.4f;
                float centerX = x + width / 2;
                float centerY = y + height / 2;
                
                // Create the outer circle
                int segments = 30;
                Vector2 prevPoint = new Vector2();
                Vector2 firstPoint = new Vector2();
                
                for (int i = 0; i <= segments; i++)
                {
                    float angle = i * 2 * (float)Math.PI / segments;
                    Vector2 currentPoint = new Vector2(
                        centerX + radius * (float)Math.Cos(angle),
                        centerY + radius * (float)Math.Sin(angle)
                    );
                    
                    if (i == 0)
                    {
                        firstPoint = currentPoint;
                    }
                    else
                    {
                        new Wall(
                            world,
                            prevPoint,
                            currentPoint,
                            wallThickness,
                            i <= segments / 2 ? SKColors.Black : SKColors.White
                        );
                    }
                    
                    prevPoint = currentPoint;
                }
                
                // Create the S-curve through the middle
                new Ramp(
                    world,
                    new Vector2(centerX - radius, centerY),
                    new Vector2(centerX + radius, centerY),
                    new Vector2(centerX, centerY + radius / 2),
                    wallThickness,
                    SKColors.Gray,
                    15
                );
                
                // Create the two dots
                new PinballObstacle(
                    world,
                    new Vector2(centerX - radius / 2, centerY),
                    radius * 0.15f,
                    SKColors.White
                );
                
                new PinballObstacle(
                    world,
                    new Vector2(centerX + radius / 2, centerY),
                    radius * 0.15f,
                    SKColors.Black
                );
            });
            RegisterSection(yinYangSection);
              // Labyrinth pattern
            var labyrinthSection = new MapSection("labyrinth", "Labyrinth", "A section with a simple labyrinth pattern");
            labyrinthSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.2f;
                float margin = width * 0.1f;
                
                // Outer wall
                float[] outerX = { x + margin, x + width - margin, x + width - margin, x + margin, x + margin };
                float[] outerY = { y + margin, y + margin, y + height - margin, y + height - margin, y + margin };
                
                for (int i = 0; i < outerX.Length - 1; i++)
                {
                    new Wall(
                        world,
                        new Vector2(outerX[i], outerY[i]),
                        new Vector2(outerX[i+1], outerY[i+1]),
                        wallThickness,
                        SKColors.DarkSlateGray
                    );
                }
                
                // Inner walls - horizontal
                new Wall(
                    world,
                    new Vector2(x + margin, y + height * 0.33f),
                    new Vector2(x + width * 0.66f, y + height * 0.33f),
                    wallThickness,
                    SKColors.DarkSlateGray
                );
                
                new Wall(
                    world,
                    new Vector2(x + width * 0.33f, y + height * 0.66f),
                    new Vector2(x + width - margin, y + height * 0.66f),
                    wallThickness,
                    SKColors.DarkSlateGray
                );
            });
            RegisterSection(labyrinthSection);
            
            // Here are the 20 additional sections to reach 50 total map sections
            CreateAdditionalSections();
        }
        
        /// <summary>
        /// Creates additional 20 map sections to reach 50 total sections
        /// </summary>
        private static void CreateAdditionalSections()
        {
            // Cascading platforms section
            var cascadingPlatformsSection = new MapSection("cascading_platforms", "Cascading Platforms", "A section with cascading platforms");
            cascadingPlatformsSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.3f;
                int platforms = 7;
                float platformWidth = width * 0.5f;
                
                for (int i = 0; i < platforms; i++)
                {
                    float platformX = i % 2 == 0 ? x : x + width - platformWidth;
                    float platformY = y + height * i / platforms;
                    
                    new Wall(
                        world,
                        new Vector2(platformX, platformY),
                        new Vector2(platformX + platformWidth, platformY),
                        wallThickness,
                        SKColors.RosyBrown
                    );
                }
            });
            RegisterSection(cascadingPlatformsSection);
            
            // Triangles pattern
            var trianglesSection = new MapSection("triangles", "Triangles", "A section with triangular obstacles");
            trianglesSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.25f;
                int triangleCount = 3;
                
                for (int i = 0; i < triangleCount; i++)
                {
                    float centerX = x + width * (i + 0.5f) / triangleCount;
                    float centerY = y + height * 0.5f;
                    float triangleHeight = height * 0.4f;
                    float triangleWidth = width * 0.2f;
                    
                    // Triangle sides
                    new Wall(
                        world,
                        new Vector2(centerX, centerY - triangleHeight/2),
                        new Vector2(centerX - triangleWidth/2, centerY + triangleHeight/2),
                        wallThickness,
                        SKColors.MediumSpringGreen
                    );
                    
                    new Wall(
                        world,
                        new Vector2(centerX - triangleWidth/2, centerY + triangleHeight/2),
                        new Vector2(centerX + triangleWidth/2, centerY + triangleHeight/2),
                        wallThickness,
                        SKColors.MediumSpringGreen
                    );
                    
                    new Wall(
                        world,
                        new Vector2(centerX + triangleWidth/2, centerY + triangleHeight/2),
                        new Vector2(centerX, centerY - triangleHeight/2),
                        wallThickness,
                        SKColors.MediumSpringGreen
                    );
                }
            });
            RegisterSection(trianglesSection);
            
            // Hexagon grid
            var hexagonGridSection = new MapSection("hexagon_grid", "Hexagon Grid", "A section with a grid of hexagons");
            hexagonGridSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.15f;
                
                // Create a grid of hexagons
                int rows = 2;
                int cols = 2;
                float hexRadius = Math.Min(width / (cols * 2), height / (rows * 2)) * 0.8f;
                
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        // Center of this hexagon
                        float centerX = x + width * (col + 0.5f) / cols;
                        float centerY = y + height * (row + 0.5f) / rows;
                        
                        // Draw the hexagon
                        int sides = 6;
                        Vector2 prevPoint = new Vector2();
                        Vector2 firstPoint = new Vector2();
                        
                        for (int i = 0; i <= sides; i++)
                        {
                            float angle = i * 2 * (float)Math.PI / sides;
                            Vector2 currentPoint = new Vector2(
                                centerX + hexRadius * (float)Math.Cos(angle),
                                centerY + hexRadius * (float)Math.Sin(angle)
                            );
                            
                            if (i == 0)
                            {
                                firstPoint = currentPoint;
                            }
                            else
                            {
                                new Wall(
                                    world,
                                    prevPoint,
                                    currentPoint,
                                    wallThickness,
                                    SKColors.Goldenrod
                                );
                            }
                            
                            prevPoint = currentPoint;
                        }
                    }
                }
            });
            RegisterSection(hexagonGridSection);
            
            // Double helix
            var doubleHelixSection = new MapSection("double_helix", "Double Helix", "A section with a double helix pattern");
            doubleHelixSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.2f;
                int segments = 30;
                
                Vector2 prevPoint1 = new Vector2();
                Vector2 prevPoint2 = new Vector2();
                
                for (int i = 0; i <= segments; i++)
                {
                    float xPos = x + width * i / segments;
                    float yPos1 = y + height / 2 + (float)Math.Sin(i * 4 * Math.PI / segments) * height / 3;
                    float yPos2 = y + height / 2 - (float)Math.Sin(i * 4 * Math.PI / segments) * height / 3;
                    
                    Vector2 currentPoint1 = new Vector2(xPos, yPos1);
                    Vector2 currentPoint2 = new Vector2(xPos, yPos2);
                    
                    if (i > 0)
                    {
                        new Wall(
                            world,
                            prevPoint1,
                            currentPoint1,
                            wallThickness,
                            SKColors.DodgerBlue
                        );
                        
                        new Wall(
                            world,
                            prevPoint2,
                            currentPoint2,
                            wallThickness,
                            SKColors.LightCoral
                        );
                    }
                    
                    prevPoint1 = currentPoint1;
                    prevPoint2 = currentPoint2;
                }
            });
            RegisterSection(doubleHelixSection);
            
            // Crossed circles
            var crossedCirclesSection = new MapSection("crossed_circles", "Crossed Circles", "A section with overlapping circles");
            crossedCirclesSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.2f;
                float radius = Math.Min(width, height) * 0.28f;
                int segments = 24;
                
                // First circle
                float center1X = x + width * 0.35f;
                float center1Y = y + height * 0.5f;
                
                Vector2 prevPoint = new Vector2();
                Vector2 firstPoint = new Vector2();
                
                for (int i = 0; i <= segments; i++)
                {
                    float angle = i * 2 * (float)Math.PI / segments;
                    Vector2 currentPoint = new Vector2(
                        center1X + radius * (float)Math.Cos(angle),
                        center1Y + radius * (float)Math.Sin(angle)
                    );
                    
                    if (i == 0)
                    {
                        firstPoint = currentPoint;
                    }
                    else
                    {
                        new Wall(
                            world,
                            prevPoint,
                            currentPoint,
                            wallThickness,
                            SKColors.Tomato
                        );
                    }
                    
                    prevPoint = currentPoint;
                }
                
                // Second circle
                float center2X = x + width * 0.65f;
                float center2Y = y + height * 0.5f;
                
                prevPoint = new Vector2();
                firstPoint = new Vector2();
                
                for (int i = 0; i <= segments; i++)
                {
                    float angle = i * 2 * (float)Math.PI / segments;
                    Vector2 currentPoint = new Vector2(
                        center2X + radius * (float)Math.Cos(angle),
                        center2Y + radius * (float)Math.Sin(angle)
                    );
                    
                    if (i == 0)
                    {
                        firstPoint = currentPoint;
                    }
                    else
                    {
                        new Wall(
                            world,
                            prevPoint,
                            currentPoint,
                            wallThickness,
                            SKColors.MediumAquamarine
                        );
                    }
                    
                    prevPoint = currentPoint;
                }
            });
            RegisterSection(crossedCirclesSection);
            
            // Fence pattern
            var fenceSection = new MapSection("fence", "Fence", "A section with a fence-like pattern");
            fenceSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.25f;
                int posts = 8;
                
                // Horizontal bars
                new Wall(
                    world,
                    new Vector2(x, y + height * 0.3f),
                    new Vector2(x + width, y + height * 0.3f),
                    wallThickness,
                    SKColors.SandyBrown
                );
                
                new Wall(
                    world,
                    new Vector2(x, y + height * 0.7f),
                    new Vector2(x + width, y + height * 0.7f),
                    wallThickness,
                    SKColors.SandyBrown
                );
                
                // Vertical posts
                for (int i = 0; i <= posts; i++)
                {
                    float postX = x + width * i / posts;
                    
                    new Wall(
                        world,
                        new Vector2(postX, y + height * 0.2f),
                        new Vector2(postX, y + height * 0.8f),
                        wallThickness,
                        SKColors.Sienna
                    );
                }
            });
            RegisterSection(fenceSection);
            
            // Floating islands
            var floatingIslandsSection = new MapSection("floating_islands", "Floating Islands", "A section with floating platform islands");
            floatingIslandsSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.4f;
                
                // Create random floating platforms
                int platforms = 4;
                Random random = new Random();
                
                for (int i = 0; i < platforms; i++)
                {
                    float platformWidth = width * (0.2f + (float)random.NextDouble() * 0.3f);
                    float platformX = x + (float)random.NextDouble() * (width - platformWidth);
                    float platformY = y + height * (i + 0.5f) / (platforms + 1);
                    
                    new Wall(
                        world,
                        new Vector2(platformX, platformY),
                        new Vector2(platformX + platformWidth, platformY),
                        wallThickness,
                        SKColors.LightSlateGray
                    );
                }
            });
            RegisterSection(floatingIslandsSection);
            
            // Circuit board pattern
            var circuitBoardSection = new MapSection("circuit_board", "Circuit Board", "A section with a circuit board-like pattern");
            circuitBoardSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.15f;
                
                // Horizontal lines
                new Wall(
                    world,
                    new Vector2(x, y + height * 0.25f),
                    new Vector2(x + width * 0.75f, y + height * 0.25f),
                    wallThickness,
                    SKColors.LimeGreen
                );
                
                new Wall(
                    world,
                    new Vector2(x + width * 0.25f, y + height * 0.5f),
                    new Vector2(x + width, y + height * 0.5f),
                    wallThickness,
                    SKColors.LimeGreen
                );
                
                new Wall(
                    world,
                    new Vector2(x, y + height * 0.75f),
                    new Vector2(x + width * 0.75f, y + height * 0.75f),
                    wallThickness,
                    SKColors.LimeGreen
                );
                
                // Vertical lines
                new Wall(
                    world,
                    new Vector2(x + width * 0.25f, y),
                    new Vector2(x + width * 0.25f, y + height * 0.25f),
                    wallThickness,
                    SKColors.LimeGreen
                );
                
                new Wall(
                    world,
                    new Vector2(x + width * 0.75f, y + height * 0.25f),
                    new Vector2(x + width * 0.75f, y + height * 0.75f),
                    wallThickness,
                    SKColors.LimeGreen
                );
                
                new Wall(
                    world,
                    new Vector2(x + width * 0.5f, y + height * 0.5f),
                    new Vector2(x + width * 0.5f, y + height),
                    wallThickness,
                    SKColors.LimeGreen
                );
                
                // Add some "component" circles
                new PinballObstacle(
                    world,
                    new Vector2(x + width * 0.25f, y + height * 0.25f),
                    width * 0.05f,
                    SKColors.Gold
                );
                
                new PinballObstacle(
                    world,
                    new Vector2(x + width * 0.75f, y + height * 0.75f),
                    width * 0.05f,
                    SKColors.Gold
                );
            });
            RegisterSection(circuitBoardSection);
            
            // Crisscross pattern
            var crisscrossSection = new MapSection("crisscross", "Crisscross", "A section with a crisscross pattern of lines");
            crisscrossSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.2f;
                int lines = 5;
                
                // Diagonal lines from top-left to bottom-right
                for (int i = 0; i < lines; i++)
                {
                    float startX = x + width * i / (lines - 1);
                    float endX = x + width * (i + 1) / (lines - 1);
                    if (endX > x + width) endX = x + width;
                    
                    new Wall(
                        world,
                        new Vector2(startX, y),
                        new Vector2(endX, y + height),
                        wallThickness,
                        SKColors.CornflowerBlue
                    );
                }
                
                // Diagonal lines from top-right to bottom-left
                for (int i = 0; i < lines; i++)
                {
                    float startX = x + width - width * i / (lines - 1);
                    float endX = x + width - width * (i + 1) / (lines - 1);
                    if (endX < x) endX = x;
                    
                    new Wall(
                        world,
                        new Vector2(startX, y),
                        new Vector2(endX, y + height),
                        wallThickness,
                        SKColors.LightCoral
                    );
                }
            });
            RegisterSection(crisscrossSection);
            
            // Bullseye target
            var bullseyeSection = new MapSection("bullseye", "Bullseye", "A section with a bullseye target");
            bullseyeSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.15f;
                int rings = 4;
                float centerX = x + width / 2;
                float centerY = y + height / 2;
                float maxRadius = Math.Min(width, height) * 0.45f;
                
                // Create concentric circles
                for (int r = 1; r <= rings; r++)
                {
                    float radius = maxRadius * r / rings;
                    int segments = 20;
                    Vector2 prevPoint = new Vector2();
                    Vector2 firstPoint = new Vector2();
                    
                    for (int i = 0; i <= segments; i++)
                    {
                        float angle = i * 2 * (float)Math.PI / segments;
                        Vector2 currentPoint = new Vector2(
                            centerX + radius * (float)Math.Cos(angle),
                            centerY + radius * (float)Math.Sin(angle)
                        );
                        
                        if (i == 0)
                        {
                            firstPoint = currentPoint;
                        }
                        else
                        {
                            new Wall(
                                world,
                                prevPoint,
                                currentPoint,
                                wallThickness,
                                r % 2 == 0 ? SKColors.Crimson : SKColors.White
                            );
                        }
                        
                        prevPoint = currentPoint;
                    }
                }
                
                // Add center point
                new PinballObstacle(
                    world,
                    new Vector2(centerX, centerY),
                    maxRadius / 8,
                    SKColors.Gold
                );
            });
            RegisterSection(bullseyeSection);
            
            // Tree pattern
            var treeSection = new MapSection("tree", "Tree", "A section with a tree-like pattern");
            treeSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.3f;
                
                // Tree trunk
                new Wall(
                    world,
                    new Vector2(x + width / 2, y + height * 0.5f),
                    new Vector2(x + width / 2, y + height),
                    wallThickness * 1.5f,
                    SKColors.Sienna
                );
                
                // Tree branches (at different angles)
                int branches = 6;
                float branchLength = width * 0.35f;
                
                for (int i = 0; i < branches; i++)
                {
                    float angle = i * (float)Math.PI / (branches - 1);
                    float branchX = (float)Math.Cos(angle) * branchLength;
                    float branchY = -(float)Math.Sin(angle) * branchLength;
                    
                    new Wall(
                        world,
                        new Vector2(x + width / 2, y + height * 0.5f),
                        new Vector2(x + width / 2 + branchX, y + height * 0.5f + branchY),
                        wallThickness,
                        SKColors.ForestGreen
                    );
                }
            });
            RegisterSection(treeSection);
            
            // Snowflake pattern
            var snowflakeSection = new MapSection("snowflake", "Snowflake", "A section with a snowflake pattern");
            snowflakeSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.2f;
                float centerX = x + width / 2;
                float centerY = y + height / 2;
                int arms = 6;
                float armLength = Math.Min(width, height) * 0.45f;
                
                // Main arms
                for (int i = 0; i < arms; i++)
                {
                    float angle = i * 2 * (float)Math.PI / arms;
                    float endX = centerX + armLength * (float)Math.Cos(angle);
                    float endY = centerY + armLength * (float)Math.Sin(angle);
                    
                    new Wall(
                        world,
                        new Vector2(centerX, centerY),
                        new Vector2(endX, endY),
                        wallThickness,
                        SKColors.LightSkyBlue
                    );
                    
                    // Add side branches to each arm
                    float sideLength = armLength * 0.4f;
                    float sideAngle1 = angle + (float)Math.PI / 6;
                    float sideAngle2 = angle - (float)Math.PI / 6;
                    float midpointX = centerX + armLength * 0.6f * (float)Math.Cos(angle);
                    float midpointY = centerY + armLength * 0.6f * (float)Math.Sin(angle);
                    
                    new Wall(
                        world,
                        new Vector2(midpointX, midpointY),
                        new Vector2(midpointX + sideLength * (float)Math.Cos(sideAngle1), midpointY + sideLength * (float)Math.Sin(sideAngle1)),
                        wallThickness,
                        SKColors.LightSkyBlue
                    );
                    
                    new Wall(
                        world,
                        new Vector2(midpointX, midpointY),
                        new Vector2(midpointX + sideLength * (float)Math.Cos(sideAngle2), midpointY + sideLength * (float)Math.Sin(sideAngle2)),
                        wallThickness,
                        SKColors.LightSkyBlue
                    );
                }
            });
            RegisterSection(snowflakeSection);
            
            // Atom pattern
            var atomSection = new MapSection("atom", "Atom", "A section with an atom-like pattern");
            atomSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.2f;
                float centerX = x + width / 2;
                float centerY = y + height / 2;
                float orbitalRadius = Math.Min(width, height) * 0.35f;
                
                // Draw nucleus
                new PinballObstacle(
                    world,
                    new Vector2(centerX, centerY),
                    orbitalRadius * 0.2f,
                    SKColors.Gold
                );
                
                // Draw orbits (ellipses at different angles)
                int orbits = 3;
                int segments = 24;
                
                for (int orbit = 0; orbit < orbits; orbit++)
                {
                    float angle = orbit * (float)Math.PI / orbits;
                    float xRadius = orbitalRadius;
                    float yRadius = orbitalRadius * 0.5f;
                    
                    // Rotation matrix components
                    float cos = (float)Math.Cos(angle);
                    float sin = (float)Math.Sin(angle);
                    
                    Vector2 prevPoint = new Vector2();
                    
                    for (int i = 0; i <= segments; i++)
                    {
                        float t = i * 2 * (float)Math.PI / segments;
                        // Ellipse point before rotation
                        float x0 = xRadius * (float)Math.Cos(t);
                        float y0 = yRadius * (float)Math.Sin(t);
                        
                        // Apply rotation
                        float xRot = x0 * cos - y0 * sin;
                        float yRot = x0 * sin + y0 * cos;
                        
                        Vector2 currentPoint = new Vector2(centerX + xRot, centerY + yRot);
                        
                        if (i > 0)
                        {
                            new Wall(
                                world,
                                prevPoint,
                                currentPoint,
                                wallThickness,
                                orbit == 0 ? SKColors.Crimson : (orbit == 1 ? SKColors.RoyalBlue : SKColors.LimeGreen)
                            );
                        }
                        
                        prevPoint = currentPoint;
                    }
                }
            });
            RegisterSection(atomSection);
            
            // Whirlpool pattern
            var whirlpoolSection = new MapSection("whirlpool", "Whirlpool", "A section with a whirlpool pattern");
            whirlpoolSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.2f;
                int turns = 3;
                int segments = 40;
                float centerX = x + width / 2;
                float centerY = y + height / 2;
                float maxRadius = Math.Min(width, height) * 0.45f;
                
                Vector2 prevPoint = new Vector2();
                
                for (int i = 0; i <= segments; i++)
                {
                    float angle = i * turns * 2 * (float)Math.PI / segments;
                    float radius = maxRadius * (1 - i / (float)segments) * (1 - i / (float)segments);
                    
                    Vector2 currentPoint = new Vector2(
                        centerX + radius * (float)Math.Cos(angle),
                        centerY + radius * (float)Math.Sin(angle)
                    );
                    
                    if (i > 0)
                    {
                        new Wall(
                            world,
                            prevPoint,
                            currentPoint,
                            wallThickness,
                            SKColors.DarkCyan
                        );
                    }
                    
                    prevPoint = currentPoint;
                }
            });
            RegisterSection(whirlpoolSection);
            
            // Cloud pattern
            var cloudSection = new MapSection("cloud", "Cloud", "A section with a cloud-like pattern of obstacles");
            cloudSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                // Create a cloud shape with multiple overlapping circles
                float centerX = x + width / 2;
                float centerY = y + height / 2;
                float baseRadius = Math.Min(width, height) * 0.15f;
                
                // Cloud "puffs"
                new PinballObstacle(
                    world,
                    new Vector2(centerX - baseRadius * 1.2f, centerY),
                    baseRadius,
                    SKColors.WhiteSmoke
                );
                
                new PinballObstacle(
                    world,
                    new Vector2(centerX, centerY - baseRadius * 0.5f),
                    baseRadius * 1.2f,
                    SKColors.WhiteSmoke
                );
                
                new PinballObstacle(
                    world,
                    new Vector2(centerX + baseRadius, centerY),
                    baseRadius * 0.9f,
                    SKColors.WhiteSmoke
                );
                
                new PinballObstacle(
                    world,
                    new Vector2(centerX - baseRadius * 0.5f, centerY + baseRadius * 0.4f),
                    baseRadius * 0.7f,
                    SKColors.WhiteSmoke
                );
            });
            RegisterSection(cloudSection);
            
            // Gear pattern
            var gearSection = new MapSection("gear", "Gear", "A section with a gear-like pattern");
            gearSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.2f;
                float centerX = x + width / 2;
                float centerY = y + height / 2;
                float outerRadius = Math.Min(width, height) * 0.45f;
                float innerRadius = outerRadius * 0.7f;
                int teeth = 12;
                
                Vector2 prevPoint = new Vector2();
                Vector2 firstPoint = new Vector2();
                
                for (int i = 0; i <= teeth * 2; i++)
                {
                    float angle = i * (float)Math.PI / teeth;
                    float radius = i % 2 == 0 ? outerRadius : innerRadius;
                    
                    Vector2 currentPoint = new Vector2(
                        centerX + radius * (float)Math.Cos(angle),
                        centerY + radius * (float)Math.Sin(angle)
                    );
                    
                    if (i == 0)
                    {
                        firstPoint = currentPoint;
                    }
                    else
                    {
                        new Wall(
                            world,
                            prevPoint,
                            currentPoint,
                            wallThickness,
                            SKColors.Silver
                        );
                    }
                    
                    prevPoint = currentPoint;
                }
                
                // Close the gear
                new Wall(
                    world,
                    prevPoint,
                    firstPoint,
                    wallThickness,
                    SKColors.Silver
                );
                
                // Add a center hole
                new PinballObstacle(
                    world,
                    new Vector2(centerX, centerY),
                    innerRadius * 0.3f,
                    SKColors.Gray
                );
            });
            RegisterSection(gearSection);
            
            // Bucket pattern
            var bucketSection = new MapSection("bucket", "Bucket", "A section with a bucket-like obstacle to catch marbles");
            bucketSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.3f;
                
                // Bucket left side
                new Wall(
                    world,
                    new Vector2(x + width * 0.25f, y + height * 0.3f),
                    new Vector2(x + width * 0.25f, y + height * 0.9f),
                    wallThickness,
                    SKColors.DimGray
                );
                
                // Bucket right side
                new Wall(
                    world,
                    new Vector2(x + width * 0.75f, y + height * 0.3f),
                    new Vector2(x + width * 0.75f, y + height * 0.9f),
                    wallThickness,
                    SKColors.DimGray
                );
                
                // Bucket bottom
                new Wall(
                    world,
                    new Vector2(x + width * 0.25f, y + height * 0.9f),
                    new Vector2(x + width * 0.75f, y + height * 0.9f),
                    wallThickness,
                    SKColors.DimGray
                );
            });
            RegisterSection(bucketSection);
            
            // Evil face pattern
            var evilFaceSection = new MapSection("evil_face", "Evil Face", "A section with an evil face pattern");
            evilFaceSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                // Eyes (two circles)
                new PinballObstacle(
                    world,
                    new Vector2(x + width * 0.35f, y + height * 0.35f),
                    width * 0.1f,
                    SKColors.Red
                );
                
                new PinballObstacle(
                    world,
                    new Vector2(x + width * 0.65f, y + height * 0.35f),
                    width * 0.1f,
                    SKColors.Red
                );
                
                // Evil smile (curved line)
                float wallThickness = 0.3f;
                float smileWidth = width * 0.6f;
                
                new Ramp(
                    world,
                    new Vector2(x + width * 0.2f, y + height * 0.6f),
                    new Vector2(x + width * 0.8f, y + height * 0.6f),
                    new Vector2(x + width * 0.5f, y + height * 0.8f),
                    wallThickness,
                    SKColors.DarkRed,
                    15
                );
            });
            RegisterSection(evilFaceSection);
            
            // Ladder pattern
            var ladderSection = new MapSection("ladder", "Ladder", "A section with a ladder pattern");
            ladderSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.25f;
                
                // Vertical rails
                new Wall(
                    world,
                    new Vector2(x + width * 0.3f, y),
                    new Vector2(x + width * 0.3f, y + height),
                    wallThickness,
                    SKColors.Brown
                );
                
                new Wall(
                    world,
                    new Vector2(x + width * 0.7f, y),
                    new Vector2(x + width * 0.7f, y + height),
                    wallThickness,
                    SKColors.Brown
                );
                
                // Horizontal rungs
                int rungs = 6;
                
                for (int i = 1; i <= rungs; i++)
                {
                    float rungY = y + height * i / (rungs + 1);
                    
                    new Wall(
                        world,
                        new Vector2(x + width * 0.3f, rungY),
                        new Vector2(x + width * 0.7f, rungY),
                        wallThickness,
                        SKColors.SaddleBrown
                    );
                }
            });
            RegisterSection(ladderSection);
            
            // Bouncy pads
            var bouncyPadsSection = new MapSection("bouncy_pads", "Bouncy Pads", "A section with angled bouncy pads");
            bouncyPadsSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.3f;
                int pads = 3;
                float padLength = width * 0.5f;
                
                for (int i = 0; i < pads; i++)
                {
                    float padY = y + height * (i + 1) / (pads + 1);
                    float padX = i % 2 == 0 ? x : x + width - padLength;
                    float angle = i % 2 == 0 ? (float)Math.PI / 6 : -(float)Math.PI / 6;
                    
                    float endX = padX + padLength * (float)Math.Cos(angle);
                    float endY = padY + padLength * (float)Math.Sin(angle);
                    
                    var wall = new Wall(
                        world,
                        new Vector2(padX, padY),
                        new Vector2(endX, endY),
                        wallThickness,
                        SKColors.HotPink
                    );
                    
                    // Make it bouncy by setting high restitution
                    foreach (var fixture in wall.Body.FixtureList)
                    {
                        fixture.Restitution = 0.9f;
                    }
                }
            });
            RegisterSection(bouncyPadsSection);
            
            // Butterfly effect
            var butterflyEffectSection = new MapSection("butterfly_effect", "Butterfly Effect", "A section with a complex butterfly-inspired pattern");
            butterflyEffectSection.AddElementFactory((world, x, y, width, height, pixelRatio) => {
                float wallThickness = 0.2f;
                
                // Draw butterfly wings with Bezier curves (more complex than the basic butterfly)
                float centerX = x + width / 2;
                float centerY = y + height / 2;
                
                // Body
                new Wall(
                    world,
                    new Vector2(centerX, y + height * 0.3f),
                    new Vector2(centerX, y + height * 0.7f),
                    wallThickness,
                    SKColors.Black
                );
                
                // Left upper wing
                new Ramp(
                    world,
                    new Vector2(centerX, centerY - height * 0.1f),
                    new Vector2(x + width * 0.15f, y + height * 0.3f),
                    new Vector2(x + width * 0.3f, y + height * 0.15f),
                    wallThickness,
                    SKColors.Purple,
                    15
                );
                
                // Right upper wing
                new Ramp(
                    world,
                    new Vector2(centerX, centerY - height * 0.1f),
                    new Vector2(x + width * 0.85f, y + height * 0.3f),
                    new Vector2(x + width * 0.7f, y + height * 0.15f),
                    wallThickness,
                    SKColors.Purple,
                    15
                );
                
                // Left lower wing
                new Ramp(
                    world,
                    new Vector2(centerX, centerY + height * 0.1f),
                    new Vector2(x + width * 0.15f, y + height * 0.7f),
                    new Vector2(x + width * 0.3f, y + height * 0.85f),
                    wallThickness,
                    SKColors.MediumPurple,
                    15
                );
                
                // Right lower wing
                new Ramp(
                    world,
                    new Vector2(centerX, centerY + height * 0.1f),
                    new Vector2(x + width * 0.85f, y + height * 0.7f),
                    new Vector2(x + width * 0.7f, y + height * 0.85f),
                    wallThickness,
                    SKColors.MediumPurple,
                    15
                );
                
                // Decorative patterns on wings
                new PinballObstacle(
                    world,
                    new Vector2(x + width * 0.3f, y + height * 0.3f),
                    width * 0.05f,
                    SKColors.MediumOrchid
                );
                
                new PinballObstacle(
                    world,
                    new Vector2(x + width * 0.7f, y + height * 0.3f),
                    width * 0.05f,
                    SKColors.MediumOrchid
                );
                
                // Antennas
                new Wall(
                    world,
                    new Vector2(centerX - width * 0.05f, y + height * 0.3f),
                    new Vector2(centerX - width * 0.1f, y + height * 0.15f),
                    wallThickness * 0.5f,
                    SKColors.Black
                );
                
                new Wall(
                    world,
                    new Vector2(centerX + width * 0.05f, y + height * 0.3f),
                    new Vector2(centerX + width * 0.1f, y + height * 0.15f),
                    wallThickness * 0.5f,
                    SKColors.Black
                );
            });
            RegisterSection(butterflyEffectSection);
        }
    }
}
