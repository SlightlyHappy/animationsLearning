using System;
using System.Collections.Generic;
using System.Linq;
using tainicom.Aether.Physics2D.Common;
using SkiaSharp;
using InfiniteMarbleRun.Marbles;

namespace InfiniteMarbleRun.Core
{
    /// <summary>
    /// Represents the procedurally generated course for the marble run
    /// </summary>
    public class Course
    {
        // Course sections and elements
        public List<CourseSection> Sections { get; private set; } = new List<CourseSection>();
        
        // Start and finish points
        public Vector2 StartPosition { get; set; }
        public Vector2 FinishPosition { get; set; }
        
        // Course boundaries
        public float Width { get; set; }
        public float Height { get; set; }
        
        // Checkpoint positions for progress tracking
        public List<Vector2> Checkpoints { get; private set; } = new List<Vector2>();
        
        // Course generation properties
        public int Complexity { get; set; }
        public int Seed { get; set; }
        
        // Course visual properties
        public SKColor PrimaryColor { get; set; }
        public SKColor SecondaryColor { get; set; }
        public SKColor AccentColor { get; set; }
        
        public Course(float width, float height, int complexity, int seed)
        {
            Width = width;
            Height = height;
            Complexity = complexity;
            Seed = seed;
            
            // Default start and finish (will be updated during generation)
            StartPosition = new Vector2(width * 0.5f, height * 0.1f);
            FinishPosition = new Vector2(width * 0.5f, height * 0.9f);
        }
        
        public void AddSection(CourseSection section)
        {
            Sections.Add(section);
        }
        
        public void AddCheckpoint(Vector2 position)
        {
            Checkpoints.Add(position);
        }
          public float GetProgressPercentage(Vector2 position)
        {
            // Find the closest checkpoint that's been passed
            // This is a simple implementation - would need more sophisticated
            // tracking for complex courses
            
            if (Checkpoints.Count == 0)
                return 0f;
                  // Calculate distances to all checkpoints
            var distances = Checkpoints.Select((cp, index) => 
                (Distance: (position - cp).Length(), Index: index)).ToList();
                
            // Sort by distance
            distances.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            
            // Progress is based on the index of the closest checkpoint
            int closestIndex = distances[0].Index;
            return (float)closestIndex / (Checkpoints.Count - 1);
        }
    }
    
    /// <summary>
    /// Represents a section of the marble run course
    /// </summary>
    public class CourseSection
    {
        // Section geometry
        public List<Vector2> Vertices { get; private set; } = new List<Vector2>();
        
        // Physical properties
        public float Friction { get; set; } = 0.1f;
        public float Bounciness { get; set; } = 0.3f;
        
        // Special features
        public SectionType Type { get; set; } = SectionType.Normal;
        
        // Visual properties
        public SKColor Color { get; set; } = SKColors.Gray;
        public float GlowIntensity { get; set; } = 0f;
        
        public CourseSection(List<Vector2> vertices)
        {
            Vertices = vertices;
        }
        
        public CourseSection(Vector2[] vertices)
        {
            Vertices = new List<Vector2>(vertices);
        }
        
        public enum SectionType
        {
            Normal,
            Ramp,
            Funnel,
            Spinner,
            Booster,
            SlowField,
            Bumpers
        }
    }
    
    /// <summary>
    /// Camera position and zoom information for dynamic views
    /// </summary>
    public class CameraState
    {
        public Vector2 Position { get; set; }
        public float Zoom { get; set; } = 1.0f;
        public float Rotation { get; set; } = 0.0f;
        
        public CameraState(Vector2 position, float zoom = 1.0f, float rotation = 0.0f)
        {
            Position = position;
            Zoom = zoom;
            Rotation = rotation;
        }
    }
    
    /// <summary>
    /// Controls camera movement, zoom and focus during the marble run
    /// </summary>
    public class DynamicCameraController
    {
        private List<Marble> _marbles;
        private Course _course;
        private int _width;
        private int _height;
        private Random _random;
        
        // Camera state
        private CameraState _currentState;
        private CameraState _targetState;
        private float _transitionProgress = 0;
        private float _transitionDuration = 1.0f;
        
        // Special camera events
        private int _nextSpecialEventFrame;
        private int _specialEventDuration;
        private bool _inSpecialEvent = false;
        
        public DynamicCameraController(List<Marble> marbles, Course course, int width, int height)
        {
            _marbles = marbles;
            _course = course;
            _width = width;
            _height = height;
            _random = new Random();
            
            // Initial camera state - overview of start area
            _currentState = new CameraState(
                new Vector2(width * 0.5f, course.StartPosition.Y + height * 0.2f),
                0.8f);
            _targetState = _currentState;
            
            // Schedule first special event
            ScheduleNextSpecialEvent(0, 60 * 5); // Within first 5 seconds
        }
        
        public void Update(int frame, int totalFrames)
        {
            // Update camera transition
            if (_transitionProgress < 1.0f)
            {
                _transitionProgress += 1.0f / (_transitionDuration * 60); // Assuming 60fps
                _transitionProgress = Math.Min(_transitionProgress, 1.0f);
                  // Interpolate camera position
                float t = SmoothStep(_transitionProgress);
                _currentState.Position = MathHelper.Lerp(_currentState.Position, _targetState.Position, t);
                _currentState.Zoom = MathHelper.Lerp(_currentState.Zoom, _targetState.Zoom, t);
                _currentState.Rotation = MathHelper.Lerp(_currentState.Rotation, _targetState.Rotation, t);
            }
            
            // Check for special camera events
            if (!_inSpecialEvent && frame >= _nextSpecialEventFrame)
            {
                StartSpecialCameraEvent(frame);
            }
            else if (_inSpecialEvent && frame >= _nextSpecialEventFrame + _specialEventDuration)
            {
                EndSpecialCameraEvent(frame);
                ScheduleNextSpecialEvent(frame, totalFrames);
            }
            
            // Default camera behavior when not in special event
            if (!_inSpecialEvent && _transitionProgress >= 1.0f)
            {
                UpdateDefaultCamera();
            }
        }
        
