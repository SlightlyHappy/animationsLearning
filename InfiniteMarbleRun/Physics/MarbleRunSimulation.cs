using System;
using System.Collections.Generic;
using System.Linq;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Dynamics.Contacts;
using tainicom.Aether.Physics2D.Common;
using InfiniteMarbleRun.Core;
using InfiniteMarbleRun.Marbles;
using InfiniteMarbleRun.Rendering;
using SkiaSharp;

namespace InfiniteMarbleRun.Physics
{
    /// <summary>
    /// Handles the physics simulation for the marble run
    /// </summary>
    public class MarbleRunSimulation
    {
        // Physics world
        private World _world;
        
        // Course reference
        private Course _course;
        
        // List of active marbles with their physics bodies
        private List<(Marble marble, Body body)> _marbles = new List<(Marble, Body)>();
        
        // Collision detection
        private Dictionary<(Body bodyA, Body bodyB), bool> _collisions = new Dictionary<(Body, Body), bool>();
        
        // Dimensions
        private int _width;
        private int _height;
        
        // Physics constants
        private const float TimeStep = 1.0f / 60.0f;
        private const int VelocityIterations = 8;
        private const int PositionIterations = 3;
        private const float Gravity = 9.8f;
        private const float PixelToMetersRatio = 100.0f; // 100 pixels = 1 meter
        
        // Race state
        private bool _raceFinished = false;
        private float _raceTime = 0f;
        private List<(Marble marble, float finishTime)> _finishers = new List<(Marble, float)>();
        
        public MarbleRunSimulation(Course course, int width, int height)
        {
            _course = course;
            _width = width;
            _height = height;
            
            // Create physics world with gravity
            _world = new World(new Vector2(0, Gravity));
              // Setup collision event handling
            _world.ContactManager.BeginContact = OnBeginContact;
            // Using a lambda that doesn't return a value for EndContact
            _world.ContactManager.EndContact = (contact) => HandleEndContact(contact);
            
            // Create the course boundaries in the physics world
            CreateCourseBoundaries();
        }
        
        /// <summary>
        /// Add a marble to the simulation
        /// </summary>
        public void AddMarble(Marble marble)
        {
            // Convert from screen coordinates to physics world
            Vector2 physicsPosition = marble.Position / PixelToMetersRatio;
            
            // Create a physics body for the marble
            var body = _world.CreateBody(physicsPosition);
            body.BodyType = BodyType.Dynamic;
            
            // Create a circle fixture for the marble
            var circle = new tainicom.Aether.Physics2D.Collision.Shapes.CircleShape(
                marble.Radius / PixelToMetersRatio, // radius in meters
                marble.Mass
            );
            
            var fixture = body.CreateFixture(circle);
            fixture.Friction = marble.Friction;
            fixture.Restitution = marble.Restitution;
            
            // Store the association between marble and physics body
            _marbles.Add((marble, body));
            
            // Store a reference to the marble in the body's user data for collision detection
            body.Tag = marble;
        }
        
        /// <summary>
        /// Step the physics simulation forward
        /// </summary>
        public void Step(float deltaTime)
        {
            // Step the physics world
            _world.Step(deltaTime);
            
            // Update race time
            _raceTime += deltaTime;
            
            // Update marble positions from physics bodies
            foreach (var (marble, body) in _marbles)
            {
                // Convert from physics world to screen coordinates
                marble.Position = body.Position * PixelToMetersRatio;
                marble.Velocity = body.LinearVelocity * PixelToMetersRatio;
                
                // Track top speed
                float currentSpeed = marble.Velocity.Length();
                if (currentSpeed > marble.TopSpeed)
                {
                    marble.TopSpeed = currentSpeed;
                }
                
                // Track distance traveled (approximation)
                marble.DistanceTraveled += currentSpeed * deltaTime;
                
                // Update the marble's own logic
                marble.Update(deltaTime, Gravity);
                
                // Check if marble has reached the finish line
                if ((marble.Position - _course.FinishPosition).Length() < 50f && 
                    marble.FinishTime < 0)
                {
                    marble.FinishTime = _raceTime;
                    _finishers.Add((marble, _raceTime));
                }
                
                // Apply special section effects
                ApplySectionEffects(marble, body);
            }
            
            // Check if race is finished (all marbles have finished or fallen off)
            if (!_raceFinished && _finishers.Count == _marbles.Count)
            {
                _raceFinished = true;
                FinalizeRankings();
            }
        }
        
