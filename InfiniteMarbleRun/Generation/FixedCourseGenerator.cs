using System;
using System.Collections.Generic;
using tainicom.Aether.Physics2D.Common;
using SkiaSharp;
using InfiniteMarbleRun.Core;

namespace InfiniteMarbleRun.Generation
{
    /// <summary>
    /// Generates a fixed, pre-designed course for consistent marble races
    /// </summary>
    public class FixedCourseGenerator
    {
        private Random _random;
        private int _width;
        private int _height;
        
        public FixedCourseGenerator(Random random, int width, int height)
        {
            _random = random;
            _width = width;
            _height = height;
        }
        
        public Course Generate()
        {
            // Initialize course with basic properties
            var course = new Course(_width, _height, 5, _random.Next());
            
            // Create a fixed track with interesting elements
            CreateFixedTrack(course);
            
            // Set visual theme
            ApplyVisualTheme(course);
            
            return course;
        }        private void CreateFixedTrack(Course course)
        {
            float centerX = _width * 0.5f;
            float startY = _height * 0.05f;
            float finishY = _height * 0.95f;

            // Set start and finish positions
            course.StartPosition = new Vector2(centerX, startY);
            course.FinishPosition = new Vector2(centerX, finishY);

            // Add checkpoints for progress tracking
            AddCheckpoints(course);

            // --- Brand new course layout based on the tile patterns ---
            CreateStartPlatform(course);          // Starting platform with central gap
            CreateWavyPath(course);               // Wavy/zigzag pattern (row 1, col 9)
            CreateDottedPassage(course);          // Dotted pattern (row 1, col 7)
            CreateSpiralSection(course);          // Spiral pattern (row 3, col 5)
            CreateGridSection(course);            // Grid pattern (row 3, col 6)
            CreateCurvySlalom(course);            // Wavy slalom (row 3, cols 2-3)
            CreateCircularObstacles(course);      // Circular obstacles (row 4, col 2)
            CreateStraightBoost(course);          // Straight boost section (row 4, col 7)
            CreateFinishSegment(course);          // Final path to finish
        }        private void AddCheckpoints(Course course)
        {
            // Simplified checkpoints at strategic locations
            float centerX = _width * 0.5f;
            
            // Start checkpoint
            course.AddCheckpoint(new Vector2(centerX, _height * 0.05f));
            
            // Middle checkpoints
            course.AddCheckpoint(new Vector2(centerX, _height * 0.25f));
            course.AddCheckpoint(new Vector2(centerX, _height * 0.45f));
            course.AddCheckpoint(new Vector2(centerX, _height * 0.65f));
            course.AddCheckpoint(new Vector2(centerX, _height * 0.85f));
            
            // Finish checkpoint
            course.AddCheckpoint(new Vector2(centerX, _height * 0.95f));
        }        private void CreateStartPlatform(Course course)
        {
            // Start platform with central gap for marbles
            float startY = _height * 0.05f;
            float platformWidth = _width * 0.42f;
            float platformHeight = _height * 0.03f;
            float centerX = _width * 0.5f;
            float gapWidth = _width * 0.14f;

            // Left platform segment
            List<Vector2> leftPlatform = new List<Vector2>
            {
                new Vector2(centerX - platformWidth/2, startY),
                new Vector2(centerX - platformWidth/2, startY + platformHeight),
                new Vector2(centerX - gapWidth/2, startY + platformHeight),
                new Vector2(centerX - gapWidth/2, startY),
            };
            var leftSection = new CourseSection(leftPlatform);
            leftSection.Type = CourseSection.SectionType.Normal;
            leftSection.Color = new SKColor(180, 180, 180); // Silver gray
            leftSection.Friction = 0.02f;
            leftSection.GlowIntensity = 0.5f;
            course.AddSection(leftSection);

            // Right platform segment
            List<Vector2> rightPlatform = new List<Vector2>
            {
                new Vector2(centerX + gapWidth/2, startY),
                new Vector2(centerX + gapWidth/2, startY + platformHeight),
                new Vector2(centerX + platformWidth/2, startY + platformHeight),
                new Vector2(centerX + platformWidth/2, startY),
            };
            var rightSection = new CourseSection(rightPlatform);
            rightSection.Type = CourseSection.SectionType.Normal;
            rightSection.Color = new SKColor(180, 180, 180); // Silver gray
            rightSection.Friction = 0.02f;
            rightSection.GlowIntensity = 0.5f;
            course.AddSection(rightSection);
        }

        private void CreateWavyPath(Course course)
        {
            // Wavy path inspired by row 1, col 9 or row 2, col 9 wave patterns
            float sectionStartY = _height * 0.09f;
            float sectionHeight = _height * 0.13f;
            float centerX = _width * 0.5f;
            float amplitude = _width * 0.15f;
            int waves = 3;
            float pathWidth = 30f;
            
            // Create a wavy path
            List<Vector2> wavyOuterPath = new List<Vector2>();
            List<Vector2> wavyInnerPath = new List<Vector2>();
            
            int segments = 48;
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float x = centerX + amplitude * (float)Math.Sin(t * Math.PI * 2 * waves);
                float y = sectionStartY + sectionHeight * t;
                
                // Calculate normal vector to create path width
                float nextT = Math.Min(1, (i + 1) / (float)segments);
                float nextX = centerX + amplitude * (float)Math.Sin(nextT * Math.PI * 2 * waves);
                float nextY = sectionStartY + sectionHeight * nextT;
                  Vector2 direction = new Vector2(nextX - x, nextY - y);
                direction.Normalize();
                Vector2 normal = new Vector2(-direction.Y, direction.X);
                
                wavyOuterPath.Add(new Vector2(x + normal.X * pathWidth, y + normal.Y * pathWidth));
                wavyInnerPath.Insert(0, new Vector2(x - normal.X * pathWidth, y - normal.Y * pathWidth));
            }
            
            List<Vector2> combinedPath = new List<Vector2>();
            combinedPath.AddRange(wavyOuterPath);
            combinedPath.AddRange(wavyInnerPath);
            
            var wavySection = new CourseSection(combinedPath);
            wavySection.Type = CourseSection.SectionType.Normal;
            wavySection.Color = new SKColor(100, 180, 255); // Light blue like in the image
            wavySection.Friction = 0.05f;
            wavySection.GlowIntensity = 0.4f;
            course.AddSection(wavySection);
        }

        private void CreateBoostRamp(Course course)
        {
            // Boost ramp (750N Forward Force)
            float rampStartY = _height * 0.08f;
            float rampEndY = _height * 0.13f;
            float rampWidth = _width * 0.3f;
            float centerX = _width * 0.5f;
            
            // Ramp vertices - angled downward for speed
            List<Vector2> ramp = new List<Vector2>
            {
                new Vector2(centerX - rampWidth/2, rampStartY),
                new Vector2(centerX - rampWidth/4, rampEndY),
                new Vector2(centerX + rampWidth/4, rampEndY),
                new Vector2(centerX + rampWidth/2, rampStartY),
            };
            
            var rampSection = new CourseSection(ramp);
            rampSection.Type = CourseSection.SectionType.Booster;
            rampSection.Color = SKColors.LimeGreen;
            rampSection.Friction = 0.01f; // Very low friction for speed boost
            rampSection.GlowIntensity = 0.8f; // Strong glow to indicate boost
            course.AddSection(rampSection);
        }
        
        private void CreateZigzagSection(Course course)
        {
            float sectionStartY = _height * 0.18f;
            float sectionHeight = _height * 0.12f;
            float pathWidth = 60f;
            
            // Create a zigzagging path
            // First zig right
            List<Vector2> zigRight = new List<Vector2>
            {
                new Vector2(_width * 0.3f, sectionStartY),
                new Vector2(_width * 0.3f, sectionStartY + pathWidth),
                new Vector2(_width * 0.7f, sectionStartY + sectionHeight),
                new Vector2(_width * 0.7f, sectionStartY + sectionHeight - pathWidth),
            };
            
            var zigRightSection = new CourseSection(zigRight);
            zigRightSection.Type = CourseSection.SectionType.Normal;
            zigRightSection.Color = SKColors.MediumPurple;
            course.AddSection(zigRightSection);
            
            // Then zag left
            List<Vector2> zagLeft = new List<Vector2>
            {
                new Vector2(_width * 0.7f, sectionStartY + sectionHeight - pathWidth),
                new Vector2(_width * 0.7f, sectionStartY + sectionHeight),
                new Vector2(_width * 0.3f, sectionStartY + sectionHeight * 2),
                new Vector2(_width * 0.3f, sectionStartY + sectionHeight * 2 - pathWidth),
            };
            
            var zagLeftSection = new CourseSection(zagLeft);
            zagLeftSection.Type = CourseSection.SectionType.Normal;
            zagLeftSection.Color = SKColors.MediumSlateBlue;
            course.AddSection(zagLeftSection);
        }
        
        private void CreateFunnelSection(Course course)
        {
            float sectionStartY = _height * 0.42f;
            float sectionHeight = _height * 0.08f;
            
            // Create a funnel to channel marbles
            List<Vector2> funnel = new List<Vector2>
            {
                new Vector2(_width * 0.3f, sectionStartY),
                new Vector2(_width * 0.2f, sectionStartY + sectionHeight * 0.5f),
                new Vector2(_width * 0.4f, sectionStartY + sectionHeight),
                new Vector2(_width * 0.6f, sectionStartY + sectionHeight),
                new Vector2(_width * 0.8f, sectionStartY + sectionHeight * 0.5f),
                new Vector2(_width * 0.7f, sectionStartY),
            };
            
            var funnelSection = new CourseSection(funnel);
            funnelSection.Type = CourseSection.SectionType.Funnel;
            funnelSection.Color = SKColors.DarkOrange;
            funnelSection.GlowIntensity = 0.3f;
            course.AddSection(funnelSection);
        }
        
