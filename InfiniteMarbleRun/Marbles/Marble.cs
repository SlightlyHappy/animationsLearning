using System;
using System.Collections.Generic;
using SkiaSharp;
using tainicom.Aether.Physics2D.Common;
using InfiniteMarbleRun.Core;

namespace InfiniteMarbleRun.Marbles
{
    /// <summary>
    /// Represents a marble with unique physical properties and appearance
    /// </summary>
    public class Marble
    {
        // Identity
        public int Id { get; }
        public string Name { get; set; }
        
        // Physical properties
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Radius { get; set; }
        public float Mass { get; set; }
        public float Friction { get; set; }
        public float Restitution { get; set; } // Bounciness
        
        // Appearance
        public SKColor PrimaryColor { get; set; }
        public SKColor SecondaryColor { get; set; }
        public MarbleType Type { get; set; }
        public TexturePattern Pattern { get; set; }
        
        // Race statistics
        public float DistanceTraveled { get; set; }
        public float TopSpeed { get; set; }
        public int Collisions { get; set; }
        public int Rank { get; set; }
        public float FinishTime { get; set; } = -1; // -1 means not finished
        
        // Special effects
        public List<ParticleEffect> ParticleEffects { get; } = new List<ParticleEffect>();
        
        public Marble(int id, string name, Vector2 position, float radius, float mass, MarbleType type)
        {
            Id = id;
            Name = name;
            Position = position;
            Velocity = Vector2.Zero;
            Radius = radius;
            Mass = mass;
            Type = type;
            
            // Set default properties based on marble type
            SetPropertiesFromType(type);
        }
        
        private void SetPropertiesFromType(MarbleType type)
        {
            switch (type)
            {
                case MarbleType.Standard:
                    Friction = 0.3f;
                    Restitution = 0.5f;
                    PrimaryColor = new SKColor(200, 200, 200);
                    SecondaryColor = new SKColor(150, 150, 150);
                    Pattern = TexturePattern.Swirl;
                    break;
                    
                case MarbleType.Glass:
                    Friction = 0.1f;
                    Restitution = 0.8f;
                    PrimaryColor = new SKColor(220, 240, 255, 150);
                    SecondaryColor = new SKColor(200, 230, 255, 180);
                    Pattern = TexturePattern.Clear;
                    break;
                    
                case MarbleType.Steel:
                    Friction = 0.2f;
                    Restitution = 0.6f;
                    PrimaryColor = new SKColor(180, 180, 190);
                    SecondaryColor = new SKColor(210, 210, 220);
                    Pattern = TexturePattern.Metallic;
                    Mass *= 1.5f;
                    break;
                    
                case MarbleType.Rubber:
                    Friction = 0.8f;
                    Restitution = 0.9f;
                    PrimaryColor = new SKColor(255, 50, 50);
                    SecondaryColor = new SKColor(200, 40, 40);
                    Pattern = TexturePattern.Solid;
                    Mass *= 0.8f;
                    break;
                    
                case MarbleType.Wood:
                    Friction = 0.7f;
                    Restitution = 0.3f;
                    PrimaryColor = new SKColor(160, 120, 60);
                    SecondaryColor = new SKColor(140, 100, 40);
                    Pattern = TexturePattern.Grain;
                    Mass *= 0.7f;
                    break;
                    
                case MarbleType.Ice:
                    Friction = 0.05f;
                    Restitution = 0.4f;
                    PrimaryColor = new SKColor(200, 240, 255);
                    SecondaryColor = new SKColor(180, 220, 255);
                    Pattern = TexturePattern.Faceted;
                    break;
                    
                case MarbleType.Lead:
                    Friction = 0.4f;
                    Restitution = 0.2f;
                    PrimaryColor = new SKColor(80, 80, 90);
                    SecondaryColor = new SKColor(60, 60, 70);
                    Pattern = TexturePattern.Solid;
                    Mass *= 2.0f;
                    break;
                    
                case MarbleType.Gold:
                    Friction = 0.3f;
                    Restitution = 0.4f;
                    PrimaryColor = new SKColor(255, 215, 0);
                    SecondaryColor = new SKColor(230, 190, 0);
                    Pattern = TexturePattern.Metallic;
                    Mass *= 1.8f;
                    break;
                    
                case MarbleType.Cosmic:
                    Friction = 0.2f;
                    Restitution = 0.7f;
                    PrimaryColor = new SKColor(100, 0, 200);
                    SecondaryColor = new SKColor(200, 0, 255);
                    Pattern = TexturePattern.Galaxy;
                    break;
                    
                case MarbleType.Neon:
                    Friction = 0.25f;
                    Restitution = 0.6f;
                    PrimaryColor = new SKColor(0, 255, 100);
                    SecondaryColor = new SKColor(0, 200, 80);
                    Pattern = TexturePattern.Glow;
                    break;
            }
        }
        
