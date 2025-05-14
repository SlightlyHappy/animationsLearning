using System;
using System.Collections.Generic;
using SkiaSharp;
using tainicom.Aether.Physics2D.Common;
using InfiniteMarbleRun.Core;

namespace InfiniteMarbleRun.Generation
{
    /// <summary>
    /// Handles procedural generation of the marble run course
    /// </summary>
    public class CourseGenerator
    {
        private Random _random;
        private int _complexity;
        private int _width;
        private int _height;
        
        // Generation parameters
        private const int MinSections = 10;
        private const float MinSectionLength = 150f;
        private const float MaxSectionLength = 500f;
        private const float RampProbability = 0.4f;
        private const float FunnelProbability = 0.2f;
        private const float SpecialFeatureProbability = 0.3f;
        
        // Segment types for path generation
        private enum SegmentType
        {
            Straight,
            CurveLeft,
            CurveRight,
            Swoop,
            Drop
        }
        
        // Feature zone types for special feature placement
        private enum ZoneType
        {
            OpenArea,
            Downhill,
            TightTurn,
            Transition
        }
        
        // Class to represent a zone where features can be placed
        private class FeatureZone
        {
            public int StartIndex { get; set; }
            public int EndIndex { get; set; }
            public ZoneType Type { get; set; }
            public float Priority { get; set; } // Higher means more likely to get features
        }
        
        public CourseGenerator(Random random, int complexity, int width, int height)
        {
            _random = random;
            _complexity = Math.Clamp(complexity, 1, 10);
            _width = width;
            _height = height;
        }
          public Course Generate()
        {
            // Initialize course with basic properties
            var course = new Course(_width, _height, _complexity, _random.Next());
            
            // Step 1: Advanced dynamic path generation
            // Determine number of sections based on complexity - more complexity = more sections
            int numSections = MinSections + _complexity * 3;
            
            // Create path control points with varied segment types for visual interest
            List<Vector2> controlPoints = GenerateControlPoints(numSections);
            
            // Define start and finish positions
            course.StartPosition = controlPoints[0];
            course.FinishPosition = controlPoints[controlPoints.Count - 1];
            
            // Add strategic checkpoints at key locations - more frequent at the beginning
            // and near difficult segments to provide better player feedback
            for (int i = 0; i < controlPoints.Count; i++)
            {
                if (i % Math.Max(2, 5 - _complexity/2) == 0 || i == controlPoints.Count - 1)
                {
                    course.AddCheckpoint(controlPoints[i]);
                }
            }
            
            // Step 2: Physics-aware section generation
            // Create physics-appropriate sections between control points
            GeneratePathSections(course, controlPoints);
            
            // Step 3: Strategic feature placement
            // Add special features based on zone analysis
            AddSpecialFeatures(course);
            
            // Step 4: Visual theming and environment
            // Apply consistent visual theme with dynamic color gradients
            AssignThematicElements(course);
            
            // Step 5: Physics optimization
            // Fine-tune physics properties for a balanced playing experience
            OptimizePhysicsProperties(course);
            
            return course;
        }
        /// <summary>
        /// Generates a list of control points for the course path, creating a smooth zigzag pattern
        /// from the top to the bottom of the screen.
        /// </summary>
        /// <param name="numSections">The number of sections or control points to generate.</param>
        /// <returns>A list of Vector2 points representing control points for the course path.</returns>
        private List<Vector2> GenerateControlPoints(int numSections)
        {
            List<Vector2> points = new List<Vector2>();
            // Start at the top center
            Vector2 current = new Vector2(_width * 0.5f, _height * 0.1f);
            points.Add(current);

            // Define segment types for variety
            var segmentTypes = new List<SegmentType> { 
                SegmentType.Straight, 
                SegmentType.CurveLeft, 
                SegmentType.CurveRight, 
                SegmentType.Swoop, 
                SegmentType.Drop
            };

            float remainingHeight = _height * 0.8f;
            float segmentHeight = remainingHeight / numSections;

            for (int i = 1; i < numSections; i++) {
                SegmentType nextType = ChooseSegmentType(i);
                float segmentLength = CalculateSegmentLength(nextType);
                
                // Calculate new point based on segment type
                Vector2 next = CalculateNextPoint(current, segmentLength, nextType, i, numSections);
                points.Add(next);
                current = next;
            }

            // Ensure smooth finish at the bottom center
            points.Add(new Vector2(_width * 0.5f, _height * 0.9f));
            return points;
        }

        private SegmentType ChooseSegmentType(int segmentIndex)
        {
            // More strategic segment selection based on position in course
            if (segmentIndex < 3)
            {
                // Early course: more gentle segments
                float rand = (float)_random.NextDouble();
                if (rand < 0.6f)
                    return SegmentType.Straight;
                else if (rand < 0.8f)
                    return SegmentType.CurveLeft;
                else
                    return SegmentType.CurveRight;
            }
            else
            {
                // Later course: full variety with emphasis on interesting segments
                float rand = (float)_random.NextDouble();
                
                if (rand < 0.2f)
                    return SegmentType.Straight;
                else if (rand < 0.4f)
                    return SegmentType.CurveLeft;
                else if (rand < 0.6f)
                    return SegmentType.CurveRight;
                else if (rand < 0.8f)
                    return SegmentType.Swoop;
                else
                    return SegmentType.Drop;
            }
        }