        private void CreateSpeedBoostSection(Course course)
        {
            float sectionStartY = _height * 0.5f;
            float sectionHeight = _height * 0.1f;
            float pathWidth = 70f;
            
            // Create a straight speed boost section
            List<Vector2> speedBoost = new List<Vector2>
            {
                new Vector2(_width * 0.5f - pathWidth, sectionStartY),
                new Vector2(_width * 0.5f - pathWidth, sectionStartY + sectionHeight),
                new Vector2(_width * 0.5f + pathWidth, sectionStartY + sectionHeight),
                new Vector2(_width * 0.5f + pathWidth, sectionStartY),
            };
            
            var boostSection = new CourseSection(speedBoost);
            boostSection.Type = CourseSection.SectionType.Booster;
            boostSection.Color = SKColors.LimeGreen;
            boostSection.Friction = 0.01f; // Very low friction for speed
            boostSection.GlowIntensity = 0.7f;
            course.AddSection(boostSection);
        }
        
        private void CreateCorkScrewSection(Course course)
        {
            float sectionStartY = _height * 0.6f;
            float sectionHeight = _height * 0.1f;
            float centerX = _width * 0.5f;
            float radius = _width * 0.2f;
            
            // Create a corkscrew/spiral shape with multiple segments
            List<Vector2> corkScrew = new List<Vector2>();
              // Create the spiral shape with multiple points
            for (int i = 0; i < 10; i++)
            {
                float angle = i * Core.MathHelper.TwoPi / 10;
                float x = centerX + radius * (float)Math.Cos(angle);
                float y = sectionStartY + (sectionHeight * i / 10);
                corkScrew.Add(new Vector2(x, y));
            }
              // Connect back to make a tube
            for (int i = 9; i >= 0; i--)
            {
                float angle = i * Core.MathHelper.TwoPi / 10;
                float x = centerX + (radius - 50) * (float)Math.Cos(angle);
                float y = sectionStartY + (sectionHeight * i / 10);
                corkScrew.Add(new Vector2(x, y));
            }
            
            var corkScrewSection = new CourseSection(corkScrew);
            corkScrewSection.Type = CourseSection.SectionType.Spinner;
            corkScrewSection.Color = SKColors.Purple;
            corkScrewSection.GlowIntensity = 0.5f;
            course.AddSection(corkScrewSection);
        }
        
        private void CreateBumperSection(Course course)
        {
            float sectionStartY = _height * 0.7f;
            float sectionHeight = _height * 0.1f;
            float pathWidth = 100f;
            
            // Create a bumper section with obstacles
            List<Vector2> bumperArea = new List<Vector2>
            {
                new Vector2(_width * 0.3f, sectionStartY),
                new Vector2(_width * 0.3f, sectionStartY + sectionHeight),
                new Vector2(_width * 0.7f, sectionStartY + sectionHeight),
                new Vector2(_width * 0.7f, sectionStartY),
            };
            
            var bumperSection = new CourseSection(bumperArea);
            bumperSection.Type = CourseSection.SectionType.Bumpers;
            bumperSection.Color = SKColors.Crimson;
            bumperSection.Bounciness = 0.9f;
            bumperSection.GlowIntensity = 0.4f;
            course.AddSection(bumperSection);
        }
        
        private void CreateFinishSection(Course course)
        {
            float sectionStartY = _height * 0.8f;
            float sectionHeight = _height * 0.1f;
            
            // Final slide to the finish
            List<Vector2> finishSlide = new List<Vector2>
            {
                new Vector2(_width * 0.3f, sectionStartY),
                new Vector2(_width * 0.4f, sectionStartY + sectionHeight),
                new Vector2(_width * 0.6f, sectionStartY + sectionHeight),
                new Vector2(_width * 0.7f, sectionStartY),
            };
            
            var finishSection = new CourseSection(finishSlide);
            finishSection.Type = CourseSection.SectionType.Normal;
            finishSection.Color = SKColors.Gold;
            finishSection.GlowIntensity = 0.6f;
            course.AddSection(finishSection);
        }
        
        private void ApplyVisualTheme(Course course)
        {
            // Set a consistent visual theme based on the blue tiles from the image
            course.PrimaryColor = new SKColor(50, 150, 255); // Blue primary
            course.SecondaryColor = new SKColor(140, 80, 200); // Purple secondary
            course.AccentColor = new SKColor(40, 200, 100); // Green accent
            
            // Enhance visual appeal for certain section types
            foreach (var section in course.Sections)
            {
                // Keep any custom colors set but update glow intensities based on type
                if (section.Type == CourseSection.SectionType.Booster)
                {
                    section.GlowIntensity = Math.Max(section.GlowIntensity, 0.8f);
                }
                else if (section.Type == CourseSection.SectionType.Bumpers)
                {
                    section.GlowIntensity = Math.Max(section.GlowIntensity, 0.6f);
                    section.Bounciness = Math.Max(section.Bounciness, 0.8f);
                }
                else if (section.Type == CourseSection.SectionType.Spinner)
                {
                    section.GlowIntensity = Math.Max(section.GlowIntensity, 0.7f);
                }
            }
        }
        
        private void CreateObstacleArena(Course course)
        {
            // Random obstacle arena with pendulums, pistons, and gravity inverters
            float arenaStartY = _height * 0.38f;
            float arenaHeight = _height * 0.1f;
            float arenaWidth = _width * 0.6f;
            float centerX = _width * 0.5f;
            
            // Main arena area
            List<Vector2> arenaBase = new List<Vector2>
            {
                new Vector2(centerX - arenaWidth/2, arenaStartY),
                new Vector2(centerX - arenaWidth/2, arenaStartY + arenaHeight),
                new Vector2(centerX + arenaWidth/2, arenaStartY + arenaHeight),
                new Vector2(centerX + arenaWidth/2, arenaStartY),
            };
            
            var arenaSection = new CourseSection(arenaBase);
            arenaSection.Type = CourseSection.SectionType.Normal;
            arenaSection.Color = SKColors.DarkSlateBlue;
            arenaSection.Friction = 0.1f;
            arenaSection.GlowIntensity = 0.3f;
            course.AddSection(arenaSection);
            
            // Add pendulum obstacles
            AddPendulumObstacles(course, centerX, arenaStartY);
            
            // Add piston obstacles
            AddPistonObstacles(course, centerX, arenaStartY + arenaHeight/2);
            
            // Add destructible floor elements
            AddDestructibleFloor(course, centerX, arenaStartY + arenaHeight * 0.8f, arenaWidth * 0.8f);
        }
        
        private void AddPendulumObstacles(Course course, float centerX, float baseY)
        {
            // Left pendulum blade
            List<Vector2> leftBlade = new List<Vector2>
            {
                new Vector2(centerX - 120, baseY + 15),
                new Vector2(centerX - 80, baseY + 40),
                new Vector2(centerX - 40, baseY + 15),
                new Vector2(centerX - 80, baseY - 10),
            };
            
            var leftBladeSection = new CourseSection(leftBlade);
            leftBladeSection.Type = CourseSection.SectionType.Bumpers;
            leftBladeSection.Color = SKColors.Crimson;
            leftBladeSection.Bounciness = 0.7f;
            leftBladeSection.GlowIntensity = 0.5f;
            course.AddSection(leftBladeSection);
            
            // Right pendulum blade
            List<Vector2> rightBlade = new List<Vector2>
            {
                new Vector2(centerX + 40, baseY + 15),
                new Vector2(centerX + 80, baseY + 40),
                new Vector2(centerX + 120, baseY + 15),
                new Vector2(centerX + 80, baseY - 10),
            };
            
            var rightBladeSection = new CourseSection(rightBlade);
            rightBladeSection.Type = CourseSection.SectionType.Bumpers;
            rightBladeSection.Color = SKColors.Crimson;
            rightBladeSection.Bounciness = 0.7f;
            rightBladeSection.GlowIntensity = 0.5f;
            course.AddSection(rightBladeSection);
        }
        
        private void AddPistonObstacles(Course course, float centerX, float baseY)
        {
            float pistonWidth = 30f;
            float pistonHeight = 60f;
            
            // Left piston
            List<Vector2> leftPiston = new List<Vector2>
            {
                new Vector2(centerX - pistonWidth - 60, baseY - pistonHeight/2),
                new Vector2(centerX - pistonWidth - 60, baseY + pistonHeight/2),
                new Vector2(centerX - 60, baseY + pistonHeight/2),
                new Vector2(centerX - 60, baseY - pistonHeight/2),
            };
            
            var leftPistonSection = new CourseSection(leftPiston);
            leftPistonSection.Type = CourseSection.SectionType.Booster;
            leftPistonSection.Color = SKColors.Orange;
            leftPistonSection.Bounciness = 0.8f;
            course.AddSection(leftPistonSection);
            
            // Right piston
            List<Vector2> rightPiston = new List<Vector2>
            {
                new Vector2(centerX + 60, baseY - pistonHeight/2),
                new Vector2(centerX + 60, baseY + pistonHeight/2),
                new Vector2(centerX + pistonWidth + 60, baseY + pistonHeight/2),
                new Vector2(centerX + pistonWidth + 60, baseY - pistonHeight/2),
            };
            
            var rightPistonSection = new CourseSection(rightPiston);
            rightPistonSection.Type = CourseSection.SectionType.Booster;
            rightPistonSection.Color = SKColors.Orange;
            rightPistonSection.Bounciness = 0.8f;
            course.AddSection(rightPistonSection);
        }
        