        public void AddParticleEffect(ParticleEffect effect)
        {
            ParticleEffects.Add(effect);
        }
        
        public void ClearParticleEffects()
        {
            ParticleEffects.Clear();
        }
        
        public void Update(float deltaTime, float gravity)
        {
            // Update particle effects
            for (int i = ParticleEffects.Count - 1; i >= 0; i--)
            {
                ParticleEffects[i].Update(deltaTime);
                if (ParticleEffects[i].IsExpired)
                {
                    ParticleEffects.RemoveAt(i);
                }
            }
        }
    }
    
    public enum MarbleType
    {
        Standard,
        Glass,
        Steel,
        Rubber,
        Wood,
        Ice,
        Lead,
        Gold,
        Cosmic,
        Neon
    }
    
    public enum TexturePattern
    {
        Solid,
        Swirl,
        Clear,
        Metallic,
        Grain,
        Faceted,
        Galaxy,
        Glow
    }
    
    /// <summary>
    /// Represents a visual particle effect for marbles
    /// </summary>
    public class ParticleEffect
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float LifeTime { get; set; } = 1.0f;
        public float CurrentLife { get; private set; } = 0f;
        public SKColor Color { get; set; }
        public float Size { get; set; }
        public EffectType Type { get; set; }
        
        public bool IsExpired => CurrentLife >= LifeTime;
        
        public ParticleEffect(Vector2 position, Vector2 velocity, SKColor color, float size, EffectType type)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Size = size;
            Type = type;
        }
        
        public void Update(float deltaTime)
        {
            Position += Velocity * deltaTime;
            CurrentLife += deltaTime;
            
            // Additional effect type specific updates
            switch (Type)
            {
                case EffectType.Trail:
                    Velocity *= 0.95f; // Slow down
                    break;
                case EffectType.Collision:
                    Size *= 0.9f; // Shrink
                    break;
                case EffectType.Spark:
                    Velocity *= 0.8f; // Rapid slow down
                    Size *= 0.85f; // Shrink faster
                    break;
            }
        }
        
        public enum EffectType
        {
            Trail,
            Collision,
            Spark,
            Smoke
        }
    }
    
    /// <summary>
    /// Factory for creating marbles with different properties
    /// </summary>
    public class MarbleFactory
    {
        private Random _random;
        private string[] _marbleNames = new string[] 
        {
            "Ruby", "Sapphire", "Emerald", "Topaz", "Onyx",
            "Blaze", "Frost", "Thunder", "Shadow", "Flash",
            "Comet", "Meteor", "Star", "Galaxy", "Orbit",
            "Bolt", "Dash", "Swift", "Rush", "Zoom",
            "Bouncer", "Speedy", "Rolly", "Spinner", "Racer"
        };
        
        public MarbleFactory(Random random)
        {
            _random = random;
        }
        
        public Marble CreateRandomMarble(int id, Vector2 position)
        {
            // Choose random marble type
            MarbleType type = (MarbleType)_random.Next(Enum.GetValues(typeof(MarbleType)).Length);
            
            // Choose random name
            string name = _marbleNames[_random.Next(_marbleNames.Length)];
            
            // Base radius and mass
            float radius = 20f + (float)_random.NextDouble() * 10f;
            float mass = radius * radius * 0.01f;
            
            return new Marble(id, name, position, radius, mass, type);
        }
        
        public Marble CreateSpecificMarble(int id, Vector2 position, MarbleType type)
        {
            // Choose name based on type
            string name = $"{type} {_marbleNames[_random.Next(_marbleNames.Length)]}";
            
            // Base radius and mass
            float radius = 25f; 
            float mass = radius * radius * 0.01f;
            
            return new Marble(id, name, position, radius, mass, type);
        }
    }
}
