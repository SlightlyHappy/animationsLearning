using System;
using tainicom.Aether.Physics2D.Common;
using SkiaSharp;

namespace InfiniteMarbleRun.Core
{
    /// <summary>
    /// Math helper functions not covered by existing libraries
    /// </summary>
    public static class MathHelper
    {        // Common mathematical constants
        public const float Pi = (float)Math.PI;
        public const float TwoPi = (float)(Math.PI * 2.0);
        public const float HalfPi = (float)(Math.PI / 2.0);
        /// <summary>
        /// Linear interpolation between two values
        /// </summary>
        public static float Lerp(float a, float b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return a + (b - a) * t;
        }
        
        /// <summary>
        /// Linear interpolation between two Vector2 values
        /// </summary>
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return a + (b - a) * t;
        }
        
        /// <summary>
        /// Smooth step interpolation
        /// </summary>
        public static float SmoothStep(float a, float b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            t = t * t * (3f - 2f * t);
            return Lerp(a, b, t);
        }
        
        /// <summary>
        /// Generate a random color
        /// </summary>
        public static SKColor RandomColor(Random random)
        {
            return new SKColor(
                (byte)random.Next(256),
                (byte)random.Next(256),
                (byte)random.Next(256)
            );
        }
        
        /// <summary>
        /// Generate a bright color (good for visibility)
        /// </summary>
        public static SKColor RandomBrightColor(Random random)
        {
            return new SKColor(
                (byte)random.Next(128, 256),
                (byte)random.Next(128, 256),
                (byte)random.Next(128, 256)
            );
        }
        
        /// <summary>
        /// Returns the perpendicular vector (rotated 90 degrees CCW)
        /// </summary>
        public static Vector2 Perpendicular(Vector2 vector)
        {
            return new Vector2(-vector.Y, vector.X);
        }
      /// <summary>
        /// Reflect a vector about a normal vector
        /// </summary>
        public static Vector2 Reflect(Vector2 vector, Vector2 normal)
        {
            float dotProduct = Dot(vector, normal);
            return vector - 2f * dotProduct * normal;
        }
        
        /// <summary>
        /// Calculate the dot product of two vectors
        /// </summary>
        public static float Dot(Vector2 a, Vector2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }/// <summary>
        /// Rotate a vector by an angle (in radians)
        /// </summary>
        public static Vector2 Rotate(Vector2 vector, float angle)
        {
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);
            
            return new Vector2(
                vector.X * cos - vector.Y * sin,
                vector.X * sin + vector.Y * cos
            );
        }
        
        /// <summary>
        /// Check if a value is approximately equal to another value
        /// </summary>
        public static bool ApproximatelyEqual(float a, float b, float epsilon = 0.001f)
        {
            return Math.Abs(a - b) < epsilon;
        }
        
        /// <summary>
        /// Map a value from one range to another
        /// </summary>
        public static float Map(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }
        
        /// <summary>
        /// Get a random point inside a circle
        /// </summary>
        public static Vector2 RandomPointInCircle(Random random, Vector2 center, float radius)
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2);
            float distance = (float)(random.NextDouble() * radius);
            
            return new Vector2(
                center.X + distance * (float)Math.Cos(angle),
                center.Y + distance * (float)Math.Sin(angle)
            );
        }
        
        /// <summary>
        /// Get a random point on a circle
        /// </summary>
        public static Vector2 RandomPointOnCircle(Random random, Vector2 center, float radius)
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2);
            
            return new Vector2(
                center.X + radius * (float)Math.Cos(angle),
                center.Y + radius * (float)Math.Sin(angle)
            );
        }
        
        /// <summary>
        /// Find the nearest point on a line segment to a point
        /// </summary>
        public static Vector2 NearestPointOnLineSegment(Vector2 lineStart, Vector2 lineEnd, Vector2 point)
        {            Vector2 line = lineEnd - lineStart;
            float lineLengthSquared = line.LengthSquared();
            
            if (lineLengthSquared < 0.0001f)
                return lineStart;
                
            Vector2 pointMinusStart = point - lineStart;
            float t = (pointMinusStart.X * line.X + pointMinusStart.Y * line.Y) / lineLengthSquared;
            t = Math.Clamp(t, 0f, 1f);
            
            return lineStart + line * t;
        }
    }
}