        private void AddDestructibleFloor(Course course, float centerX, float baseY, float width)
        {
            // Create a segmented floor that looks "destructible"
            int segments = 5;
            float segmentWidth = width / segments;
            
            for (int i = 0; i < segments; i++)
            {
                float segStartX = centerX - width/2 + i * segmentWidth;
                float segEndX = segStartX + segmentWidth * 0.9f; // Gap between segments
                
                List<Vector2> floorSegment = new List<Vector2>
                {
                    new Vector2(segStartX, baseY),
                    new Vector2(segStartX, baseY + 10),
                    new Vector2(segEndX, baseY + 10),
                    new Vector2(segEndX, baseY),
                };
                
                var floorSection = new CourseSection(floorSegment);
                floorSection.Type = CourseSection.SectionType.Normal;
                floorSection.Color = SKColors.SandyBrown;
                floorSection.Friction = 0.3f;
                course.AddSection(floorSection);
            }
        }
        
        private void CreateSwitchbackMaze(Course course)
        {
            // Switchback maze with tilting platforms
            float mazeStartY = _height * 0.48f;
            float mazeHeight = _height * 0.1f;
            float centerX = _width * 0.5f;
            
            // Create a zigzagging path
            CreateMazePath(course, centerX, mazeStartY, mazeHeight, true);  // Right path
            CreateMazePath(course, centerX, mazeStartY + mazeHeight * 0.33f, mazeHeight * 0.33f, false);  // Left path
            CreateMazePath(course, centerX, mazeStartY + mazeHeight * 0.66f, mazeHeight * 0.34f, true);  // Right path again
            
            // Add a pressure plate that "redirects" the path
            AddPressurePlate(course, centerX, mazeStartY + mazeHeight * 0.5f);
        }
        
        private void CreateMazePath(Course course, float centerX, float startY, float height, bool goingRight)
        {
            float pathWidth = 50f;
            float xOffset = _width * 0.25f;
            
            List<Vector2> path = new List<Vector2>();
            if (goingRight)
            {
                path.Add(new Vector2(centerX - xOffset, startY));
                path.Add(new Vector2(centerX - xOffset, startY + pathWidth));
                path.Add(new Vector2(centerX + xOffset, startY + height));
                path.Add(new Vector2(centerX + xOffset, startY + height - pathWidth));
            }
            else
            {
                path.Add(new Vector2(centerX + xOffset, startY));
                path.Add(new Vector2(centerX + xOffset, startY + pathWidth));
                path.Add(new Vector2(centerX - xOffset, startY + height));
                path.Add(new Vector2(centerX - xOffset, startY + height - pathWidth));
            }
            
            var pathSection = new CourseSection(path);
            pathSection.Type = CourseSection.SectionType.Normal;
            pathSection.Color = SKColors.DarkCyan;
            pathSection.Friction = 0.1f;
            course.AddSection(pathSection);
        }
        
        private void AddPressurePlate(Course course, float centerX, float y)
        {
            // Add circular pressure plate
            float plateRadius = 20f;
            List<Vector2> platePoints = new List<Vector2>();
            
            // Create a circle
            int numPoints = 12;
            for (int i = 0; i < numPoints; i++)
            {
                float angle = i * Core.MathHelper.TwoPi / numPoints;
                float x = centerX + plateRadius * (float)Math.Cos(angle);
                float yPos = y + plateRadius * (float)Math.Sin(angle);
                platePoints.Add(new Vector2(x, yPos));
            }
            
            var plateSection = new CourseSection(platePoints);
            plateSection.Type = CourseSection.SectionType.SlowField;
            plateSection.Color = SKColors.Cyan;
            plateSection.GlowIntensity = 0.7f;
            plateSection.Friction = 0.5f; // High friction to slow down
            course.AddSection(plateSection);
        }
        
        private void CreateSpiralZone(Course course)
        {
            // Triple-track corkscrew with magnet arms
            float spiralStartY = _height * 0.13f;
            float spiralHeight = _height * 0.12f;
            float centerX = _width * 0.5f;
            float baseRadius = _width * 0.2f;
            
            // Create three spiral tracks in parallel
            CreateSpiralTrack(course, centerX, spiralStartY, spiralHeight, baseRadius, 0, SKColors.Red);
            CreateSpiralTrack(course, centerX, spiralStartY, spiralHeight, baseRadius, 1, SKColors.Blue);
            CreateSpiralTrack(course, centerX, spiralStartY, spiralHeight, baseRadius, 2, SKColors.Green);
            
            // Add magnet arms at the middle of the spiral
            CreateMagnetArms(course, centerX, spiralStartY + spiralHeight * 0.5f);
        }
        
        private void CreateSpiralTrack(Course course, float centerX, float startY, float height, float baseRadius, int trackOffset, SKColor trackColor)
        {
            // Create a spiral track with parametric equations
            List<Vector2> spiralTrack = new List<Vector2>();
            int segments = 18;
            float trackWidth = 25f;
            
            // Track offset creates separation between the three tracks
            float radiusOffset = trackOffset * 15f;
            float angleOffset = trackOffset * 0.3f;
            
            // Create the outer edge of the spiral
            for (int i = 0; i <= segments; i++)
            {
                float progress = (float)i / segments;
                // Use parametric spiral equation r = a + bθ
                float radius = baseRadius + radiusOffset - progress * 30f;
                // 3 full rotations
                float angle = progress * Core.MathHelper.TwoPi * 3 + angleOffset;
                
                float x = centerX + radius * (float)Math.Cos(angle);
                float y = startY + height * progress;
                spiralTrack.Add(new Vector2(x, y));
            }
            
            // Create the inner edge of the spiral (go back in reverse)
            for (int i = segments; i >= 0; i--)
            {
                float progress = (float)i / segments;
                float radius = baseRadius + radiusOffset - progress * 30f - trackWidth;
                float angle = progress * Core.MathHelper.TwoPi * 3 + angleOffset;
                
                float x = centerX + radius * (float)Math.Cos(angle);
                float y = startY + height * progress;
                spiralTrack.Add(new Vector2(x, y));
            }
            
            var spiralSection = new CourseSection(spiralTrack);
            spiralSection.Type = CourseSection.SectionType.Normal;
            spiralSection.Color = trackColor;
            spiralSection.Friction = 0.05f;
            spiralSection.GlowIntensity = 0.4f;
            course.AddSection(spiralSection);
        }
        
        private void CreateMagnetArms(Course course, float centerX, float centerY)
        {
            // Create two magnet arms at opposite sides of the spiral
            float armLength = _width * 0.15f;
            float armWidth = 20f;
            
            // Left magnet arm
            List<Vector2> leftArm = new List<Vector2>
            {
                new Vector2(centerX - armLength, centerY - armWidth/2),
                new Vector2(centerX - armLength, centerY + armWidth/2),
                new Vector2(centerX, centerY + armWidth/2),
                new Vector2(centerX, centerY - armWidth/2),
            };
            
            var leftMagnet = new CourseSection(leftArm);
            leftMagnet.Type = CourseSection.SectionType.SlowField;
            leftMagnet.Color = SKColors.Cyan;
            leftMagnet.GlowIntensity = 0.7f;
            course.AddSection(leftMagnet);
            
            // Right magnet arm
            List<Vector2> rightArm = new List<Vector2>
            {
                new Vector2(centerX, centerY - armWidth/2),
                new Vector2(centerX, centerY + armWidth/2),
                new Vector2(centerX + armLength, centerY + armWidth/2),
                new Vector2(centerX + armLength, centerY - armWidth/2),
            };
            
            var rightMagnet = new CourseSection(rightArm);
            rightMagnet.Type = CourseSection.SectionType.SlowField;
            rightMagnet.Color = SKColors.Cyan;
            rightMagnet.GlowIntensity = 0.7f;
            course.AddSection(rightMagnet);
        }
        