        private float CalculateSegmentLength(SegmentType type)
        {
            // Base length modified by segment type
            float baseLength = MinSectionLength + (float)_random.NextDouble() * (MaxSectionLength - MinSectionLength);
            
            // Adjust based on type
            switch(type)
            {
                case SegmentType.Drop:
                    return baseLength * 1.5f; // Longer drop sections
                case SegmentType.Swoop:
                    return baseLength * 1.3f; // Longer swoops
                case SegmentType.Straight:
                    return baseLength * 0.9f; // Shorter straights
                default:
                    return baseLength;
            }
        }

        private Vector2 CalculateNextPoint(Vector2 current, float length, SegmentType type, int index, int total)
        {
            float baseYIncrement = length * 0.8f;
            float xVariation = _width * 0.3f * (float)_random.NextDouble();
            
            switch(type) {
                case SegmentType.CurveLeft:
                    return new Vector2(current.X - xVariation, current.Y + baseYIncrement);
                case SegmentType.CurveRight:
                    return new Vector2(current.X + xVariation, current.Y + baseYIncrement);
                case SegmentType.Swoop:
                    // Create a downward arc
                    float swoopX = current.X + (_random.NextDouble() > 0.5 ? -1 : 1) * xVariation * 1.5f;
                    return new Vector2(swoopX, current.Y + baseYIncrement * 1.5f);
                case SegmentType.Drop:
                    // Vertical drop with minimal horizontal movement
                    return new Vector2(current.X, current.Y + baseYIncrement * 2f);
                default: // Straight
                    float centerX = _width * 0.5f;
                    float xDrift = (current.X - centerX) * 0.2f; // Pull toward center
                    return new Vector2(current.X - xDrift, current.Y + baseYIncrement);
            }
        }        private void GeneratePathSections(Course course, List<Vector2> controlPoints)
        {
            // Ensure we don't have too many special sections in a row
            int consecutiveSpecialSections = 0;
            CourseSection.SectionType lastType = CourseSection.SectionType.Normal;
            Vector2? previousStart = null;
            
            for (int i = 0; i < controlPoints.Count - 1; i++)
            {
                Vector2 start = controlPoints[i];
                Vector2 end = controlPoints[i + 1];
                
                // Analyze segment characteristics
                SegmentContext context = AnalyzeSegment(start, end, previousStart);
                previousStart = start;
                
                // Limit consecutive special sections
                if (lastType != CourseSection.SectionType.Normal)
                {
                    consecutiveSpecialSections++;
                    if (consecutiveSpecialSections >= 2)
                    {
                        // Force normal section to break up special sections
                        CourseSection normalSection = CreateDefaultPathSection(context);
                        normalSection.Type = CourseSection.SectionType.Normal;
                        course.AddSection(normalSection);
                        lastType = CourseSection.SectionType.Normal;
                        consecutiveSpecialSections = 0;
                        continue;
                    }
                }
                else
                {
                    consecutiveSpecialSections = 0;
                }
                
                // Don't allow special sections at the start or end
                if (i == 0 || i >= controlPoints.Count - 2)
                {
                    CourseSection section = CreateDefaultPathSection(context);
                    section.Type = CourseSection.SectionType.Normal;
                    course.AddSection(section);
                    lastType = CourseSection.SectionType.Normal;
                    continue;
                }
                
                // Generate appropriate section based on context
                CourseSection newSection;
                
                // Priority logic for section type
                if (context.IsVerticalDrop && _random.NextDouble() < 0.6f)
                {
                    newSection = CreateVerticalDropSection(context);
                    newSection.Type = CourseSection.SectionType.Ramp;
                    lastType = CourseSection.SectionType.Ramp;
                }
                else if (context.IsSharpCurve && _random.NextDouble() < 0.4f)
                {
                    newSection = CreateBankedTurnSection(context);
                    newSection.Type = CourseSection.SectionType.Funnel;
                    lastType = CourseSection.SectionType.Funnel;
                }
                else if (context.IsLongSegment && _random.NextDouble() < 0.3f)
                {
                    newSection = CreateBoostZoneSection(context);
                    newSection.Type = CourseSection.SectionType.Booster;
                    lastType = CourseSection.SectionType.Booster;
                }
                else
                {
                    newSection = CreateDefaultPathSection(context);
                    newSection.Type = CourseSection.SectionType.Normal;
                    lastType = CourseSection.SectionType.Normal;
                }
                
                course.AddSection(newSection);
            }
        }
        