        private void ApplySectionEffects(Marble marble, Body body)
        {
            // Find which course section the marble is in
            foreach (var section in _course.Sections)
            {
                if (IsPointInPolygon(marble.Position, section.Vertices))
                {
                    // Apply special effects based on section type
                    switch (section.Type)
                    {
                        case CourseSection.SectionType.Booster:
                            // Apply a boost in the direction of the section
                            Vector2 direction = section.Vertices[2] - section.Vertices[0];
                            direction.Normalize();
                            body.ApplyLinearImpulse(direction * 0.05f);
                            
                            // Create particle effect for boosting
                            CreateTrailEffect(marble);
                            break;
                            
                        case CourseSection.SectionType.Spinner:
                            // Apply angular velocity
                            body.ApplyTorque(0.01f);
                            break;
                            
                        case CourseSection.SectionType.SlowField:
                            // Apply damping
                            body.LinearVelocity *= 0.98f;
                            break;
                            
                        case CourseSection.SectionType.Bumpers:
                            // Random small impulses
                            if (new Random().NextDouble() < 0.05)
                            {
                                Vector2 randomDir = new Vector2(
                                    (float)(new Random().NextDouble() * 2 - 1),
                                    (float)(new Random().NextDouble() * 2 - 1)
                                );
                                randomDir.Normalize();
                                body.ApplyLinearImpulse(randomDir * 0.03f);
                                
                                // Create spark particle effect
                                CreateSparkEffect(marble);
                            }
                            break;
                    }
                    
                    break; // Only apply effects from one section
                }
            }
        }
        
        /// <summary>
        /// Gets a list of current rankings based on progress
        /// </summary>
        public List<(Marble marble, int rank, float progress)> GetRankings()
        {
            var rankings = new List<(Marble marble, int rank, float progress)>();
            
            // Combine finished marbles and active marbles
            foreach (var (marble, body) in _marbles)
            {
                float progress;
                
                if (marble.FinishTime >= 0)
                {
                    progress = 1.0f; // Marble has finished
                }
                else
                {
                    // Calculate progress based on checkpoints
                    progress = _course.GetProgressPercentage(marble.Position);
                }
                
                rankings.Add((marble, 0, progress));
            }
            
            // Sort by progress (descending)
            rankings.Sort((a, b) => b.progress.CompareTo(a.progress));
            
            // Assign ranks
            for (int i = 0; i < rankings.Count; i++)
            {
                var (marble, _, progress) = rankings[i];
                rankings[i] = (marble, i + 1, progress);
                marble.Rank = i + 1;
            }
            
            return rankings;
        }
        
        /// <summary>
        /// Finalize the rankings at the end of the race
        /// </summary>
        private void FinalizeRankings()
        {
            // Sort finishers by finish time
            _finishers.Sort((a, b) => a.finishTime.CompareTo(b.finishTime));
            
            // Assign final ranks
            for (int i = 0; i < _finishers.Count; i++)
            {
                _finishers[i].marble.Rank = i + 1;
            }
        }
        
        /// <summary>
        /// Create the physics bodies for the course boundaries
        /// </summary>
        private void CreateCourseBoundaries()
        {
            foreach (var section in _course.Sections)
            {
                // Create a static body for this section
                var body = _world.CreateBody();
                body.BodyType = BodyType.Static;
                
                // Convert vertices to physics coordinates
                var vertices = new Vertices(section.Vertices.Count);
                foreach (var vertex in section.Vertices)
                {
                    vertices.Add(vertex / PixelToMetersRatio);
                }
                
                // Create a polygon shape and fixture
                var polygon = new tainicom.Aether.Physics2D.Collision.Shapes.PolygonShape(vertices, 1);
                var fixture = body.CreateFixture(polygon);
                
                // Set physical properties
                fixture.Friction = section.Friction;
                fixture.Restitution = section.Bounciness;
                
                // Store a reference to the section for collision effects
                body.Tag = section;
            }
        }
          
        /// <summary>
        /// Collision detection callback
        /// </summary>
        private bool OnBeginContact(Contact contact)
        {
            var bodyA = contact.FixtureA.Body;
            var bodyB = contact.FixtureB.Body;
            
            // Check if one body is a marble
            if (bodyA.Tag is Marble marbleA && bodyB.Tag is not Marble)
            {
                HandleCollision(marbleA, bodyB);
                _collisions[(bodyA, bodyB)] = true;
            }
            else if (bodyB.Tag is Marble marbleB && bodyA.Tag is not Marble)
            {
                HandleCollision(marbleB, bodyA);
                _collisions[(bodyB, bodyA)] = true;
            }
            else if (bodyA.Tag is Marble ma && bodyB.Tag is Marble mb)
            {
                // Marble-to-marble collision
                HandleMarbleCollision(ma, mb);
                _collisions[(bodyA, bodyB)] = true;
            }
            
            return true;
        }
        
        /// <summary>
        /// Collision end callback
        /// </summary>
        private void HandleEndContact(Contact contact)
        {
            var bodyA = contact.FixtureA.Body;
            var bodyB = contact.FixtureB.Body;
            
            // Remove from active collisions
            if (_collisions.ContainsKey((bodyA, bodyB)))
                _collisions.Remove((bodyA, bodyB));
                
            if (_collisions.ContainsKey((bodyB, bodyA)))
                _collisions.Remove((bodyB, bodyA));
        }
        