        private void CreateLoopSwirl(Course course)
        {
            // Loop swirl with plasma bounce walls
            float loopStartY = _height * 0.25f;
            float loopHeight = _height * 0.1f;
            float centerX = _width * 0.5f;
            float radius = _width * 0.25f;
            
            // Create a circular loop with high bounce walls
            List<Vector2> outerLoop = new List<Vector2>();
            List<Vector2> innerLoop = new List<Vector2>();
            int numPoints = 24;
            float innerRadius = radius * 0.6f;
            
            // Create the circular loop points
            for (int i = 0; i <= numPoints; i++)
            {
                float angle = i * Core.MathHelper.TwoPi / numPoints;
                
                // Vary height with angle to create a sloped circular path
                float heightOffset = (float)Math.Sin(angle) * loopHeight * 0.3f;
                
                // Outer wall points
                outerLoop.Add(new Vector2(
                    centerX + radius * (float)Math.Cos(angle),
                    loopStartY + heightOffset
                ));
                
                // Inner wall points (in reverse order for later)
                innerLoop.Insert(0, new Vector2(
                    centerX + innerRadius * (float)Math.Cos(angle),
                    loopStartY + heightOffset
                ));
            }
            
            // Combine outer and inner points to form a complete loop
            var loopPoints = new List<Vector2>();
            loopPoints.AddRange(outerLoop);
            loopPoints.AddRange(innerLoop);
            
            var loopSection = new CourseSection(loopPoints);
            loopSection.Type = CourseSection.SectionType.Bumpers; // High bounce for plasma effect
            loopSection.Color = SKColors.MediumVioletRed;
            loopSection.Bounciness = 0.95f;
            loopSection.GlowIntensity = 0.8f;
            loopSection.Friction = 0.02f; // Low friction for maintaining speed
            course.AddSection(loopSection);
            
            // Add entry guide to the loop
            CreateLoopEntryGuide(course, centerX, loopStartY, radius);
            
            // Add exit guide from the loop
            CreateLoopExitGuide(course, centerX, loopStartY, radius);
        }
        
        private void CreateLoopEntryGuide(Course course, float centerX, float loopY, float radius)
        {
            float entryStartY = loopY - radius * 0.5f;
            float entryWidth = radius * 0.5f;
            
            // Entry guide to smoothly direct marbles into the loop
            List<Vector2> entryGuide = new List<Vector2>
            {
                new Vector2(centerX - entryWidth, entryStartY),
                new Vector2(centerX - radius * 0.9f, loopY),
                new Vector2(centerX - radius * 0.7f, loopY + radius * 0.1f),
                new Vector2(centerX, entryStartY + radius * 0.3f),
            };
            
            var entrySection = new CourseSection(entryGuide);
            entrySection.Type = CourseSection.SectionType.Normal;
            entrySection.Color = SKColors.DeepPink;
            entrySection.GlowIntensity = 0.5f;
            course.AddSection(entrySection);
        }
        
        private void CreateLoopExitGuide(Course course, float centerX, float loopY, float radius)
        {
            float exitEndY = loopY + radius * 1.0f;
            float exitWidth = radius * 0.5f;
            
            // Exit guide to smoothly transition marbles out of the loop
            List<Vector2> exitGuide = new List<Vector2>
            {
                new Vector2(centerX, loopY + radius * 0.7f),
                new Vector2(centerX + radius * 0.7f, loopY + radius * 0.1f),
                new Vector2(centerX + radius * 0.9f, loopY),
                new Vector2(centerX + exitWidth, exitEndY),
            };
            
            var exitSection = new CourseSection(exitGuide);
            exitSection.Type = CourseSection.SectionType.Normal;
            exitSection.Color = SKColors.DeepPink;
            exitSection.GlowIntensity = 0.5f;
            course.AddSection(exitSection);
        }
        
        private void CreateFireJetZone(Course course)
        {
            // Fire jet zone with upward thrust
            float jetZoneStartY = _height * 0.58f;
            float jetZoneHeight = _height * 0.07f;
            float centerX = _width * 0.5f;
            float zoneWidth = _width * 0.6f;
            
            // Create main path
            List<Vector2> jetZone = new List<Vector2>
            {
                new Vector2(centerX - zoneWidth/2, jetZoneStartY),
                new Vector2(centerX - zoneWidth/2, jetZoneStartY + jetZoneHeight),
                new Vector2(centerX + zoneWidth/2, jetZoneStartY + jetZoneHeight),
                new Vector2(centerX + zoneWidth/2, jetZoneStartY),
            };
            
            var jetZoneSection = new CourseSection(jetZone);
            jetZoneSection.Type = CourseSection.SectionType.Normal;
            jetZoneSection.Color = SKColors.DarkOrange;
            jetZoneSection.Friction = 0.1f;
            jetZoneSection.GlowIntensity = 0.3f;
            course.AddSection(jetZoneSection);
            
            // Add fire jets
            AddFireJets(course, centerX, jetZoneStartY, jetZoneHeight, zoneWidth);
        }
        
        private void AddFireJets(Course course, float centerX, float startY, float height, float width)
        {
            // Create several fire jet boosters along the path
            int numJets = 5;
            float jetWidth = 30f;
            float jetHeight = 50f;
            
            for (int i = 0; i < numJets; i++)
            {
                float xPos = centerX - width/2 + width * ((float)(i + 0.5f) / numJets);
                
                List<Vector2> jet = new List<Vector2>
                {
                    new Vector2(xPos - jetWidth/2, startY + height/2),
                    new Vector2(xPos, startY),
                    new Vector2(xPos + jetWidth/2, startY + height/2),
                    new Vector2(xPos, startY + jetHeight),
                };
                
                var jetSection = new CourseSection(jet);
                jetSection.Type = CourseSection.SectionType.Booster;
                jetSection.Color = SKColors.OrangeRed;
                jetSection.GlowIntensity = 0.9f;
                course.AddSection(jetSection);
            }
        }
        
        private void CreateElectroGrid(Course course)
        {
            // Electro grid with random boosts
            float gridStartY = _height * 0.65f;
            float gridHeight = _height * 0.05f;
            float centerX = _width * 0.5f;
            float gridWidth = _width * 0.7f;
            
            // Create main grid area
            List<Vector2> gridArea = new List<Vector2>
            {
                new Vector2(centerX - gridWidth/2, gridStartY),
                new Vector2(centerX - gridWidth/2, gridStartY + gridHeight),
                new Vector2(centerX + gridWidth/2, gridStartY + gridHeight),
                new Vector2(centerX + gridWidth/2, gridStartY),
            };
            
            var gridSection = new CourseSection(gridArea);
            gridSection.Type = CourseSection.SectionType.Normal;
            gridSection.Color = SKColors.DarkBlue;
            gridSection.GlowIntensity = 0.6f;
            course.AddSection(gridSection);
            
            // Add electrified tiles
            CreateElectroTiles(course, centerX, gridStartY, gridHeight, gridWidth);
        }
        