        private SegmentContext AnalyzeSegment(Vector2 start, Vector2 end, Vector2? previousStart)
        {
            var context = new SegmentContext();
            context.Start = start;
            context.End = end;
            
            // Calculate direction and length
            context.Direction = end - start;
            context.Length = context.Direction.Length();
            if (context.Length > 0)
            {
                context.Direction.Normalize();
            }
            
            // Calculate vertical drop
            context.VerticalDrop = end.Y - start.Y;
            
            // Calculate angle change if we have a previous point
            if (previousStart.HasValue)
            {
                Vector2 prevDirection = start - previousStart.Value;
                if (prevDirection.Length() > 0)
                {
                    prevDirection.Normalize();
                    
                    // Calculate dot product to determine angle
                    float dot = prevDirection.X * context.Direction.X + prevDirection.Y * context.Direction.Y;
                    context.Angle = (float)Math.Acos(dot);
                    
                    // Use cross product to determine sign of angle
                    if (prevDirection.X * context.Direction.Y - prevDirection.Y * context.Direction.X < 0)
                    {
                        context.Angle = -context.Angle;
                    }
                }
            }
            
            return context;
        }
          private CourseSection CreatePathSection(Vector2 start, Vector2 end)
        {
            // Calculate direction and length
            Vector2 direction = end - start;
            float length = direction.Length();
            direction.Normalize();
            Vector2 normal = new Vector2(-direction.Y, direction.X);
            normal.Normalize();
            
            // Calculate width of the path - make it wider for better containment
            // Ensure a minimum width to prevent marbles from falling through
            float width = 60f + (float)_random.NextDouble() * 20f;
            
            // Create a concave channel shape to better guide the marbles
            // We'll use six points instead of just four to create walls on both sides
            float wallHeight = width * 0.5f; // Height of side walls
            
            List<Vector2> vertices = new List<Vector2>
            {
                // Left outer wall
                start + normal * (width + wallHeight),
                
                // Left inner wall
                start + normal * width,
                
                // Inner path points
                start - normal * width,
                end - normal * width,
                
                // Right inner wall
                end + normal * width,
                
                // Right outer wall
                end + normal * (width + wallHeight)
            };
            
            var section = new CourseSection(vertices);
            
            // Lower friction for smoother marble travel
            section.Friction = 0.05f + (float)_random.NextDouble() * 0.2f;
            section.Color = SKColors.Gray;
            
            return section;
        }
        
        private CourseSection CreateRampSection(Vector2 start, Vector2 end)
        {
            // Similar to path but with different properties
            CourseSection ramp = CreatePathSection(start, end);
            ramp.Friction = 0.05f + (float)_random.NextDouble() * 0.1f; // Slicker
            ramp.Color = SKColors.SteelBlue;
            
            return ramp;
        }
          private CourseSection CreateFunnelSection(Vector2 start, Vector2 end)
        {
            // Calculate direction and length
            Vector2 direction = end - start;
            float length = direction.Length();
            direction.Normalize();
            Vector2 normal = new Vector2(-direction.Y, direction.X);
            
            // Create a wider entrance and narrow exit with better control
            float entranceWidth = 100f + (float)_random.NextDouble() * 40f;
            float exitWidth = 40f + (float)_random.NextDouble() * 15f; // Make exit wider to prevent bottlenecks
            
            // Create vertices for the funnel with extended walls for better guidance
            float wallHeight = 30f; // Height of side walls
            
            // Create a more complex funnel shape with 8 points to better guide marbles
            List<Vector2> vertices = new List<Vector2>
            {
                // Outer entrance (left)
                start + normal * (entranceWidth + wallHeight),
                
                // Inner entrance (left)
                start + normal * entranceWidth,
                
                // Create a smooth curve for the funnel using mid-points
                start - normal * entranceWidth,
                
                // Midpoint on right side
                start + direction * (length * 0.3f) - normal * (entranceWidth * 0.6f + exitWidth * 0.4f),
                
                // Midpoint on left side
                start + direction * (length * 0.7f) - normal * (entranceWidth * 0.3f + exitWidth * 0.7f),
                
                // Exit left
                end - normal * exitWidth,
                
                // Exit right
                end + normal * exitWidth,
                
                // Outer exit (right)
                end + normal * (exitWidth + wallHeight)
            };
            
            var section = new CourseSection(vertices);
            section.Friction = 0.1f + (float)_random.NextDouble() * 0.2f; // Lower friction for smoother flow
            section.Color = SKColors.DarkOrange;
            
            return section;
        }
          private void AddSpecialFeatures(Course course)
        {
            // Analyze the course and identify good feature placement zones
            var featureZones = IdentifyFeatureZones(course);
            
            // Determine how many features to add based on complexity
            int numFeatures = Math.Max(3, _complexity);
            int featuresAdded = 0;
            
            // Sort zones by priority
            featureZones.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            
            // Process each zone in priority order
            foreach (var zone in featureZones)
            {
                if (featuresAdded >= numFeatures) break;
                
                // Calculate probability based on zone type and remaining features needed
                float probability = GetFeatureProbability(zone, featuresAdded, numFeatures);
                
                if (_random.NextDouble() < probability)
                {
                    ApplyFeatureToZone(course, zone);
                    featuresAdded++;
                }
            }
            
            // If we didn't add enough features through zones, add some randomly
            while (featuresAdded < numFeatures && course.Sections.Count > 5)
            {
                // Find a section that doesn't already have a special feature
                int attempts = 0;
                int sectionIndex;
                do
                {
                    // Don't add features to the first or last 2 sections
                    sectionIndex = 2 + _random.Next(course.Sections.Count - 4);
                    attempts++;
                } while (course.Sections[sectionIndex].Type != CourseSection.SectionType.Normal && attempts < 10);
                
                if (attempts < 10)
                {
                    // Apply a random feature to this section
                    ApplyRandomFeature(course.Sections[sectionIndex]);
                    featuresAdded++;
                }
                else
                {
                    // Couldn't find a good section, stop trying
                    break;
                }
            }
        }
        