        /// <summary>
        /// Handle collision between a marble and another object
        /// </summary>
        private void HandleCollision(Marble marble, Body other)
        {
            // Increment collision counter
            marble.Collisions++;
            
            // Get collision velocity
            float speed = marble.Velocity.Length();
            
            // Create particle effects based on collision speed
            if (speed > 300)
            {
                // Hard collision
                CreateCollisionEffect(marble, 10);
            }
            else if (speed > 150)
            {
                // Medium collision
                CreateCollisionEffect(marble, 5);
            }
            
            // Special effects for section types
            if (other.Tag is CourseSection section)
            {
                switch (section.Type)
                {
                    case CourseSection.SectionType.Booster:
                        CreateTrailEffect(marble);
                        break;
                        
                    case CourseSection.SectionType.Bumpers:
                        CreateSparkEffect(marble);
                        break;
                }
            }
        }
        
        /// <summary>
        /// Handle collision between two marbles
        /// </summary>
        private void HandleMarbleCollision(Marble marbleA, Marble marbleB)
        {
            marbleA.Collisions++;
            marbleB.Collisions++;
            
            // Calculate relative velocity
            float relativeSpeed = (marbleA.Velocity - marbleB.Velocity).Length();
            
            // Create particle effects for significant collisions
            if (relativeSpeed > 200)
            {
                CreateCollisionEffect(marbleA, 7);
                CreateCollisionEffect(marbleB, 7);
            }
        }
        
        /// <summary>
        /// Create a collision particle effect
        /// </summary>
        private void CreateCollisionEffect(Marble marble, int particleCount)
        {
            Random random = new Random();
            
            for (int i = 0; i < particleCount; i++)
            {
                // Create random velocity away from the center
                Vector2 direction = new Vector2(
                    (float)(random.NextDouble() * 2 - 1),
                    (float)(random.NextDouble() * 2 - 1)
                );
                direction.Normalize();
                
                Vector2 velocity = direction * ((float)random.NextDouble() * 100 + 50);                // Create the particle
                var particle = new Marbles.ParticleEffect(
                    marble.Position,
                    velocity,
                    marble.PrimaryColor.WithAlpha(150),
                    (float)random.NextDouble() * 5 + 2,
                    Marbles.ParticleEffect.EffectType.Collision
                );
                
                particle.LifeTime = 0.5f;
                marble.AddParticleEffect(particle);
            }
        }
        
        /// <summary>
        /// Create a trail effect behind the marble
        /// </summary>
        private void CreateTrailEffect(Marble marble)
        {
            Random random = new Random();
            Vector2 direction = -marble.Velocity;
            direction.Normalize();
            
            for (int i = 0; i < 3; i++)
            {
                // Create particles along the trail
                Vector2 offset = direction * (marble.Radius * 0.5f + i * 5);
                
                // Add some randomness to the position
                offset += new Vector2(
                    (float)(random.NextDouble() * 10 - 5),
                    (float)(random.NextDouble() * 10 - 5)
                );
                
                Vector2 particlePos = marble.Position + offset;                // Create the particle with a velocity opposite to the marble's
                var particle = new Marbles.ParticleEffect(
                    particlePos,
                    direction * 20,
                    marble.PrimaryColor.WithAlpha(100),
                    marble.Radius * 0.5f * (float)random.NextDouble() + 2,
                    Marbles.ParticleEffect.EffectType.Trail
                );
                
                particle.LifeTime = 0.3f;
                marble.AddParticleEffect(particle);
            }
        }
        
        /// <summary>
        /// Create a spark effect
        /// </summary>
        private void CreateSparkEffect(Marble marble)
        {
            Random random = new Random();
            
            for (int i = 0; i < 5; i++)
            {
                // Create random direction
                Vector2 direction = new Vector2(
                    (float)(random.NextDouble() * 2 - 1),
                    (float)(random.NextDouble() * 2 - 1)
                );
                direction.Normalize();
                
                // Fast but short-lived particles
                Vector2 velocity = direction * ((float)random.NextDouble() * 300 + 100);
                
                // Bright color
                SKColor sparkColor = new SKColor(
                    (byte)random.Next(200, 255),
                    (byte)random.Next(200, 255),
                    (byte)random.Next(100, 200)
                );                // Create the particle
                var particle = new Marbles.ParticleEffect(
                    marble.Position,
                    velocity,
                    sparkColor,
                    (float)random.NextDouble() * 3 + 1,
                    Marbles.ParticleEffect.EffectType.Spark
                );
                
                particle.LifeTime = 0.2f;
                marble.AddParticleEffect(particle);
            }
        }
        
        /// <summary>
        /// Check if a point is inside a polygon
        /// </summary>
        private bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
        {
            bool inside = false;
            
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                if (((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y)) &&
                    (point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / 
                    (polygon[j].Y - polygon[i].Y) + polygon[i].X))
                {
                    inside = !inside;
                }
            }
            
            return inside;
        }
    }
}