        private void CreateElectroTiles(Course course, float centerX, float startY, float height, float width)
        {
            // Create a grid of alternating electrified tiles
            int cols = 6;
            int rows = 2;
            float tileWidth = width / cols;
            float tileHeight = height / rows;
            
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    // Only add electrified tiles in a checkerboard pattern
                    if ((row + col) % 2 == 0)
                    {
                        float x = centerX - width/2 + col * tileWidth;
                        float y = startY + row * tileHeight;
                        
                        // Create slightly smaller than grid cells to see the pattern
                        List<Vector2> tile = new List<Vector2>
                        {
                            new Vector2(x + tileWidth * 0.1f, y + tileHeight * 0.1f),
                            new Vector2(x + tileWidth * 0.1f, y + tileHeight * 0.9f),
                            new Vector2(x + tileWidth * 0.9f, y + tileHeight * 0.9f),
                            new Vector2(x + tileWidth * 0.9f, y + tileHeight * 0.1f),
                        };
                        
                        var tileSection = new CourseSection(tile);
                        tileSection.Type = CourseSection.SectionType.Booster;
                        tileSection.Color = SKColors.YellowGreen;
                        tileSection.GlowIntensity = 0.8f;
                        course.AddSection(tileSection);
                    }
                }
            }
        }
        
        private void CreateLiquidMercuryPits(Course course)
        {
            // Liquid mercury pits with magnetic float
            float pitsStartY = _height * 0.7f;
            float pitsHeight = _height * 0.07f;
            float centerX = _width * 0.5f;
            float pitsWidth = _width * 0.8f;
            
            // Create containing area for mercury pits
            List<Vector2> pitsArea = new List<Vector2>
            {
                new Vector2(centerX - pitsWidth/2, pitsStartY),
                new Vector2(centerX - pitsWidth/2, pitsStartY + pitsHeight),
                new Vector2(centerX + pitsWidth/2, pitsStartY + pitsHeight),
                new Vector2(centerX + pitsWidth/2, pitsStartY),
            };
            
            var pitsSection = new CourseSection(pitsArea);
            pitsSection.Type = CourseSection.SectionType.Normal;
            pitsSection.Color = SKColors.Silver;
            pitsSection.GlowIntensity = 0.4f;
            course.AddSection(pitsSection);
            
            // Add mercury pools
            AddMercuryPools(course, centerX, pitsStartY, pitsHeight, pitsWidth);
            
            // Add bounce walls at sides
            AddPlasmaBounceWalls(course, centerX, pitsStartY, pitsHeight, pitsWidth);
        }
        
        private void AddMercuryPools(Course course, float centerX, float startY, float height, float width)
        {
            // Create mercury pools that appear like liquid
            int numPools = 3;
            float poolSize = 60f;
            
            for (int i = 0; i < numPools; i++)
            {
                float xPos = centerX - width/3 + width * ((float)i / (numPools - 1));
                float yPos = startY + height/2;
                
                // Create circular pool
                List<Vector2> poolPoints = new List<Vector2>();
                int numPoints = 16;
                
                for (int j = 0; j < numPoints; j++)
                {
                    float angle = j * Core.MathHelper.TwoPi / numPoints;
                    float radius = poolSize/2;
                    
                    // Add waviness to the pool edge
                    float waveOffset = (float)Math.Sin(angle * 4) * 5f;
                    
                    poolPoints.Add(new Vector2(
                        xPos + (radius + waveOffset) * (float)Math.Cos(angle),
                        yPos + (radius + waveOffset) * (float)Math.Sin(angle)
                    ));
                }
                
                var poolSection = new CourseSection(poolPoints);
                poolSection.Type = CourseSection.SectionType.SlowField; // Magnetic slowing effect
                poolSection.Color = new SKColor(180, 180, 190); // Mercury silver
                poolSection.GlowIntensity = 0.7f;
                poolSection.Friction = 0.8f; // High friction for the "magnetic pull" effect
                course.AddSection(poolSection);
            }
        }
        
        private void AddPlasmaBounceWalls(Course course, float centerX, float startY, float height, float width)
        {
            // Left plasma wall
            List<Vector2> leftWall = new List<Vector2>
            {
                new Vector2(centerX - width/2 - 10, startY),
                new Vector2(centerX - width/2 - 10, startY + height),
                new Vector2(centerX - width/2 + 10, startY + height),
                new Vector2(centerX - width/2 + 10, startY),
            };
            
            var leftWallSection = new CourseSection(leftWall);
            leftWallSection.Type = CourseSection.SectionType.Bumpers;
            leftWallSection.Color = SKColors.DeepPink;
            leftWallSection.Bounciness = 0.95f;
            leftWallSection.GlowIntensity = 0.8f;
            course.AddSection(leftWallSection);
            
            // Right plasma wall
            List<Vector2> rightWall = new List<Vector2>
            {
                new Vector2(centerX + width/2 - 10, startY),
                new Vector2(centerX + width/2 - 10, startY + height),
                new Vector2(centerX + width/2 + 10, startY + height),
                new Vector2(centerX + width/2 + 10, startY),
            };
            
            var rightWallSection = new CourseSection(rightWall);
            rightWallSection.Type = CourseSection.SectionType.Bumpers;
            rightWallSection.Color = SKColors.DeepPink;
            rightWallSection.Bounciness = 0.95f;
            rightWallSection.GlowIntensity = 0.8f;
            course.AddSection(rightWallSection);
        }
        
        private void CreateEndlessDropZone(Course course)
        {
            // Endless dropping zone
            float dropStartY = _height * 0.77f;
            float dropHeight = _height * 0.08f;
            float centerX = _width * 0.5f;
            
            // Create a series of stepped platforms for a "falling" effect
            int numPlatforms = 12;
            float platformWidth = _width * 0.5f;
            float platformHeight = 8f;
            float platformSpacing = dropHeight / numPlatforms;
            
            for (int i = 0; i < numPlatforms; i++)
            {
                float y = dropStartY + i * platformSpacing;
                float xOffset = ((i % 2) == 0) ? -40f : 40f;
                
                List<Vector2> platform = new List<Vector2>
                {
                    new Vector2(centerX + xOffset - platformWidth/2, y),
                    new Vector2(centerX + xOffset - platformWidth/2, y + platformHeight),
                    new Vector2(centerX + xOffset + platformWidth/2, y + platformHeight),
                    new Vector2(centerX + xOffset + platformWidth/2, y),
                };
                
                var platformSection = new CourseSection(platform);
                platformSection.Type = CourseSection.SectionType.Normal;
                
                // Create a rainbow gradient effect
                float hue = (i / (float)numPlatforms) * 360f;
                platformSection.Color = SKColor.FromHsl(hue, 80, 60);
                
                platformSection.Friction = 0.05f;
                platformSection.GlowIntensity = 0.7f - (i / (float)numPlatforms) * 0.5f; // Fade out glow as platforms go down
                course.AddSection(platformSection);
            }
            
            // Add vertical guide rails to keep marbles on track
            CreateDropZoneGuideRails(course, centerX, dropStartY, dropHeight);
        }
        
        private void CreateDropZoneGuideRails(Course course, float centerX, float startY, float height)
        {
            float railWidth = 10f;
            float railOffset = _width * 0.3f;
            
            // Left rail
            List<Vector2> leftRail = new List<Vector2>
            {
                new Vector2(centerX - railOffset - railWidth, startY),
                new Vector2(centerX - railOffset - railWidth, startY + height),
                new Vector2(centerX - railOffset + railWidth, startY + height),
                new Vector2(centerX - railOffset + railWidth, startY),
            };
            
            var leftRailSection = new CourseSection(leftRail);
            leftRailSection.Type = CourseSection.SectionType.Normal;
            leftRailSection.Color = SKColors.Indigo;
            leftRailSection.GlowIntensity = 0.6f;
            course.AddSection(leftRailSection);
            
            // Right rail
            List<Vector2> rightRail = new List<Vector2>
            {
                new Vector2(centerX + railOffset - railWidth, startY),
                new Vector2(centerX + railOffset - railWidth, startY + height),
                new Vector2(centerX + railOffset + railWidth, startY + height),
                new Vector2(centerX + railOffset + railWidth, startY),
            };
            
            var rightRailSection = new CourseSection(rightRail);
            rightRailSection.Type = CourseSection.SectionType.Normal;
            rightRailSection.Color = SKColors.Indigo;
            rightRailSection.GlowIntensity = 0.6f;
            course.AddSection(rightRailSection);
        }
        
        private void CreateVortexPortal(Course course)
        {
            // Vortex portal (infinite loop start)
            float vortexY = _height * 0.85f;
            float centerX = _width * 0.5f;
            float vortexRadius = _width * 0.15f;
            
            // Create a swirling vortex shape
            List<Vector2> vortexPoints = new List<Vector2>();
            int numSpirals = 4; // Number of spiral arms
            int pointsPerArm = 15;
            
            for (int arm = 0; arm < numSpirals; arm++)
            {
                float angleOffset = arm * Core.MathHelper.TwoPi / numSpirals;
                
                for (int i = 0; i < pointsPerArm; i++)
                {
                    float t = i / (float)pointsPerArm;
                    float radius = vortexRadius * (1.0f - 0.7f * t); // Spiral inward
                    float angle = angleOffset + Core.MathHelper.TwoPi * 2.0f * t;
                    
                    vortexPoints.Add(new Vector2(
                        centerX + radius * (float)Math.Cos(angle),
                        vortexY + radius * (float)Math.Sin(angle)
                    ));
                }
            }
            
            var vortexSection = new CourseSection(vortexPoints);
            vortexSection.Type = CourseSection.SectionType.Spinner;
            vortexSection.Color = new SKColor(80, 0, 100); // Deep purple
            vortexSection.GlowIntensity = 0.9f;
            course.AddSection(vortexSection);
            
            // Add central portal
            CreatePortalCenter(course, centerX, vortexY, vortexRadius * 0.3f);
        }
        
        private void CreatePortalCenter(Course course, float centerX, float centerY, float radius)
        {
            // Create the center of the vortex portal
            List<Vector2> portalPoints = new List<Vector2>();
            int numPoints = 16;
            
            for (int i = 0; i < numPoints; i++)
            {
                float angle = i * Core.MathHelper.TwoPi / numPoints;
                portalPoints.Add(new Vector2(
                    centerX + radius * (float)Math.Cos(angle),
                    centerY + radius * (float)Math.Sin(angle)
                ));
            }
            
            var portalSection = new CourseSection(portalPoints);
            portalSection.Type = CourseSection.SectionType.Booster;
            portalSection.Color = SKColors.Cyan;
            portalSection.GlowIntensity = 1.0f; // Maximum glow
            course.AddSection(portalSection);
        }
        
        private void CreateGlassFloor(Course course)
        {
            // Glass floor (reset checkpoint)
            float glassY = _height * 0.925f;
            float centerX = _width * 0.5f;
            float glassWidth = _width * 0.7f;
            float glassHeight = 8f;
            
            List<Vector2> glassFloor = new List<Vector2>
            {
                new Vector2(centerX - glassWidth/2, glassY),
                new Vector2(centerX - glassWidth/2, glassY + glassHeight),
                new Vector2(centerX + glassWidth/2, glassY + glassHeight),
                new Vector2(centerX + glassWidth/2, glassY),
            };
            
            var glassSection = new CourseSection(glassFloor);
            glassSection.Type = CourseSection.SectionType.Normal;
            glassSection.Color = new SKColor(150, 220, 255, 150); // Semi-transparent light blue
            glassSection.Friction = 0.02f; // Low friction like glass
            glassSection.GlowIntensity = 0.5f;
            course.AddSection(glassSection);
            
            // Add glass floor supports
            CreateGlassSupports(course, centerX, glassY, glassWidth);
        }
        
        private void CreateGlassSupports(Course course, float centerX, float glassY, float width)
        {
            // Add supports at each end of the glass floor
            float supportWidth = 20f;
            float supportHeight = 30f;
            
            // Left support
            List<Vector2> leftSupport = new List<Vector2>
            {
                new Vector2(centerX - width/2 - supportWidth/2, glassY),
                new Vector2(centerX - width/2 - supportWidth/2, glassY + supportHeight),
                new Vector2(centerX - width/2 + supportWidth/2, glassY + supportHeight),
                new Vector2(centerX - width/2 + supportWidth/2, glassY),
            };
            
            var leftSupportSection = new CourseSection(leftSupport);
            leftSupportSection.Type = CourseSection.SectionType.Normal;
            leftSupportSection.Color = SKColors.Silver;
            course.AddSection(leftSupportSection);
            
            // Right support
            List<Vector2> rightSupport = new List<Vector2>
            {
                new Vector2(centerX + width/2 - supportWidth/2, glassY),
                new Vector2(centerX + width/2 - supportWidth/2, glassY + supportHeight),
                new Vector2(centerX + width/2 + supportWidth/2, glassY + supportHeight),
                new Vector2(centerX + width/2 + supportWidth/2, glassY),
            };
            
            var rightSupportSection = new CourseSection(rightSupport);
            rightSupportSection.Type = CourseSection.SectionType.Normal;
            rightSupportSection.Color = SKColors.Silver;
            course.AddSection(rightSupportSection);
        }
        
        private void CreateFinishLine(Course course)
        {
            // Hidden finish line below the glass floor
            float finishY = _height * 0.95f;
            float centerX = _width * 0.5f;
            float finishWidth = _width * 0.5f;
            float finishHeight = 10f;
            
            List<Vector2> finishLine = new List<Vector2>
            {
                new Vector2(centerX - finishWidth/2, finishY),
                new Vector2(centerX - finishWidth/2, finishY + finishHeight),
                new Vector2(centerX + finishWidth/2, finishY + finishHeight),
                new Vector2(centerX + finishWidth/2, finishY),
            };
            
            var finishSection = new CourseSection(finishLine);
            finishSection.Type = CourseSection.SectionType.Normal;
            finishSection.Color = SKColors.Gold;
            finishSection.GlowIntensity = 1.0f; // Maximum glow for finish line
            course.AddSection(finishSection);
            
            // Add finish line decorations
            AddFinishLineFlags(course, centerX, finishY, finishWidth);
        }
        
        private void AddFinishLineFlags(Course course, float centerX, float finishY, float width)
        {
            float flagPoleWidth = 5f;
            float flagPoleHeight = 40f;
            float flagWidth = 25f;
            float flagHeight = 15f;
            
            // Left flag pole
            List<Vector2> leftPole = new List<Vector2>
            {
                new Vector2(centerX - width/2 - flagPoleWidth/2, finishY),
                new Vector2(centerX - width/2 - flagPoleWidth/2, finishY - flagPoleHeight),
                new Vector2(centerX - width/2 + flagPoleWidth/2, finishY - flagPoleHeight),
                new Vector2(centerX - width/2 + flagPoleWidth/2, finishY),
            };
            
            var leftPoleSection = new CourseSection(leftPole);
            leftPoleSection.Type = CourseSection.SectionType.Normal;
            leftPoleSection.Color = SKColors.White;
            course.AddSection(leftPoleSection);
            
            // Left flag
            List<Vector2> leftFlag = new List<Vector2>
            {
                new Vector2(centerX - width/2 + flagPoleWidth/2, finishY - flagPoleHeight + flagHeight),
                new Vector2(centerX - width/2 + flagPoleWidth/2, finishY - flagPoleHeight),
                new Vector2(centerX - width/2 + flagWidth, finishY - flagPoleHeight + flagHeight/2),
            };
            
            var leftFlagSection = new CourseSection(leftFlag);
            leftFlagSection.Type = CourseSection.SectionType.Normal;
            leftFlagSection.Color = SKColors.Black;
            course.AddSection(leftFlagSection);
            
            // Right flag pole
            List<Vector2> rightPole = new List<Vector2>
            {
                new Vector2(centerX + width/2 - flagPoleWidth/2, finishY),
                new Vector2(centerX + width/2 - flagPoleWidth/2, finishY - flagPoleHeight),
                new Vector2(centerX + width/2 + flagPoleWidth/2, finishY - flagPoleHeight),
                new Vector2(centerX + width/2 + flagPoleWidth/2, finishY),
            };
            
            var rightPoleSection = new CourseSection(rightPole);
            rightPoleSection.Type = CourseSection.SectionType.Normal;
            rightPoleSection.Color = SKColors.White;
            course.AddSection(rightPoleSection);
            
            // Right flag
            List<Vector2> rightFlag = new List<Vector2>
            {
                new Vector2(centerX + width/2 - flagPoleWidth/2, finishY - flagPoleHeight + flagHeight),
                new Vector2(centerX + width/2 - flagPoleWidth/2, finishY - flagPoleHeight),
                new Vector2(centerX + width/2 - flagWidth, finishY - flagPoleHeight + flagHeight/2),
            };
            
            var rightFlagSection = new CourseSection(rightFlag);
            rightFlagSection.Type = CourseSection.SectionType.Normal;
            rightFlagSection.Color = SKColors.Black;
            course.AddSection(rightFlagSection);
        }

        // --- New/Redesigned Section Methods ---
        private void CreateSpiralSection(Course course)
        {
            // Spiral section inspired by row 3, col 5
            float spiralCenterY = _height * 0.36f;
            float centerX = _width * 0.5f;
            float radius = _width * 0.18f;
            int spiralTurns = 2;
            int segments = 48;
            float trackWidth = 24f;
            
            List<Vector2> spiralOuter = new List<Vector2>();
            List<Vector2> spiralInner = new List<Vector2>();
            
            // Generate spiral path
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * spiralTurns * Core.MathHelper.TwoPi;
                // Gradually decrease radius as we move inward
                float currentRadius = radius * (1 - 0.7f * ((float)i / segments));
                
                float x = centerX + currentRadius * (float)Math.Cos(angle);
                float y = spiralCenterY + currentRadius * (float)Math.Sin(angle);
                
                // Calculate normal for path width
                float nextAngle = (float)(i + 1) / segments * spiralTurns * Core.MathHelper.TwoPi;
                float nextRadius = radius * (1 - 0.7f * ((float)(i + 1) / segments));
                float nextX = centerX + nextRadius * (float)Math.Cos(nextAngle);
                float nextY = spiralCenterY + nextRadius * (float)Math.Sin(nextAngle);
                  Vector2 direction = new Vector2(nextX - x, nextY - y);
                direction.Normalize();
                Vector2 normal = new Vector2(-direction.Y, direction.X);
                
                spiralOuter.Add(new Vector2(x + normal.X * trackWidth/2, y + normal.Y * trackWidth/2));
                spiralInner.Insert(0, new Vector2(x - normal.X * trackWidth/2, y - normal.Y * trackWidth/2));
            }
            
            List<Vector2> combinedSpiral = new List<Vector2>();
            combinedSpiral.AddRange(spiralOuter);
            combinedSpiral.AddRange(spiralInner);
            
            var spiralSection = new CourseSection(combinedSpiral);
            spiralSection.Type = CourseSection.SectionType.Normal;
            spiralSection.Color = new SKColor(60, 0, 90); // Dark purple
            spiralSection.GlowIntensity = 0.6f;
            spiralSection.Friction = 0.04f;
            course.AddSection(spiralSection);
        }

        private void CreateBumperArena(Course course)
        {
            // Pinball bumper/obstacle arena
            float arenaStartY = _height * 0.32f;
            float arenaHeight = _height * 0.09f;
            float arenaWidth = _width * 0.7f;
            float centerX = _width * 0.5f;

            // Arena base
            List<Vector2> arena = new List<Vector2>
            {
                new Vector2(centerX - arenaWidth/2, arenaStartY),
                new Vector2(centerX - arenaWidth/2, arenaStartY + arenaHeight),
                new Vector2(centerX + arenaWidth/2, arenaStartY + arenaHeight),
                new Vector2(centerX + arenaWidth/2, arenaStartY),
            };
            var arenaSection = new CourseSection(arena);
            arenaSection.Type = CourseSection.SectionType.Normal;
            arenaSection.Color = SKColors.DarkSlateBlue;
            arenaSection.Friction = 0.12f;
            course.AddSection(arenaSection);

            // Add circular bumpers (pinball style)
            int numBumpers = 5;
            float bumperRadius = 22f;
            for (int i = 0; i < numBumpers; i++)
            {
                float angle = Core.MathHelper.TwoPi * i / numBumpers;
                float bx = centerX + (arenaWidth * 0.28f) * (float)Math.Cos(angle);
                float by = arenaStartY + arenaHeight/2 + (arenaHeight * 0.18f) * (float)Math.Sin(angle);
                List<Vector2> bumper = new List<Vector2>();
                int points = 10;
                for (int j = 0; j < points; j++)
                {
                    float a = Core.MathHelper.TwoPi * j / points;
                    bumper.Add(new Vector2(
                        bx + bumperRadius * (float)Math.Cos(a),
                        by + bumperRadius * (float)Math.Sin(a)
                    ));
                }
                var bumperSection = new CourseSection(bumper);
                bumperSection.Type = CourseSection.SectionType.Bumpers;
                bumperSection.Color = SKColors.Crimson;
                bumperSection.Bounciness = 0.95f;
                bumperSection.GlowIntensity = 0.7f;
                course.AddSection(bumperSection);
            }
        }

        private void CreateSwitchbacks(Course course)
        {
            // Alternating left-right switchbacks
            float startY = _height * 0.42f;
            float sectionHeight = _height * 0.13f;
            float centerX = _width * 0.5f;
            float xOffset = _width * 0.22f;
            float pathWidth = 44f;
            int numSwitchbacks = 4;
            for (int i = 0; i < numSwitchbacks; i++)
            {
                bool right = (i % 2 == 0);
                float y0 = startY + (sectionHeight / numSwitchbacks) * i;
                float y1 = startY + (sectionHeight / numSwitchbacks) * (i + 1);
                List<Vector2> path = new List<Vector2>();
                if (right)
                {
                    path.Add(new Vector2(centerX - xOffset, y0));
                    path.Add(new Vector2(centerX - xOffset, y1 - pathWidth));
                    path.Add(new Vector2(centerX + xOffset, y1));
                    path.Add(new Vector2(centerX + xOffset, y0 + pathWidth));
                }
                else
                {
                    path.Add(new Vector2(centerX + xOffset, y0));
                    path.Add(new Vector2(centerX + xOffset, y1 - pathWidth));
                    path.Add(new Vector2(centerX - xOffset, y1));
                    path.Add(new Vector2(centerX - xOffset, y0 + pathWidth));
                }
                var section = new CourseSection(path);
                section.Type = CourseSection.SectionType.Normal;
                section.Color = right ? SKColors.MediumPurple : SKColors.MediumSlateBlue;
                section.Friction = 0.09f;
                course.AddSection(section);
            }
        }

        private void CreateLoopSwirlSection(Course course)
        {
            // Loop or swirl section (circular, high bounce)
            float loopY = _height * 0.57f;
            float centerX = _width * 0.5f;
            float radius = _width * 0.19f;
            int numPoints = 22;
            float innerRadius = radius * 0.65f;
            List<Vector2> outer = new List<Vector2>();
            List<Vector2> inner = new List<Vector2>();
            for (int i = 0; i <= numPoints; i++)
            {
                float angle = Core.MathHelper.TwoPi * i / numPoints;
                outer.Add(new Vector2(centerX + radius * (float)Math.Cos(angle), loopY + radius * (float)Math.Sin(angle)));
                inner.Insert(0, new Vector2(centerX + innerRadius * (float)Math.Cos(angle), loopY + innerRadius * (float)Math.Sin(angle)));
            }
            List<Vector2> points = new List<Vector2>();
            points.AddRange(outer);
            points.AddRange(inner);
            var loopSection = new CourseSection(points);
            loopSection.Type = CourseSection.SectionType.Bumpers;
            loopSection.Color = SKColors.DeepPink;
            loopSection.Bounciness = 0.98f;
            loopSection.GlowIntensity = 0.85f;
            loopSection.Friction = 0.03f;
            course.AddSection(loopSection);
        }

        private void CreateFinalSlide(Course course)
        {
            // Fast, straight final slide to finish
            float slideStartY = _height * 0.72f;
            float slideHeight = _height * 0.18f;
            float centerX = _width * 0.5f;
            float slideWidth = _width * 0.32f;
            List<Vector2> slide = new List<Vector2>
            {
                new Vector2(centerX - slideWidth/2, slideStartY),
                new Vector2(centerX - slideWidth/2, slideStartY + slideHeight),
                new Vector2(centerX + slideWidth/2, slideStartY + slideHeight),
                new Vector2(centerX + slideWidth/2, slideStartY),
            };
            var slideSection = new CourseSection(slide);
            slideSection.Type = CourseSection.SectionType.Booster;
            slideSection.Color = SKColors.LimeGreen;
            slideSection.Friction = 0.01f;
            slideSection.GlowIntensity = 0.8f;
            course.AddSection(slideSection);
        }

        private void CreateDottedPassage(Course course) 
        {
            // Dotted pattern inspired by row 1, col 7
            float sectionStartY = _height * 0.23f;
            float sectionHeight = _height * 0.11f;
            float centerX = _width * 0.5f;
            float pathWidth = _width * 0.3f;
            
            // Main passage
            List<Vector2> passagePath = new List<Vector2>
            {
                new Vector2(centerX - pathWidth/2, sectionStartY),
                new Vector2(centerX - pathWidth/2, sectionStartY + sectionHeight),
                new Vector2(centerX + pathWidth/2, sectionStartY + sectionHeight),
                new Vector2(centerX + pathWidth/2, sectionStartY),
            };
            
            var passageSection = new CourseSection(passagePath);
            passageSection.Type = CourseSection.SectionType.Normal;
            passageSection.Color = new SKColor(80, 80, 100); // Dark blue-gray
            passageSection.Friction = 0.08f;
            course.AddSection(passageSection);
            
            // Add dot obstacles
            int rows = 3;
            int dotsPerRow = 5;
            float dotRadius = 10f;
            float rowSpacing = sectionHeight / (rows + 1);
            float colSpacing = pathWidth / (dotsPerRow + 1);
            
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < dotsPerRow; col++)
                {
                    // Skip some dots to create a pattern (not all positions have dots)
                    if ((row + col) % 2 == 0)
                    {
                        float x = centerX - pathWidth/2 + colSpacing * (col + 1);
                        float y = sectionStartY + rowSpacing * (row + 1);
                        
                        List<Vector2> dot = new List<Vector2>();
                        int points = 8;
                        for (int i = 0; i < points; i++)
                        {
                            float angle = Core.MathHelper.TwoPi * i / points;
                            dot.Add(new Vector2(
                                x + dotRadius * (float)Math.Cos(angle),
                                y + dotRadius * (float)Math.Sin(angle)
                            ));
                        }
                        
                        var dotSection = new CourseSection(dot);
                        dotSection.Type = CourseSection.SectionType.Bumpers;
                        dotSection.Color = new SKColor(150, 150, 170); // Light gray with blue tint
                        dotSection.Bounciness = 0.7f;
                        dotSection.GlowIntensity = 0.5f;
                        course.AddSection(dotSection);
                    }
                }
            }
        }

        private void CreateGridSection(Course course)
        {
            // Grid pattern inspired by row 3, col 6
            float gridStartY = _height * 0.47f;
            float gridHeight = _height * 0.08f;
            float centerX = _width * 0.5f;
            float gridWidth = _width * 0.4f;
            
            // Create background grid area
            List<Vector2> gridBase = new List<Vector2>
            {
                new Vector2(centerX - gridWidth/2, gridStartY),
                new Vector2(centerX - gridWidth/2, gridStartY + gridHeight),
                new Vector2(centerX + gridWidth/2, gridStartY + gridHeight),
                new Vector2(centerX + gridWidth/2, gridStartY),
            };
            
            var gridSection = new CourseSection(gridBase);
            gridSection.Type = CourseSection.SectionType.Normal;
            gridSection.Color = new SKColor(20, 20, 80); // Very dark blue
            gridSection.Friction = 0.1f;
            course.AddSection(gridSection);
            
            // Create grid lines
            int rows = 3;
            int cols = 6;
            float lineWidth = 6f;
            float hBarWidth = gridWidth / cols;
            float vBarHeight = gridHeight / rows;
            
            // Horizontal grid lines
            for (int row = 1; row < rows; row++)
            {
                float y = gridStartY + row * vBarHeight;
                List<Vector2> hBar = new List<Vector2>
                {
                    new Vector2(centerX - gridWidth/2, y - lineWidth/2),
                    new Vector2(centerX - gridWidth/2, y + lineWidth/2),
                    new Vector2(centerX + gridWidth/2, y + lineWidth/2),
                    new Vector2(centerX + gridWidth/2, y - lineWidth/2),
                };
                
                var hBarSection = new CourseSection(hBar);
                hBarSection.Type = CourseSection.SectionType.Normal;
                hBarSection.Color = new SKColor(80, 180, 255); // Light blue 
                hBarSection.GlowIntensity = 0.6f;
                hBarSection.Bounciness = 0.5f;
                course.AddSection(hBarSection);
            }
            
            // Vertical grid lines
            for (int col = 1; col < cols; col++)
            {
                float x = centerX - gridWidth/2 + col * hBarWidth;
                List<Vector2> vBar = new List<Vector2>
                {
                    new Vector2(x - lineWidth/2, gridStartY),
                    new Vector2(x - lineWidth/2, gridStartY + gridHeight),
                    new Vector2(x + lineWidth/2, gridStartY + gridHeight),
                    new Vector2(x + lineWidth/2, gridStartY),
                };
                
                var vBarSection = new CourseSection(vBar);
                vBarSection.Type = CourseSection.SectionType.Normal;
                vBarSection.Color = new SKColor(80, 180, 255); // Light blue
                vBarSection.GlowIntensity = 0.6f;
                vBarSection.Bounciness = 0.5f;
                course.AddSection(vBarSection);
            }
        }

        private void CreateCurvySlalom(Course course)
        {
            // Curvy slalom inspired by wavy patterns in row 3, cols 2-3
            float sectionStartY = _height * 0.56f;
            float sectionHeight = _height * 0.12f;
            float centerX = _width * 0.5f;
            float curveWidth = _width * 0.35f;
            
            // Create series of smooth curves forming an S-shape
            List<Vector2> points = new List<Vector2>();
            int segments = 60;
            
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float x = centerX + curveWidth * (float)Math.Sin(t * Math.PI * 2);
                float y = sectionStartY + t * sectionHeight;
                points.Add(new Vector2(x, y));
            }
            
            // Create path with width by expanding in both directions along normals
            List<Vector2> leftEdge = new List<Vector2>();
            List<Vector2> rightEdge = new List<Vector2>();
            float pathWidth = 25f;
            
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 current = points[i];
                Vector2 next = points[i + 1];                Vector2 direction = next - current;
                direction.Normalize();
                Vector2 normal = new Vector2(-direction.Y, direction.X);
                
                leftEdge.Add(current + normal * pathWidth);
                rightEdge.Insert(0, current - normal * pathWidth);
            }
            
            // Add the final point
            Vector2 lastPoint = points[points.Count - 1];            Vector2 lastDirection = lastPoint - points[points.Count - 2];
            lastDirection.Normalize();
            Vector2 lastNormal = new Vector2(-lastDirection.Y, lastDirection.X);
            
            leftEdge.Add(lastPoint + lastNormal * pathWidth);
            rightEdge.Insert(0, lastPoint - lastNormal * pathWidth);
            
            List<Vector2> finalPath = new List<Vector2>();
            finalPath.AddRange(leftEdge);
            finalPath.AddRange(rightEdge);
            
            var slalomSection = new CourseSection(finalPath);
            slalomSection.Type = CourseSection.SectionType.Normal;
            slalomSection.Color = new SKColor(200, 100, 255); // Soft purple
            slalomSection.GlowIntensity = 0.5f;
            slalomSection.Friction = 0.06f;
            course.AddSection(slalomSection);
        }

        private void CreateCircularObstacles(Course course) 
        {
            // Circular obstacles inspired by row 4, col 2
            float sectionStartY = _height * 0.69f;
            float sectionHeight = _height * 0.08f;
            float centerX = _width * 0.5f;
            float sectionWidth = _width * 0.45f;
            
            // Base section
            List<Vector2> basePath = new List<Vector2> 
            {
                new Vector2(centerX - sectionWidth/2, sectionStartY),
                new Vector2(centerX - sectionWidth/2, sectionStartY + sectionHeight),
                new Vector2(centerX + sectionWidth/2, sectionStartY + sectionHeight),
                new Vector2(centerX + sectionWidth/2, sectionStartY),
            };
            
            var baseSection = new CourseSection(basePath);
            baseSection.Type = CourseSection.SectionType.Normal;
            baseSection.Color = new SKColor(50, 50, 60); // Dark gray
            baseSection.Friction = 0.08f;
            course.AddSection(baseSection);
            
            // Add one large circular obstacle in the center
            float mainRadius = Math.Min(sectionWidth, sectionHeight) * 0.3f;
            List<Vector2> mainCircle = new List<Vector2>();
            int mainPoints = 24;
            for (int i = 0; i < mainPoints; i++) {
                float angle = Core.MathHelper.TwoPi * i / mainPoints;
                mainCircle.Add(new Vector2(
                    centerX + mainRadius * (float)Math.Cos(angle),
                    sectionStartY + sectionHeight/2 + mainRadius * (float)Math.Sin(angle)
                ));
            }
            
            var mainCircleSection = new CourseSection(mainCircle);
            mainCircleSection.Type = CourseSection.SectionType.Bumpers;
            mainCircleSection.Color = new SKColor(40, 180, 40); // Green like in tile
            mainCircleSection.Bounciness = 0.85f;
            mainCircleSection.GlowIntensity = 0.7f;
            course.AddSection(mainCircleSection);
            
            // Add ring of smaller circles around the main one
            int numSmallCircles = 6;
            float smallRadius = mainRadius * 0.4f;
            float orbitRadius = mainRadius * 1.6f;
            
            for (int i = 0; i < numSmallCircles; i++) {
                float angle = Core.MathHelper.TwoPi * i / numSmallCircles;
                float x = centerX + orbitRadius * (float)Math.Cos(angle);
                float y = sectionStartY + sectionHeight/2 + orbitRadius * (float)Math.Sin(angle);
                
                List<Vector2> smallCircle = new List<Vector2>();
                int smallPoints = 12;
                for (int j = 0; j < smallPoints; j++) {
                    float circleAngle = Core.MathHelper.TwoPi * j / smallPoints;
                    smallCircle.Add(new Vector2(
                        x + smallRadius * (float)Math.Cos(circleAngle),
                        y + smallRadius * (float)Math.Sin(circleAngle)
                    ));
                }
                
                var smallCircleSection = new CourseSection(smallCircle);
                smallCircleSection.Type = CourseSection.SectionType.Bumpers;
                smallCircleSection.Color = new SKColor(40, 140, 40); // Slightly darker green
                smallCircleSection.Bounciness = 0.8f;
                smallCircleSection.GlowIntensity = 0.6f;
                course.AddSection(smallCircleSection);
            }
        }
        
        private void CreateStraightBoost(Course course)
        {
            // Straight boost section inspired by row 4, col 7
            float boostStartY = _height * 0.78f;
            float boostHeight = _height * 0.08f;
            float centerX = _width * 0.5f;
            float boostWidth = _width * 0.3f;
            
            // Main boost lane
            List<Vector2> boostPath = new List<Vector2> 
            {
                new Vector2(centerX - boostWidth/2, boostStartY),
                new Vector2(centerX - boostWidth/2, boostStartY + boostHeight),
                new Vector2(centerX + boostWidth/2, boostStartY + boostHeight),
                new Vector2(centerX + boostWidth/2, boostStartY),
            };
            
            var boostSection = new CourseSection(boostPath);
            boostSection.Type = CourseSection.SectionType.Booster;
            boostSection.Color = new SKColor(180, 230, 255); // Light blue like in the image
            boostSection.Friction = 0.01f;
            boostSection.GlowIntensity = 0.85f;
            course.AddSection(boostSection);
            
            // Add horizontal stripes as visual indicators of speed
            int numStripes = 6;
            float stripeHeight = boostHeight / (numStripes * 2 - 1); // Alternate stripe and gap
            
            for (int i = 0; i < numStripes; i++) {
                float y = boostStartY + i * stripeHeight * 2; // Skip a gap between each stripe
                
                List<Vector2> stripe = new List<Vector2> 
                {
                    new Vector2(centerX - boostWidth/2, y),
                    new Vector2(centerX - boostWidth/2, y + stripeHeight),
                    new Vector2(centerX + boostWidth/2, y + stripeHeight),
                    new Vector2(centerX + boostWidth/2, y),
                };
                
                var stripeSection = new CourseSection(stripe);
                stripeSection.Type = CourseSection.SectionType.Booster;
                stripeSection.Color = new SKColor(230, 250, 255); // Extra light blue/white
                stripeSection.Friction = 0.01f;
                stripeSection.GlowIntensity = 0.9f;
                course.AddSection(stripeSection);
            }
        }
        
        private void CreateFinishSegment(Course course) 
        {
            // Final path to finish
            float finishStartY = _height * 0.87f;
            float finishHeight = _height * 0.08f;
            float centerX = _width * 0.5f;
            float finishWidth = _width * 0.36f;
            
            // Simple straight path to finish
            List<Vector2> finishPath = new List<Vector2> 
            {
                new Vector2(centerX - finishWidth/2, finishStartY),
                new Vector2(centerX - finishWidth/2, finishStartY + finishHeight),
                new Vector2(centerX + finishWidth/2, finishStartY + finishHeight),
                new Vector2(centerX + finishWidth/2, finishStartY),
            };
            
            var finishSection = new CourseSection(finishPath);
            finishSection.Type = CourseSection.SectionType.Normal;
            finishSection.Color = new SKColor(80, 80, 100); // Dark blue-gray
            finishSection.Friction = 0.1f;
            course.AddSection(finishSection);
            
            // Add finish line
            float lineY = finishStartY + finishHeight * 0.75f;
            float lineHeight = 10f;
            
            List<Vector2> finishLine = new List<Vector2> 
            {
                new Vector2(centerX - finishWidth/2, lineY),
                new Vector2(centerX - finishWidth/2, lineY + lineHeight),
                new Vector2(centerX + finishWidth/2, lineY + lineHeight),
                new Vector2(centerX + finishWidth/2, lineY),
            };
            
            var finishLineSection = new CourseSection(finishLine);
            finishLineSection.Type = CourseSection.SectionType.Normal;
            finishLineSection.Color = SKColors.Gold;
            finishLineSection.GlowIntensity = 1.0f;
            course.AddSection(finishLineSection);
            
            // Add finish flags
            AddFinishFlags(course, centerX, lineY, finishWidth);
        }
        
        private void AddFinishFlags(Course course, float centerX, float finishY, float width) 
        {
            float flagPoleWidth = 5f;
            float flagPoleHeight = 30f;
            float flagWidth = 20f;
            float flagHeight = 15f;
            
            // Left flag
            List<Vector2> leftFlag = new List<Vector2> 
            {
                new Vector2(centerX - width/2 + flagPoleWidth, finishY),
                new Vector2(centerX - width/2 + flagPoleWidth, finishY - flagPoleHeight),
                new Vector2(centerX - width/2 + flagWidth + flagPoleWidth, finishY - flagPoleHeight + flagHeight/2),
            };
            
            var leftFlagSection = new CourseSection(leftFlag);
            leftFlagSection.Type = CourseSection.SectionType.Normal;
            leftFlagSection.Color = SKColors.Black;
            leftFlagSection.GlowIntensity = 0.3f;
            course.AddSection(leftFlagSection);
            
            // Right flag
            List<Vector2> rightFlag = new List<Vector2> 
            {
                new Vector2(centerX + width/2 - flagPoleWidth, finishY),
                new Vector2(centerX + width/2 - flagPoleWidth, finishY - flagPoleHeight),
                new Vector2(centerX + width/2 - flagWidth - flagPoleWidth, finishY - flagPoleHeight + flagHeight/2),
            };
            
            var rightFlagSection = new CourseSection(rightFlag);
            rightFlagSection.Type = CourseSection.SectionType.Normal;
            rightFlagSection.Color = SKColors.Black;
            rightFlagSection.GlowIntensity = 0.3f;
            course.AddSection(rightFlagSection);
        }
    }
}
