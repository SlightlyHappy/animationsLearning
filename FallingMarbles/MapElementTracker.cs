using System;
using System.Collections.Generic;

namespace FallingMarbles
{
    /// <summary>
    /// Static class to track and manage all map elements created in the game
    /// This helps collect elements created by map section factories
    /// </summary>
    public static class MapElementTracker
    {
        public static List<MapElement> Elements { get; } = new List<MapElement>();
        
        /// <summary>
        /// Register a new map element with the tracker
        /// </summary>
        /// <param name="element">The map element to track</param>
        public static void RegisterElement(MapElement element)
        {
            Elements.Add(element);
        }
        
        /// <summary>
        /// Clear all tracked elements
        /// </summary>
        public static void ClearElements()
        {
            Elements.Clear();
        }
    }
}