        private List<FeatureZone> IdentifyFeatureZones(Course course)
        {
            var zones = new List<FeatureZone>();
            
            // Don't process if course is too small
            if (course.Sections.Count < 6) return zones;
            
            // Identify candidate zones based on section type and position
            for (int i = 2; i < course.Sections.Count - 3; i += 3)
            {
                // Analyze this section and the next two to determine zone type
                var currentSection = course.Sections[i];
                var nextSection = i + 1 < course.Sections.Count ? course.Sections[i + 1] : null;
                var thirdSection = i + 2 < course.Sections.Count ? course.Sections[i + 2] : null;
                
                if (nextSection == null) continue;
                
                ZoneType zoneType = DetermineZoneType(currentSection, nextSection, thirdSection);
                float priority = CalculateZonePriority(i, zoneType, course.Sections.Count);
                
                var zone = new FeatureZone
                {
                    StartIndex = i,
                    EndIndex = i + (thirdSection != null ? 2 : 1),
                    Type = zoneType,
                    Priority = priority
                };
                
                zones.Add(zone);
            }
            
            return zones;
        }
        
        private ZoneType DetermineZoneType(CourseSection current, CourseSection next, CourseSection third = null)
        {
            // Analyze section geometry and properties to determine zone type
            
            // Check for downhill sections
            if (current.Vertices.Count >= 4 && next.Vertices.Count >= 4)
            {
                float startY = (current.Vertices[0].Y + current.Vertices[current.Vertices.Count - 1].Y) / 2;
                float endY = (next.Vertices[0].Y + next.Vertices[next.Vertices.Count - 1].Y) / 2;
                
                if (endY - startY > 200f)
                {
                    return ZoneType.Downhill;
                }
            }
            
            // Check for tight turns
            if (current.Type == CourseSection.SectionType.Funnel || next.Type == CourseSection.SectionType.Funnel)
            {
                return ZoneType.TightTurn;
            }
            
            // Check for transitions (changes between section types)
            if (third != null && current.Type != next.Type || next.Type != third.Type)
            {
                return ZoneType.Transition;
            }
            
            // Default to open area
            return ZoneType.OpenArea;
        }
        
        private float CalculateZonePriority(int index, ZoneType type, int totalSections)
        {
            // Base priority on position and type
            float positionFactor = 1.0f - Math.Abs((float)index / totalSections - 0.5f) * 2; // Favor middle sections
            
            // Type-specific priority modifiers
            float typePriority;
            switch (type)
            {
                case ZoneType.Downhill:
                    typePriority = 1.2f;
                    break;
                case ZoneType.TightTurn:
                    typePriority = 1.1f;
                    break;
                case ZoneType.Transition:
                    typePriority = 0.9f;
                    break;
                default:
                    typePriority = 1.0f;
                    break;
            }
            
            return positionFactor * typePriority;
        }
        
        private float GetFeatureProbability(FeatureZone zone, int featuresAdded, int totalFeatures)
        {
            // Base probability on how many features we still need to add
            float remainingRatio = (float)(totalFeatures - featuresAdded) / totalFeatures;
            
            // Modify by zone priority
            float baseProbability = remainingRatio * zone.Priority;
            
            // Cap probability
            return Math.Min(0.85f, baseProbability);
        }
        
        private void ApplyFeatureToZone(Course course, FeatureZone zone)
        {
            // Apply appropriate features based on zone type
            switch (zone.Type)
            {
                case ZoneType.OpenArea:
                    AddSpinnerCluster(course, zone);
                    break;
                case ZoneType.Downhill:
                    AddSpeedRamp(course, zone);
                    break;
                case ZoneType.TightTurn:
                    AddBumperArray(course, zone);
                    break;
                case ZoneType.Transition:
                    AddCheckpointWithGlow(course, zone);
                    break;
            }
        }
        
        private void AddSpinnerCluster(Course course, FeatureZone zone)
        {
            // Add spinner feature to a section in this zone
            int sectionIndex = zone.StartIndex + _random.Next(zone.EndIndex - zone.StartIndex + 1);
            if (sectionIndex < course.Sections.Count)
            {
                var section = course.Sections[sectionIndex];
                section.Type = CourseSection.SectionType.Spinner;
                section.Color = SKColors.Purple;
                section.GlowIntensity = 0.5f + (float)_random.NextDouble() * 0.3f;
            }
        }
        
