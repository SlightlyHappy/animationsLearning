using System;
using tainicom.Aether.Physics2D.Common;
using SkiaSharp;

namespace InfiniteMarbleRun.Rendering
{
    /// <summary>
    /// Represents a visual particle effect for rendering
    /// </summary>
    public class ParticleEffect
    {
        // Particle properties
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public SKColor Color { get; set; }
        public float Size { get; set; }
        public float LifeTime { get; set; }
        public float Age { get; set; }
        public bool IsAlive => Age < LifeTime;
        public EffectType Type { get; private set; }
        
        /// <summary>
        /// Types of particle effects
        /// </summary>
        public enum EffectType
        {
            Collision,
            Trail,
            Spark,
            Finish
        }
        
        public ParticleEffect(Vector2 position, Vector2 velocity, SKColor color, float size, EffectType type)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Size = size;
            Type = type;
            LifeTime = 1.0f; // Default lifespan
            Age = 0f;
        }
        
        /// <summary>
        /// Update particle position and properties
        /// </summary>
        public void Update(float deltaTime)
        {
            // Update position based on velocity
            Position += Velocity * deltaTime;
            
            // Apply physics effects based on type
            switch (Type)
            {
                case EffectType.Collision:
                    // Slow down collision particles
                    Velocity *= 0.95f;
                    break;
                    
                case EffectType.Trail:
                    // Trail particles fade and slow
                    Velocity *= 0.9f;
                    break;
                    
                case EffectType.Spark:
                    // Sparks move quickly but fade fast
                    Velocity *= 0.8f;
                    break;
                    
                case EffectType.Finish:
                    // Finish particles rise with random motion
                    Velocity += new Vector2(
                        (float)(new Random().NextDouble() * 2 - 1) * 10, 
                        -50) * deltaTime;
                    break;
            }
            
            // Age the particle
            Age += deltaTime;
        }
        
        /// <summary>
        /// Get the current alpha value based on lifetime
        /// </summary>
        public byte GetAlpha()
        {
            float lifePercent = Age / LifeTime;
            
            // Different fade patterns based on effect type
            switch (Type)
            {
                case EffectType.Trail:
                    // Quick fade out
                    return (byte)(Color.Alpha * (1 - lifePercent * lifePercent));
                    
                case EffectType.Spark:
                    // Sparks fade quickly at the end
                    return (byte)(Color.Alpha * (1 - Math.Pow(lifePercent, 0.5)));
                    
                case EffectType.Finish:
                    // Finish particles pulse
                    float pulse = (float)Math.Sin(lifePercent * Math.PI * 6) * 0.3f + 0.7f;
                    return (byte)(Color.Alpha * (1 - lifePercent) * pulse);
                    
                case EffectType.Collision:
                default:
                    // Linear fade out
                    return (byte)(Color.Alpha * (1 - lifePercent));
            }
        }
        
        /// <summary>
        /// Get the current size based on lifetime
        /// </summary>
        public float GetCurrentSize()
        {
            float lifePercent = Age / LifeTime;
            
            switch (Type)
            {
                case EffectType.Collision:
                    // Collision particles expand then contract
                    if (lifePercent < 0.3f)
                        return Size * (1 + lifePercent);
                    else
                        return Size * (1 + 0.3f - (lifePercent - 0.3f) * 1.3f);
                    
                case EffectType.Trail:
                    // Trails shrink as they age
                    return Size * (1 - lifePercent * 0.7f);
                    
                case EffectType.Spark:
                    // Sparks start small, expand quickly, then contract
                    if (lifePercent < 0.2f)
                        return Size * lifePercent * 5;
                    else
                        return Size * (1 - (lifePercent - 0.2f) * 1.25f);
                    
                case EffectType.Finish:
                    // Finish particles vary in size
                    return Size * (1 + (float)Math.Sin(lifePercent * Math.PI * 3) * 0.3f);
                    
                default:
                    return Size;
            }
        }
    }
}