        private void UpdateDefaultCamera()
        {
            // Find the leader marble
            Marble leader = GetLeaderMarble();
            if (leader == null) return;
            
            // Set target to follow the leader
            var newTarget = new CameraState(
                new Vector2(leader.Position.X, leader.Position.Y + _height * 0.1f),
                1.0f);
                
            SetCameraTarget(newTarget, 1.0f);
        }
        
        private Marble GetLeaderMarble()
        {
            if (_marbles.Count == 0) return null;
            
            // Find marble with highest progress
            return _marbles.OrderByDescending(m => _course.GetProgressPercentage(m.Position)).First();
        }
        
        private void StartSpecialCameraEvent(int currentFrame)
        {
            _inSpecialEvent = true;
            
            // Choose random special event type
            int eventType = _random.Next(4);
            
            switch(eventType)
            {
                case 0: // Zoom in on random marble
                    ZoomInOnRandomMarble();
                    break;
                case 1: // Overview shot
                    ShowOverview();
                    break;
                case 2: // Focus on close race
                    FocusOnCloseRace();
                    break;
                case 3: // Dramatic angle for upcoming obstacle
                    ShowDramaticAngle();
                    break;
            }
        }
        
        private void ZoomInOnRandomMarble()
        {
            // Pick a random marble
            if (_marbles.Count == 0) return;
            Marble target = _marbles[_random.Next(_marbles.Count)];
            
            // Create dramatic zoom in
            var newTarget = new CameraState(
                target.Position,
                1.8f, // Zoomed in
                (float)(_random.NextDouble() * 10 - 5) // Slight tilt
            );
            
            SetCameraTarget(newTarget, 0.5f);
            _specialEventDuration = 60; // 1 second
        }
        
        private void ShowOverview()
        {
            // Get average position of all marbles
            if (_marbles.Count == 0) return;
            
            Vector2 avgPos = Vector2.Zero;
            foreach (var marble in _marbles)
            {
                avgPos += marble.Position;
            }
            avgPos /= _marbles.Count;
            
            // Set zoomed out view
            var newTarget = new CameraState(
                avgPos,
                0.5f, // Zoomed out
                0f // No rotation
            );
            
            SetCameraTarget(newTarget, 1.0f);
            _specialEventDuration = 120; // 2 seconds
        }
        
        private void FocusOnCloseRace()
        {
            // Find two marbles that are close to each other
            if (_marbles.Count < 2) return;
            
            // Sort by progress
            var sortedMarbles = _marbles.OrderByDescending(m => 
                _course.GetProgressPercentage(m.Position)).ToList();
                
            // Look at the top two
            Vector2 avgPos = (sortedMarbles[0].Position + sortedMarbles[1].Position) * 0.5f;
            
            var newTarget = new CameraState(
                avgPos,
                1.3f, // Slightly zoomed in
                0f // No rotation
            );
            
            SetCameraTarget(newTarget, 0.7f);
            _specialEventDuration = 180; // 3 seconds
        }
        
        private void ShowDramaticAngle()
        {
            // Find a dramatic point ahead of the leader
            Marble leader = GetLeaderMarble();
            if (leader == null) return;
            
            // Try to find a checkpoint ahead
            float progress = _course.GetProgressPercentage(leader.Position);
            int nextCheckpointIndex = (int)(progress * _course.Checkpoints.Count) + 1;
            
            if (nextCheckpointIndex < _course.Checkpoints.Count)
            {
                Vector2 targetPos = _course.Checkpoints[nextCheckpointIndex];
                
                var newTarget = new CameraState(
                    targetPos,
                    1.2f,
                    (float)(_random.NextDouble() * 20 - 10) // More dramatic tilt
                );
                
                SetCameraTarget(newTarget, 0.8f);
                _specialEventDuration = 120; // 2 seconds
            }
            else
            {
                // Fallback to overview
                ShowOverview();
            }
        }
        
        private void EndSpecialCameraEvent(int currentFrame)
        {
            _inSpecialEvent = false;
            
            // Return to default camera behavior
            UpdateDefaultCamera();
        }
        
        private void ScheduleNextSpecialEvent(int currentFrame, int totalFrames)
        {
            // Schedule next event in 3-10 seconds
            int secondsToNext = _random.Next(3, 10);
            _nextSpecialEventFrame = currentFrame + (secondsToNext * 60);
            
            // Make sure we don't schedule beyond the end
            if (_nextSpecialEventFrame >= totalFrames - 300) // 5 seconds before end
            {
                _nextSpecialEventFrame = totalFrames - 300;
            }
        }
        
        private void SetCameraTarget(CameraState target, float duration)
        {
            _targetState = target;
            _transitionProgress = 0f;
            _transitionDuration = duration;
        }
        
        public CameraState GetCameraState()
        {
            return _currentState;
        }
        
        // Smooth step function for camera interpolation
        private float SmoothStep(float x)
        {
            return x * x * (3 - 2 * x);
        }
    }
}