        private void AddSpeedRamp(Course course, FeatureZone zone)
        {
            // Add speed boost to a downhill section
            int sectionIndex = zone.StartIndex;
            if (sectionIndex < course.Sections.Count)
            {
                var section = course.Sections[sectionIndex];
                section.Type = CourseSection.SectionType.Booster;
                section.Color = SKColors.Green;
                section.GlowIntensity = 0.7f;
                section.Friction = 0.01f;
            }
        }
        
        private void AddBumperArray(Course course, FeatureZone zone)
        {
            // Add bumpers to a turn section
            int sectionIndex = zone.StartIndex;
            if (sectionIndex < course.Sections.Count)
            {
                var section = course.Sections[sectionIndex];
                section.Type = CourseSection.SectionType.Bumpers;
                section.Color = SKColors.Red;
                section.Bounciness = 0.9f;
                section.GlowIntensity = 0.4f;
            }
        }
        
        private void AddCheckpointWithGlow(Course course, FeatureZone zone)
        {
            // Add visual indicator at transition point
            int sectionIndex = zone.EndIndex;
            if (sectionIndex < course.Sections.Count)
            {
                var section = course.Sections[sectionIndex];
                section.GlowIntensity = 0.6f;
                section.Color = SKColors.Yellow;
                
                // Add a checkpoint here if it doesn't already have one nearby
                bool hasNearbyCheckpoint = false;                foreach (var checkpoint in course.Checkpoints)
                {
                    float midX = 0, midY = 0;
                    foreach (var vertex in section.Vertices)
                    {
                        midX += vertex.X;
                        midY += vertex.Y;
                    }
                    midX /= section.Vertices.Count;
                    midY /= section.Vertices.Count;
                      Vector2 midpoint = new Vector2(midX, midY);
                    // Calculate distance manually without ref parameters
                    float dx = checkpoint.X - midpoint.X;
                    float dy = checkpoint.Y - midpoint.Y;
                    float distanceSquared = dx * dx + dy * dy;
                    
                    if (distanceSquared < 200f * 200f) // Square the comparison value
                    {
                        hasNearbyCheckpoint = true;
                        break;
                    }
                }
                
                if (!hasNearbyCheckpoint)
                {
                    float midX = 0, midY = 0;
                    foreach (var vertex in section.Vertices)
                    {
                        midX += vertex.X;
                        midY += vertex.Y;
                    }
                    midX /= section.Vertices.Count;
                    midY /= section.Vertices.Count;
                    
                    course.AddCheckpoint(new Vector2(midX, midY));
                }
            }
        }
        
        private void ApplyRandomFeature(CourseSection section)
        {
            // Choose a random feature type
            int featureType = _random.Next(5);
            
            switch(featureType)
            {
                case 0:
                    section.Type = CourseSection.SectionType.Spinner;
                    section.Color = SKColors.Purple;
                    section.GlowIntensity = 0.5f;
                    break;
                case 1:
                    section.Type = CourseSection.SectionType.Booster;
                    section.Color = SKColors.Green;
                    section.GlowIntensity = 0.7f;
                    break;
                case 2:
                    section.Type = CourseSection.SectionType.SlowField;
                    section.Color = SKColors.SlateBlue;
                    section.GlowIntensity = 0.4f;
                    section.Friction = 0.8f;
                    break;
                case 3:
                    section.Type = CourseSection.SectionType.Bumpers;
                    section.Color = SKColors.Red;
                    section.Bounciness = 0.9f;
                    break;
                case 4:
                    // Leave as is but add glow
                    section.GlowIntensity = 0.6f;
                    section.Color = SKColors.Yellow;
                    break;
            }
        }
          private void AssignThematicElements(Course course)
        {
            // Base theme
            course.PrimaryColor = VibrantThemeColor();
            course.SecondaryColor = AnalogousColor(course.PrimaryColor);
            course.AccentColor = ComplementaryColor(course.PrimaryColor);
            
            // Section-specific styling
            StylePathSections(course);
            StyleSpecialFeatures(course);
            AddEnvironmentalElements(course);
        }
        
        private void StylePathSections(Course course)
        {
            float sectionCount = course.Sections.Count;
            for (int i = 0; i < sectionCount; i++)
            {
                var section = course.Sections[i];
                float progress = i / sectionCount;
                
                // Gradient color transition
                section.Color = ColorGradient(
                    course.PrimaryColor, 
                    course.SecondaryColor, 
                    Math.Sin(progress * Math.PI)
                );
                
                // Only add glow to special sections
                if (section.Type != CourseSection.SectionType.Normal)
                {
                    // Dynamic glow intensity
                    section.GlowIntensity = Math.Max(section.GlowIntensity, 
                        0.2f + 0.5f * (float)Math.Sin(progress * Math.PI * 2));
                }
            }
        }
        
