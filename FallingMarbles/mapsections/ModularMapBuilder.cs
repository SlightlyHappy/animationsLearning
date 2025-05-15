using System;
using System.Collections.Generic;
using System.Linq;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Common;

namespace FallingMarbles
{
    public class ModularMapBuilder
    {
        private World _world;
        private int _width;
        private int _height;
        private float _pixelRatio;
        private int _gridColumns;
        private int _gridRows;
        private List<MapElement> _elements = new List<MapElement>();
        private Random _random;
        
        public ModularMapBuilder(World world, int width, int height, float pixelRatio)
        {
            _world = world;
            _width = width;
            _height = height;
            _pixelRatio = pixelRatio;
            _random = new Random();
        }
        
        /// <summary>
        /// Creates a map layout with a grid of map sections
        /// </summary>
        /// <param name="columns">Number of section columns</param>
        /// <param name="rows">Number of section rows</param>
        public List<MapElement> CreateGridLayout(int columns, int rows)
        {
            _gridColumns = columns;
            _gridRows = rows;
            
            // Convert screen dimensions to physics dimensions
            float worldWidth = _width / _pixelRatio;
            float worldHeight = _height / _pixelRatio;
            
            // Calculate section dimensions
            float sectionWidth = worldWidth / columns;
            float sectionHeight = worldHeight / rows;
            
            // Initialize map section factory
            MapSectionFactory.Initialize();
            var allSections = MapSectionFactory.GetAllSections();
            
            // Create wall boundaries
            AddBoundaries(worldWidth, worldHeight);
              // Create grid layout of map sections
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {                    // Choose a random map section
                    var allSectionsList = allSections.ToList();
                    int sectionIndex = _random.Next(allSectionsList.Count);
                    var section = allSectionsList[sectionIndex];
                      try
                    {
                        
                        // Calculate section position
                        float sectionX = col * sectionWidth;
                        float sectionY = row * sectionHeight;
                        
                        // Create section elements
                        var sectionElements = section.CreateElements(_world, sectionX, sectionY, sectionWidth, sectionHeight, _pixelRatio);
                        _elements.AddRange(sectionElements);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error creating map section: {ex.Message}");
                    }
                }
            }
            
            return _elements;
        }
        
        /// <summary>
        /// Creates a map with a custom layout of map sections
        /// </summary>
        /// <param name="layout">2D array of section IDs</param>
        public List<MapElement> CreateCustomLayout(string[,] layout)
        {
            int columns = layout.GetLength(1);
            int rows = layout.GetLength(0);
            _gridColumns = columns;
            _gridRows = rows;
            
            // Convert screen dimensions to physics dimensions
            float worldWidth = _width / _pixelRatio;
            float worldHeight = _height / _pixelRatio;
            
            // Calculate section dimensions
            float sectionWidth = worldWidth / columns;
            float sectionHeight = worldHeight / rows;
            
            // Initialize map section factory
            MapSectionFactory.Initialize();
            
            // Create wall boundaries
            AddBoundaries(worldWidth, worldHeight);
            
            // Create sections based on layout
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    string sectionId = layout[row, col];
                    
                    try
                    {
                        var section = MapSectionFactory.GetSection(sectionId);
                        
                        // Calculate section position
                        float sectionX = col * sectionWidth;
                        float sectionY = row * sectionHeight;
                        
                        // Create section elements
                        var sectionElements = section.CreateElements(_world, sectionX, sectionY, sectionWidth, sectionHeight, _pixelRatio);
                        _elements.AddRange(sectionElements);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error creating map section {sectionId}: {ex.Message}");
                    }
                }
            }
            
            return _elements;
        }
        
        /// <summary>
        /// Creates a completely randomized map layout
        /// </summary>
        /// <param name="columns">Number of columns</param>
        /// <param name="rows">Number of rows</param>
        /// <param name="complexity">Complexity level from 0 (simple) to 1 (complex)</param>
        public List<MapElement> CreateRandomLayout(int columns, int rows, float complexity = 0.5f)
        {
            _gridColumns = columns;
            _gridRows = rows;
            
            // Convert screen dimensions to physics dimensions
            float worldWidth = _width / _pixelRatio;
            float worldHeight = _height / _pixelRatio;
            
            // Calculate section dimensions
            float sectionWidth = worldWidth / columns;
            float sectionHeight = worldHeight / rows;
            
            // Initialize map section factory
            MapSectionFactory.Initialize();
            var allSections = MapSectionFactory.GetAllSections();
            List<MapSection> availableSections = new List<MapSection>();
            
            // Filter sections based on complexity
            foreach (var section in allSections)
            {
                // Simple approach - just use all sections for now
                availableSections.Add(section);
            }
            
            // Create wall boundaries
            AddBoundaries(worldWidth, worldHeight);
            
            // Create grid layout of map sections
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    // Choose a random map section
                    int sectionIndex = _random.Next(availableSections.Count);
                    var section = availableSections[sectionIndex];
                    
                    // Calculate section position
                    float sectionX = col * sectionWidth;
                    float sectionY = row * sectionHeight;
                    
                    // Create section elements
                    var sectionElements = section.CreateElements(_world, sectionX, sectionY, sectionWidth, sectionHeight, _pixelRatio);
                    _elements.AddRange(sectionElements);
                }
            }
            
            return _elements;
        }
          /// <summary>
        /// Add boundary walls to the map
        /// </summary>
        private void AddBoundaries(float worldWidth, float worldHeight)
        {
            float wallThickness = 0.5f; // 50 pixels
            
            // Bottom wall
            _elements.Add(new Wall(
                _world,
                new Vector2(0, worldHeight),
                new Vector2(worldWidth, worldHeight),
                wallThickness,
                SkiaSharp.SKColors.Gray
            ));
            
            // Left wall
            _elements.Add(new Wall(
                _world,
                new Vector2(0, 0),
                new Vector2(0, worldHeight),
                wallThickness,
                SkiaSharp.SKColors.Gray
            ));
            
            // Right wall
            _elements.Add(new Wall(
                _world,
                new Vector2(worldWidth, 0),
                new Vector2(worldWidth, worldHeight),
                wallThickness,
                SkiaSharp.SKColors.Gray
            ));
            
            // Top wall
            _elements.Add(new Wall(
                _world,
                new Vector2(0, 0),
                new Vector2(worldWidth, 0),
                wallThickness,
                SkiaSharp.SKColors.Gray
            ));
        }
    }
}