        private void StyleSpecialFeatures(Course course)
        {
            // Apply special styling to feature sections
            foreach (var section in course.Sections)
            {
                switch (section.Type)
                {
                    case CourseSection.SectionType.Spinner:
                        section.Color = SKColors.Purple;
                        section.GlowIntensity = 0.6f;
                        break;
                    case CourseSection.SectionType.Booster:
                        section.Color = SKColors.LimeGreen;
                        section.GlowIntensity = 0.7f;
                        break;
                    case CourseSection.SectionType.SlowField:
                        section.Color = SKColors.SlateBlue;
                        section.GlowIntensity = 0.5f;
                        break;
                    case CourseSection.SectionType.Bumpers:
                        section.Color = SKColors.Crimson;
                        section.GlowIntensity = 0.4f;
                        break;
                    case CourseSection.SectionType.Ramp:
                        section.Color = MakeDarker(course.SecondaryColor, 0.2f);
                        section.GlowIntensity = 0.2f;
                        break;
                    case CourseSection.SectionType.Funnel:
                        section.Color = MakeBrighter(course.PrimaryColor, 0.2f);
                        section.GlowIntensity = 0.3f;
                        break;
                }
            }
        }
        
        private void AddEnvironmentalElements(Course course)
        {
            // This would typically add background elements, but since we don't have direct access
            // to those systems in this code, we'll use the glowing effect on checkpoints
            
            // Add glow to checkpoints
            for (int i = 0; i < course.Checkpoints.Count; i++)
            {
                // Find sections near this checkpoint to add visual emphasis
                Vector2 checkpoint = course.Checkpoints[i];
                
                foreach (var section in course.Sections)
                {
                    // Calculate section center
                    float centerX = 0, centerY = 0;
                    foreach (var vertex in section.Vertices)
                    {
                        centerX += vertex.X;
                        centerY += vertex.Y;
                    }
                    centerX /= section.Vertices.Count;
                    centerY /= section.Vertices.Count;                    // If checkpoint is near this section, add visual emphasis
                    Vector2 sectionCenter = new Vector2(centerX, centerY);
                    // Calculate distance manually without ref parameters
                    float dx = checkpoint.X - sectionCenter.X;
                    float dy = checkpoint.Y - sectionCenter.Y;
                    float distanceSquared = dx * dx + dy * dy;
                    if (distanceSquared < 150f * 150f) // Square the comparison value
                    {
                        // Add subtle glow to this section if it doesn't already have a strong glow
                        if (section.GlowIntensity < 0.3f)
                        {
                            section.GlowIntensity = 0.3f;
                            
                            // Make checkpoint areas visually distinct
                            float h, s, l;
                            course.AccentColor.ToHsl(out h, out s, out l);
                            section.Color = SKColor.FromHsl(h, s, l + 15);
                        }
                        break;
                    }
                }
            }
        }

        private SKColor VibrantThemeColor()
        {
            // Generate vibrant, camera-friendly colors
            // Favor hues that look good on screen (avoid muddy colors)
            int hueGroup = _random.Next(6);
            float hue;
            
            switch(hueGroup)
            {
                case 0: // Blues
                    hue = 210 + _random.Next(30);
                    break;
                case 1: // Purples
                    hue = 270 + _random.Next(30);
                    break;
                case 2: // Reds
                    hue = 0 + _random.Next(20);
                    break;
                case 3: // Oranges
                    hue = 30 + _random.Next(20);
                    break;
                case 4: // Greens
                    hue = 120 + _random.Next(30);
                    break;
                default: // Teals
                    hue = 180 + _random.Next(20);
                    break;
            }
            
            return SKColor.FromHsl(hue, 80, 50);
        }
        
        private SKColor AnalogousColor(SKColor color)
        {
            // Create an analogous color (adjacent on color wheel)
            float h, s, l;
            color.ToHsl(out h, out s, out l);
            
            // Shift hue by 30 degrees in either direction
            h = (h + (_random.NextDouble() > 0.5 ? 30 : -30) + 360) % 360;
            
            return SKColor.FromHsl(h, s, l);
        }
        
        private SKColor ComplementaryColor(SKColor color)
        {
            // Create a complementary color
            float h, s, l;
            color.ToHsl(out h, out s, out l);
            
            // Complementary hue (opposite on the color wheel)
            h = (h + 180) % 360;
            
            // Make it a bit brighter for more pop
            l = Math.Min(l + 10, 100);
            
            return SKColor.FromHsl(h, s, l);
        }
        
        private SKColor BrightAccentColor()
        {
            // Generate a bright accent color
            byte hue = (byte)_random.Next(255);
            return SKColor.FromHsl(hue, 100, 70);
        }
        
        private SKColor ColorGradient(SKColor start, SKColor end, double factor)
        {
            // Create a gradient between two colors
            byte r = (byte)(start.Red + (end.Red - start.Red) * factor);
            byte g = (byte)(start.Green + (end.Green - start.Green) * factor);
            byte b = (byte)(start.Blue + (end.Blue - start.Blue) * factor);
            
            return new SKColor(r, g, b);
        }
        
        private SKColor MakeBrighter(SKColor color, float amount)
        {
            float h, s, l;
            color.ToHsl(out h, out s, out l);
            
            l = Math.Min(l + amount * 100, 100); // Increase lightness
            
            return SKColor.FromHsl(h, s, l);
        }
        
        private SKColor MakeDarker(SKColor color, float amount)
        {
            float h, s, l;
            color.ToHsl(out h, out s, out l);
            
            l = Math.Max(l - amount * 100, 0); // Decrease lightness
            
            return SKColor.FromHsl(h, s, l);
        }
        
        // Class to hold segment analysis data
        private class SegmentContext
        {
            public Vector2 Start { get; set; }
            public Vector2 End { get; set; }
            public Vector2 Direction { get; set; }
            public float Length { get; set; }
            public float Angle { get; set; }
            public float VerticalDrop { get; set; }
            public bool IsVerticalDrop => VerticalDrop > Length * 0.8f;
            public bool IsSharpCurve => Math.Abs(Angle) > 0.5f;
            public bool IsLongSegment => Length > 400f;
            public bool IsShortSegment => Length < 200f;
            
            // Calculate normal vector (perpendicular to direction)
            public Vector2 Normal => new Vector2(-Direction.Y, Direction.X);
        }
          private CourseSection CreateDefaultPathSection(SegmentContext context)
        {
            // Calculate width of the path - make it wider for better containment
            // Ensure a minimum width to prevent marbles from falling through
            float width = 60f + (float)_random.NextDouble() * 20f;
            float wallHeight = width * 0.5f; // Height of side walls
            
            // Create a concave channel shape using the normal vector
            Vector2 normal = context.Normal;
            
            List<Vector2> vertices = new List<Vector2>
            {
                // Left outer wall
                context.Start + normal * (width + wallHeight),
                
                // Left inner wall
                context.Start + normal * width,
                
                // Inner path points
                context.Start - normal * width,
                context.End - normal * width,
                
                // Right inner wall
                context.End + normal * width,
                
                // Right outer wall
                context.End + normal * (width + wallHeight)
            };
            
            var section = new CourseSection(vertices);
            
            // Lower friction for smoother marble travel
            section.Friction = 0.05f + (float)_random.NextDouble() * 0.2f;
            section.Color = SKColors.Gray;
            
            return section;
        }
        
        private CourseSection CreateVerticalDropSection(SegmentContext context)
        {
            // Similar to path but with different properties for vertical drops
            float width = 70f + (float)_random.NextDouble() * 20f; // Slightly wider
            float wallHeight = width * 0.7f; // Taller walls for drops
            
            Vector2 normal = context.Normal;
            
            // Create a steeper-walled channel for dropping
            List<Vector2> vertices = new List<Vector2>
            {
                // Left outer wall
                context.Start + normal * (width + wallHeight),
                
                // Left inner wall (steeper)
                context.Start + normal * width * 0.8f,
                
                // Inner path points (wider)
                context.Start - normal * width * 0.8f,
                context.End - normal * width,
                
                // Right inner wall
                context.End + normal * width,
                
                // Right outer wall
                context.End + normal * (width + wallHeight)
            };
            
            var section = new CourseSection(vertices);
            section.Friction = 0.02f + (float)_random.NextDouble() * 0.08f; // Very slick for faster drops
            section.Color = SKColors.DarkCyan;
            section.GlowIntensity = 0.3f;
            
            return section;
        }
        
        private CourseSection CreateBankedTurnSection(SegmentContext context)
        {
            // Calculate curvature direction and bank angle
            float bankFactor = context.Angle > 0 ? 1f : -1f;
            float bankMagnitude = Math.Abs(context.Angle) * 1.5f;
            
            // Width parameters
            float width = 80f + (float)_random.NextDouble() * 30f; // Wider for turns
            float outerWallHeight = width * (0.6f + bankMagnitude * 0.3f); // Higher outer wall
            float innerWallHeight = width * 0.3f; // Lower inner wall
            
            Vector2 normal = context.Normal;
            
            // Determine which side is the outer bank based on turn direction
            Vector2 outerNormal = bankFactor > 0 ? normal : -normal;
            Vector2 innerNormal = bankFactor > 0 ? -normal : normal;
            
            // Create a banked curve with higher outer wall and lower inner wall
            List<Vector2> vertices = new List<Vector2>
            {
                // Outer bank start (higher wall)
                context.Start + outerNormal * (width + outerWallHeight),
                
                // Outer bank inner edge
                context.Start + outerNormal * width * 0.9f,
                
                // Inner bank
                context.Start + innerNormal * width * 0.8f,
                
                // Inner bank wall (lower)
                context.Start + innerNormal * (width + innerWallHeight),
                
                // Inner bank wall end (lower)
                context.End + innerNormal * (width + innerWallHeight),
                
                // Inner bank end
                context.End + innerNormal * width * 0.8f,
                
                // Outer bank inner edge end
                context.End + outerNormal * width * 0.9f,
                
                // Outer bank end (higher wall)
                context.End + outerNormal * (width + outerWallHeight)
            };
            
            var section = new CourseSection(vertices);
            section.Friction = 0.1f + (float)_random.NextDouble() * 0.15f; // Moderate friction for turns
            section.Color = SKColors.SandyBrown;
            
            return section;
        }
        
        private CourseSection CreateBoostZoneSection(SegmentContext context)
        {
            // A straight section with visual indicators of boost
            float width = 65f + (float)_random.NextDouble() * 15f;
            float wallHeight = width * 0.4f;
            
            Vector2 normal = context.Normal;
            
            // Create a standard path but with boost properties
            List<Vector2> vertices = new List<Vector2>
            {
                // Left wall
                context.Start + normal * (width + wallHeight),
                
                // Left edge
                context.Start + normal * width,
                
                // Path points
                context.Start - normal * width,
                context.End - normal * width,
                
                // Right edge
                context.End + normal * width,
                
                // Right wall
                context.End + normal * (width + wallHeight)
            };
            
            var section = new CourseSection(vertices);
            section.Friction = 0.01f; // Very low friction for boost effect
            section.Color = SKColors.LimeGreen;
            section.GlowIntensity = 0.8f; // Strong glow for visual effect
            
            return section;
        }
        
        private void OptimizePhysicsProperties(Course course)
        {
            for (int i = 0; i < course.Sections.Count; i++)
            {
                var section = course.Sections[i];
                var nextSection = i < course.Sections.Count - 1 ? course.Sections[i+1] : null;
                
                // Adjust friction based on incline
                float incline = CalculateIncline(section);
                section.Friction = BaseFrictionForIncline(incline);
                
                // Match bounciness between connected sections
                if (nextSection != null)
                {
                    section.Bounciness = MatchBounciness(section, nextSection);
                }
                
                // Add wall collision adjustments
                ApplyWallPhysics(section);
            }
        }
        
        private float CalculateIncline(CourseSection section)
        {
            // Calculate the average incline of this section
            if (section.Vertices.Count < 4) return 0f;
            
            // Get average start and end y-positions
            float startY = (section.Vertices[0].Y + section.Vertices[1].Y) / 2f;
            float endY = (section.Vertices[section.Vertices.Count - 2].Y + section.Vertices[section.Vertices.Count - 1].Y) / 2f;
            
            // Get average x distance
            float startX = (section.Vertices[0].X + section.Vertices[1].X) / 2f;
            float endX = (section.Vertices[section.Vertices.Count - 2].X + section.Vertices[section.Vertices.Count - 1].X) / 2f;
            
            float xDist = Math.Abs(endX - startX);
            if (xDist < 0.01f) return 1f; // Prevent division by zero, assume steep incline
            
            float yDiff = endY - startY;
            return yDiff / xDist; // Positive for downhill, negative for uphill
        }
        
        private float BaseFrictionForIncline(float incline)
        {
            // Adjust friction based on incline - steeper inclines should have less friction
            if (incline > 0.8f)
            {
                // Very steep downhill - very low friction for speed
                return 0.01f + (float)_random.NextDouble() * 0.05f;
            }
            else if (incline > 0.3f)
            {
                // Moderate downhill - moderate friction
                return 0.05f + (float)_random.NextDouble() * 0.1f;
            }
            else if (incline > -0.1f)
            {
                // Relatively flat - normal friction
                return 0.1f + (float)_random.NextDouble() * 0.2f;
            }
            else
            {
                // Uphill - higher friction to help marbles climb
                return 0.2f + (float)_random.NextDouble() * 0.3f;
            }
        }
        
        private float MatchBounciness(CourseSection current, CourseSection next)
        {
            // Return current bounciness if it's already been explicitly set
            if (current.Type != CourseSection.SectionType.Normal && current.Bounciness > 0.1f)
            {
                return current.Bounciness;
            }
            
            // Match bounciness at section transitions for smoother gameplay
            // Default bounciness
            float baseBounce = 0.2f + (float)_random.NextDouble() * 0.1f;
            
            // For special bouncy sections
            if (next.Type == CourseSection.SectionType.Bumpers)
            {
                return Math.Min(0.7f, next.Bounciness - 0.2f); // Gradual increase in bounciness
            }
            
            return baseBounce;
        }
        
        private void ApplyWallPhysics(CourseSection section)
        {
            // Special physics properties for wall collision areas
            if (section.Vertices.Count < 6) return;
            
            // Special walls have different physics than the main path
            // This would typically be implemented by tagging vertices with physical properties
            // or splitting the section into multiple physical bodies in the physics system
            
            // In this simulation, we'll just update section-wide properties based on the type
            switch (section.Type)
            {
                case CourseSection.SectionType.Funnel:
                    // Funnels should guide marbles smoothly
                    section.Bounciness = Math.Min(section.Bounciness, 0.1f); // Absorb impacts for smoother flow
                    break;
                case CourseSection.SectionType.Ramp:
                    // Ramps should have slightly bouncy edges to keep marbles on track
                    section.Bounciness = Math.Max(section.Bounciness, 0.3f); 
                    break;
                case CourseSection.SectionType.Bumpers:
                    // Already very bouncy
                    break;
                default:
                    // For normal sections, make walls slightly bouncy to keep marbles in bounds
                    if (section.Bounciness < 0.2f)
                    {
                        section.Bounciness = 0.2f + (float)_random.NextDouble() * 0.1f;
                    }
                    break;
            }
        }
    }
}
