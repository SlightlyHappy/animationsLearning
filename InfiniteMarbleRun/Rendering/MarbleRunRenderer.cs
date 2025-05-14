using System;
using System.Collections.Generic;
using System.IO;
using SkiaSharp;
using tainicom.Aether.Physics2D.Common;
using InfiniteMarbleRun.Core;
using InfiniteMarbleRun.Marbles;

namespace InfiniteMarbleRun.Rendering
{
    /// <summary>
    /// Handles rendering the marble run animation
    /// </summary>
    public class MarbleRunRenderer : IDisposable
    {
        // Canvas dimensions
        private readonly int _width;
        private readonly int _height;
        
        // References to simulation objects
        private readonly Course _course;
        private readonly List<Marble> _marbles;
        
        // Rendering resources
        private SKBitmap _bitmap;
        private SKTypeface _typeface;
        
        // Visual effects
        private bool _useBlur = true;
        private bool _useGlow = true;
        
        // Background
        private SKColor _backgroundColor = new SKColor(20, 20, 40);
        private SKColor _backgroundAccent = new SKColor(40, 20, 60);
        
        public MarbleRunRenderer(int width, int height, Course course, List<Marble> marbles)
        {
            _width = width;
            _height = height;
            _course = course;
            _marbles = marbles;
            
            // Initialize bitmap for drawing
            _bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            
            // Load typeface
            _typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        }
        
        /// <summary>
        /// Render a single frame of the marble run
        /// </summary>
        public void RenderFrame(string outputPath, CameraState camera)
        {
            using (var canvas = new SKCanvas(_bitmap))
            {
                // Clear the background
                DrawBackground(canvas);
                
                // Apply camera transform
                ApplyCameraTransform(canvas, camera);
                
                // Draw the course
                DrawCourse(canvas);
                
                // Draw the marbles
                DrawMarbles(canvas);
                
                // Reset camera transform
                canvas.ResetMatrix();
                
                // Draw HUD
                DrawHUD(canvas);
                
                // Save the frame as PNG
                using (var image = SKImage.FromBitmap(_bitmap))
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                using (var stream = File.OpenWrite(outputPath))
                {
                    data.SaveTo(stream);
                }
            }
        }
        
        /// <summary>
        /// Render the final results screen
        /// </summary>
        public void RenderResultsScreen(string outputPath, List<(Marble marble, int rank, float progress)> rankings)
        {
            using (var canvas = new SKCanvas(_bitmap))
            {
                // Clear the background with a special gradient
                using (var bgPaint = new SKPaint())
                using (var bgShader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0),
                    new SKPoint(0, _height),
                    new[] { new SKColor(20, 0, 40), new SKColor(80, 20, 100) },
                    null,
                    SKShaderTileMode.Clamp))
                {
                    bgPaint.Shader = bgShader;
                    canvas.DrawRect(0, 0, _width, _height, bgPaint);
                }
                
                // Draw a title
                using (var paint = new SKPaint())
                {
                    paint.Color = SKColors.White;
                    paint.TextSize = 80;
                    paint.IsAntialias = true;
                    paint.Typeface = _typeface;
                    paint.TextAlign = SKTextAlign.Center;
                    
                    canvas.DrawText("RACE RESULTS", _width / 2, 150, paint);
                    
                    // Draw a subtitle with a question
                    paint.TextSize = 50;
                    paint.Color = new SKColor(220, 220, 100);
                    canvas.DrawText("Which marble do YOU think", _width / 2, 250, paint);
                    canvas.DrawText("will win next time?", _width / 2, 320, paint);
                }
                
                // Draw a podium for the top 3
                DrawPodium(canvas, rankings);
                
                // Draw the rankings
                DrawRankings(canvas, rankings);
                
                // Draw a prompt for the next race
                using (var paint = new SKPaint())
                {
                    paint.Color = new SKColor(200, 200, 255);
                    paint.TextSize = 40;
                    paint.IsAntialias = true;
                    paint.Typeface = _typeface;
                    paint.TextAlign = SKTextAlign.Center;
                    
                    canvas.DrawText("Comment your prediction below!", _width / 2, _height - 200, paint);
                    canvas.DrawText("New race coming soon...", _width / 2, _height - 140, paint);
                }
                
                // Save the frame as PNG
                using (var image = SKImage.FromBitmap(_bitmap))
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                using (var stream = File.OpenWrite(outputPath))
                {
                    data.SaveTo(stream);
                }
            }
        }
        
        /// <summary>
        /// Draw the background for the race
        /// </summary>
        private void DrawBackground(SKCanvas canvas)
        {
            using (var bgPaint = new SKPaint())
            {
                // Create a gradient background
                using (var bgShader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0),
                    new SKPoint(0, _height),
                    new[] { _backgroundColor, _backgroundAccent },
                    null,
                    SKShaderTileMode.Clamp))
                {
                    bgPaint.Shader = bgShader;
                    canvas.DrawRect(0, 0, _width, _height, bgPaint);
                }
                
                // Add some ambient particles in the background
                Random random = new Random();
                int numParticles = 100;
                
                for (int i = 0; i < numParticles; i++)
                {
                    float x = (float)random.NextDouble() * _width;
                    float y = (float)random.NextDouble() * _height;
                    float size = (float)random.NextDouble() * 3 + 1;
                    
                    using (var particlePaint = new SKPaint())
                    {
                        byte alpha = (byte)(random.Next(40, 100));
                        particlePaint.Color = new SKColor(255, 255, 255, alpha);
                        canvas.DrawCircle(x, y, size, particlePaint);
                    }
                }
            }
        }
        
        /// <summary>
        /// Apply the camera transformation to the canvas
        /// </summary>
        private void ApplyCameraTransform(SKCanvas canvas, CameraState camera)
        {
            // Center of the screen
            float centerX = _width / 2;
            float centerY = _height / 2;
            
            // Translate to center, apply transformations, then translate to camera position
            canvas.Translate(centerX, centerY);
            canvas.Scale(camera.Zoom);
            canvas.RotateDegrees(camera.Rotation);
            canvas.Translate(-camera.Position.X, -camera.Position.Y);
        }
        
        /// <summary>
        /// Draw the course sections
        /// </summary>
        private void DrawCourse(SKCanvas canvas)
        {
            // Draw all course sections
            foreach (var section in _course.Sections)
            {
                DrawCourseSection(canvas, section);
            }
            
            // Draw start and finish markers
            DrawStartMarker(canvas, _course.StartPosition);
            DrawFinishMarker(canvas, _course.FinishPosition);
        }
        
        /// <summary>
        /// Draw a single course section
        /// </summary>
        private void DrawCourseSection(SKCanvas canvas, CourseSection section)
        {
            if (section.Vertices.Count < 3) return;
            
            // Create path from vertices
            using (var path = new SKPath())
            {
                path.MoveTo(section.Vertices[0].X, section.Vertices[0].Y);
                
                for (int i = 1; i < section.Vertices.Count; i++)
                {
                    path.LineTo(section.Vertices[i].X, section.Vertices[i].Y);
                }
                
                path.Close();
                
                // Draw with glow if enabled
                if (_useGlow && section.GlowIntensity > 0)
                {
                    using (var glowPaint = new SKPaint())
                    {
                        glowPaint.Style = SKPaintStyle.Stroke;
                        glowPaint.StrokeWidth = 10;
                        glowPaint.Color = section.Color.WithAlpha((byte)(section.GlowIntensity * 255));
                        glowPaint.IsAntialias = true;
                        glowPaint.ImageFilter = SKImageFilter.CreateBlur(15, 15);
                        
                        canvas.DrawPath(path, glowPaint);
                    }
                }
                
                // Draw main section
                using (var paint = new SKPaint())
                {
                    paint.Style = SKPaintStyle.Fill;
                    paint.Color = section.Color;
                    paint.IsAntialias = true;
                    
                    // Add texture/pattern based on section type
                    switch (section.Type)
                    {
                        case CourseSection.SectionType.Ramp:
                            // Add gradient for ramp
                            using (var shader = SKShader.CreateLinearGradient(
                                new SKPoint(section.Vertices[0].X, section.Vertices[0].Y),
                                new SKPoint(section.Vertices[2].X, section.Vertices[2].Y),
                                new[] { section.Color, section.Color.WithAlpha(150) },
                                null,
                                SKShaderTileMode.Clamp))
                            {
                                paint.Shader = shader;
                            }
                            break;
                            
                        case CourseSection.SectionType.Booster:
                            // Add animated-looking pattern for booster
                            using (var shader = SKShader.CreateLinearGradient(
                                new SKPoint(section.Vertices[0].X, section.Vertices[0].Y),
                                new SKPoint(section.Vertices[2].X, section.Vertices[2].Y),
                                new[] { 
                                    section.Color, 
                                    section.Color.WithAlpha(200),
                                    section.Color,
                                    section.Color.WithAlpha(200),
                                    section.Color
                                },
                                null,
                                SKShaderTileMode.Clamp))
                            {
                                paint.Shader = shader;
                            }
                            break;
                    }
                    
                    canvas.DrawPath(path, paint);
                    
                    // Draw outline
                    paint.Style = SKPaintStyle.Stroke;
                    paint.StrokeWidth = 2;
                    paint.Color = section.Color.WithAlpha(200);
                    paint.Shader = null;
                    
                    canvas.DrawPath(path, paint);
                }
            }
        }
        
        /// <summary>
        /// Draw the start marker
        /// </summary>
        private void DrawStartMarker(SKCanvas canvas, Vector2 position)
        {
            using (var paint = new SKPaint())
            {
                // Draw a circular marker
                paint.Style = SKPaintStyle.Fill;
                paint.Color = new SKColor(50, 200, 50, 150);
                paint.IsAntialias = true;
                
                canvas.DrawCircle(position.X, position.Y, 40, paint);
                
                // Add text
                paint.Color = SKColors.White;
                paint.TextSize = 24;
                paint.TextAlign = SKTextAlign.Center;
                paint.Typeface = _typeface;
                
                canvas.DrawText("START", position.X, position.Y + 8, paint);
            }
        }
        
        /// <summary>
        /// Draw the finish marker
        /// </summary>
        private void DrawFinishMarker(SKCanvas canvas, Vector2 position)
        {
            using (var paint = new SKPaint())
            {
                // Draw a circular marker with checkered pattern
                paint.Style = SKPaintStyle.Fill;
                paint.Color = new SKColor(200, 50, 50, 150);
                paint.IsAntialias = true;
                
                canvas.DrawCircle(position.X, position.Y, 40, paint);
                
                // Add checkered pattern
                paint.Color = new SKColor(0, 0, 0, 100);
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * (float)Math.PI / 4;
                    canvas.DrawArc(
                        new SKRect(position.X - 40, position.Y - 40, position.X + 40, position.Y + 40),
                        angle * 180 / (float)Math.PI,
                        45,
                        true,
                        paint
                    );
                }
                
                // Add text
                paint.Color = SKColors.White;
                paint.TextSize = 24;
                paint.TextAlign = SKTextAlign.Center;
                paint.Typeface = _typeface;
                
                canvas.DrawText("FINISH", position.X, position.Y + 8, paint);
            }
        }
        
        /// <summary>
        /// Draw all marbles and their effects
        /// </summary>
        private void DrawMarbles(SKCanvas canvas)
        {
            // Draw marbles in order of position (back to front)
            List<Marble> orderedMarbles = new List<Marble>(_marbles);
            orderedMarbles.Sort((a, b) => a.Position.Y.CompareTo(b.Position.Y));
            
            foreach (var marble in orderedMarbles)
            {
                // Draw particle effects first (behind the marble)
                DrawParticleEffects(canvas, marble);
                
                // Draw the marble itself
                DrawMarble(canvas, marble);
            }
        }
        
        /// <summary>
        /// Draw a single marble
        /// </summary>
        private void DrawMarble(SKCanvas canvas, Marble marble)
        {
            // Calculate shadow position
            Vector2 shadowPos = new Vector2(marble.Position.X + 5, marble.Position.Y + 8);
            
            // Draw shadow
            using (var shadowPaint = new SKPaint())
            {
                shadowPaint.Color = new SKColor(0, 0, 0, 100);
                shadowPaint.IsAntialias = true;
                
                if (_useBlur)
                {
                    shadowPaint.ImageFilter = SKImageFilter.CreateBlur(10, 10);
                }
                
                canvas.DrawCircle(shadowPos.X, shadowPos.Y, marble.Radius * 0.9f, shadowPaint);
            }
            
            // Draw the marble based on its type
            switch (marble.Pattern)
            {
                case TexturePattern.Clear:
                    DrawGlassMarble(canvas, marble);
                    break;
                    
                case TexturePattern.Metallic:
                    DrawMetallicMarble(canvas, marble);
                    break;
                    
                case TexturePattern.Swirl:
                    DrawSwirlMarble(canvas, marble);
                    break;
                    
                case TexturePattern.Grain:
                    DrawGrainMarble(canvas, marble);
                    break;
                    
                case TexturePattern.Faceted:
                    DrawFacetedMarble(canvas, marble);
                    break;
                    
                case TexturePattern.Galaxy:
                    DrawGalaxyMarble(canvas, marble);
                    break;
                    
                case TexturePattern.Glow:
                    DrawGlowMarble(canvas, marble);
                    break;
                    
                default:
                    DrawSolidMarble(canvas, marble);
                    break;
            }
            
            // Draw the marble ID or name
            using (var textPaint = new SKPaint())
            {
                textPaint.Color = SKColors.White;
                textPaint.TextSize = 16;
                textPaint.TextAlign = SKTextAlign.Center;
                textPaint.IsAntialias = true;
                textPaint.Typeface = _typeface;
                
                canvas.DrawText(
                    marble.Name,
                    marble.Position.X,
                    marble.Position.Y + 6,
                    textPaint
                );
            }
        }
        
        /// <summary>
        /// Draw marble particle effects
        /// </summary>
        private void DrawParticleEffects(SKCanvas canvas, Marble marble)
        {
            foreach (var effect in marble.ParticleEffects)
            {
                using (var paint = new SKPaint())
                {
                    // Base particle appearance
                    paint.Color = effect.Color;
                    paint.IsAntialias = true;
                    
                    // Fade out based on lifetime
                    float lifeFactor = 1.0f - (effect.CurrentLife / effect.LifeTime);
                    byte alpha = (byte)(255 * lifeFactor);
                    paint.Color = effect.Color.WithAlpha(alpha);
                      // Add blur for glow effect if enabled
                    if (_useBlur && (effect.Type.Equals(ParticleEffect.EffectType.Spark) || 
                                    effect.Type.Equals(ParticleEffect.EffectType.Collision)))
                    {
                        paint.ImageFilter = SKImageFilter.CreateBlur(effect.Size / 2, effect.Size / 2);
                    }
                    
                    // Draw the particle
                    canvas.DrawCircle(effect.Position.X, effect.Position.Y, effect.Size, paint);
                }
            }
        }
        
        /// <summary>
        /// Draw a solid-colored marble
        /// </summary>
        private void DrawSolidMarble(SKCanvas canvas, Marble marble)
        {
            using (var paint = new SKPaint())
            {
                paint.Color = marble.PrimaryColor;
                paint.IsAntialias = true;
                
                canvas.DrawCircle(marble.Position.X, marble.Position.Y, marble.Radius, paint);
                
                // Add highlight
                paint.Color = SKColors.White.WithAlpha(100);
                canvas.DrawCircle(
                    marble.Position.X - marble.Radius * 0.3f,
                    marble.Position.Y - marble.Radius * 0.3f,
                    marble.Radius * 0.4f,
                    paint
                );
            }
        }
        
        /// <summary>
        /// Draw a glass marble with transparency
        /// </summary>
        private void DrawGlassMarble(SKCanvas canvas, Marble marble)
        {
            using (var paint = new SKPaint())
            {
                // Outer glow for glass effect
                if (_useGlow)
                {
                    paint.Color = marble.PrimaryColor.WithAlpha(50);
                    paint.IsAntialias = true;
                    paint.ImageFilter = SKImageFilter.CreateBlur(15, 5);
                    canvas.DrawCircle(marble.Position.X, marble.Position.Y, marble.Radius * 1.1f, paint);
                }
                
                // Main glass sphere
                paint.Color = marble.PrimaryColor;
                paint.IsAntialias = true;
                
                // Create a radial gradient for glass effect
                using (var shader = SKShader.CreateRadialGradient(
                    new SKPoint(marble.Position.X - marble.Radius * 0.3f, marble.Position.Y - marble.Radius * 0.3f),
                    marble.Radius * 1.5f,
                    new[] { 
                        marble.PrimaryColor.WithAlpha(180),
                        marble.SecondaryColor.WithAlpha(100)
                    },
                    null,
                    SKShaderTileMode.Clamp))
                {
                    paint.Shader = shader;
                    canvas.DrawCircle(marble.Position.X, marble.Position.Y, marble.Radius, paint);
                }
                
                // Highlight
                paint.Shader = null;
                paint.Color = SKColors.White.WithAlpha(180);
                canvas.DrawCircle(
                    marble.Position.X - marble.Radius * 0.3f,
                    marble.Position.Y - marble.Radius * 0.3f,
                    marble.Radius * 0.3f,
                    paint
                );
            }
        }
        
        /// <summary>
        /// Draw a metallic marble with reflection
        /// </summary>
        private void DrawMetallicMarble(SKCanvas canvas, Marble marble)
        {
            using (var paint = new SKPaint())
            {
                paint.IsAntialias = true;
                
                // Create a linear gradient for metal effect
                using (var shader = SKShader.CreateLinearGradient(
                    new SKPoint(marble.Position.X - marble.Radius, marble.Position.Y - marble.Radius),
                    new SKPoint(marble.Position.X + marble.Radius, marble.Position.Y + marble.Radius),
                    new[] { 
                        marble.SecondaryColor,
                        marble.PrimaryColor,
                        marble.SecondaryColor,
                        marble.PrimaryColor
                    },
                    null,
                    SKShaderTileMode.Clamp))
                {
                    paint.Shader = shader;
                    canvas.DrawCircle(marble.Position.X, marble.Position.Y, marble.Radius, paint);
                }
                
                // Highlight
                paint.Shader = null;
                paint.Color = SKColors.White.WithAlpha(200);
                canvas.DrawCircle(
                    marble.Position.X - marble.Radius * 0.2f,
                    marble.Position.Y - marble.Radius * 0.2f,
                    marble.Radius * 0.3f,
                    paint
                );
            }
        }
        
        /// <summary>
        /// Draw a marble with a swirly pattern
        /// </summary>
        private void DrawSwirlMarble(SKCanvas canvas, Marble marble)
        {
            using (var paint = new SKPaint())
            {
                paint.IsAntialias = true;
                
                // Base color
                paint.Color = marble.PrimaryColor;
                canvas.DrawCircle(marble.Position.X, marble.Position.Y, marble.Radius, paint);
                
                // Draw swirl pattern
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = 3;
                paint.Color = marble.SecondaryColor;
                
                for (int i = 0; i < 3; i++)
                {
                    float startAngle = i * 120;
                    canvas.DrawArc(
                        new SKRect(
                            marble.Position.X - marble.Radius * 0.8f,
                            marble.Position.Y - marble.Radius * 0.8f,
                            marble.Position.X + marble.Radius * 0.8f,
                            marble.Position.Y + marble.Radius * 0.8f
                        ),
                        startAngle,
                        240,
                        false,
                        paint
                    );
                }
                
                // Highlight
                paint.Style = SKPaintStyle.Fill;
                paint.Color = SKColors.White.WithAlpha(120);
                canvas.DrawCircle(
                    marble.Position.X - marble.Radius * 0.3f,
                    marble.Position.Y - marble.Radius * 0.3f,
                    marble.Radius * 0.25f,
                    paint
                );
            }
        }
        
        /// <summary>
        /// Draw a marble with a wood grain pattern
        /// </summary>
        private void DrawGrainMarble(SKCanvas canvas, Marble marble)
        {
            using (var paint = new SKPaint())
            {
                paint.IsAntialias = true;
                
                // Base color
                using (var shader = SKShader.CreateLinearGradient(
                    new SKPoint(marble.Position.X - marble.Radius, marble.Position.Y),
                    new SKPoint(marble.Position.X + marble.Radius, marble.Position.Y),
                    new[] { marble.PrimaryColor, marble.SecondaryColor, marble.PrimaryColor },
                    null,
                    SKShaderTileMode.Clamp))
                {
                    paint.Shader = shader;
                    canvas.DrawCircle(marble.Position.X, marble.Position.Y, marble.Radius, paint);
                }
                
                // Draw grain lines
                paint.Shader = null;
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = 1;
                paint.Color = marble.SecondaryColor;
                
                Random random = new Random(marble.Id); // Consistent grain pattern
                
                for (int i = 0; i < 8; i++)
                {
                    float yOffset = (float)random.NextDouble() * marble.Radius * 2 - marble.Radius;
                    float curve = (float)random.NextDouble() * 30 - 15;
                    
                    using (var path = new SKPath())
                    {
                        path.MoveTo(marble.Position.X - marble.Radius, marble.Position.Y + yOffset);
                        path.QuadTo(
                            marble.Position.X, marble.Position.Y + yOffset + curve,
                            marble.Position.X + marble.Radius, marble.Position.Y + yOffset
                        );
                        
                        // Clip to marble circle
                        using (var clipPath = new SKPath())
                        {
                            clipPath.AddCircle(marble.Position.X, marble.Position.Y, marble.Radius);
                            canvas.Save();
                            canvas.ClipPath(clipPath);
                            canvas.DrawPath(path, paint);
                            canvas.Restore();
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Draw a faceted marble (like ice or crystal)
        /// </summary>
        private void DrawFacetedMarble(SKCanvas canvas, Marble marble)
        {
            using (var paint = new SKPaint())
            {
                paint.IsAntialias = true;
                
                // Base color with gradient
                using (var shader = SKShader.CreateRadialGradient(
                    new SKPoint(marble.Position.X, marble.Position.Y),
                    marble.Radius,
                    new[] { marble.PrimaryColor, marble.SecondaryColor },
                    null,
                    SKShaderTileMode.Clamp))
                {
                    paint.Shader = shader;
                    canvas.DrawCircle(marble.Position.X, marble.Position.Y, marble.Radius, paint);
                }
                
                // Draw facet lines
                paint.Shader = null;
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = 1;
                paint.Color = SKColors.White.WithAlpha(150);
                
                // Create consistent facet pattern for this marble
                Random random = new Random(marble.Id);
                int facets = random.Next(5, 8);
                
                for (int i = 0; i < facets; i++)
                {
                    float angle1 = (float)random.NextDouble() * 360;
                    float angle2 = (angle1 + 180 + (float)random.NextDouble() * 60 - 30) % 360;
                    
                    float x1 = marble.Position.X + marble.Radius * (float)Math.Cos(angle1 * Math.PI / 180);
                    float y1 = marble.Position.Y + marble.Radius * (float)Math.Sin(angle1 * Math.PI / 180);
                    
                    float x2 = marble.Position.X + marble.Radius * (float)Math.Cos(angle2 * Math.PI / 180);
                    float y2 = marble.Position.Y + marble.Radius * (float)Math.Sin(angle2 * Math.PI / 180);
                    
                    using (var path = new SKPath())
                    {
                        path.MoveTo(x1, y1);
                        path.LineTo(x2, y2);
                        
                        // Clip to marble circle
                        using (var clipPath = new SKPath())
                        {
                            clipPath.AddCircle(marble.Position.X, marble.Position.Y, marble.Radius);
                            canvas.Save();
                            canvas.ClipPath(clipPath);
                            canvas.DrawPath(path, paint);
                            canvas.Restore();
                        }
                    }
                }
                
                // Add shine
                paint.Style = SKPaintStyle.Fill;
                paint.Color = SKColors.White.WithAlpha(120);
                canvas.DrawCircle(
                    marble.Position.X - marble.Radius * 0.25f,
                    marble.Position.Y - marble.Radius * 0.25f,
                    marble.Radius * 0.3f,
                    paint
                );
            }
        }
        
        /// <summary>
        /// Draw a cosmic/galaxy-themed marble
        /// </summary>
        private void DrawGalaxyMarble(SKCanvas canvas, Marble marble)
        {
            using (var paint = new SKPaint())
            {
                paint.IsAntialias = true;
                
                // Draw outer glow if enabled
                if (_useGlow)
                {
                    paint.Color = marble.PrimaryColor.WithAlpha(80);
                    paint.ImageFilter = SKImageFilter.CreateBlur(20, 20);
                    canvas.DrawCircle(marble.Position.X, marble.Position.Y, marble.Radius * 1.2f, paint);
                    paint.ImageFilter = null;
                }
                
                // Base spiral galaxy effect
                using (var shader = SKShader.CreateSweepGradient(
                    new SKPoint(marble.Position.X, marble.Position.Y),
                    new[] { 
                        marble.PrimaryColor, 
                        marble.SecondaryColor,
                        marble.PrimaryColor,
                        marble.SecondaryColor,
                        marble.PrimaryColor 
                    },
                    null))
                {
                    paint.Shader = shader;
                    canvas.DrawCircle(marble.Position.X, marble.Position.Y, marble.Radius, paint);
                }
                
                // Add stars (small dots)
                paint.Shader = null;
                paint.Color = SKColors.White;
                
                Random random = new Random(marble.Id);
                int stars = 12;
                
                for (int i = 0; i < stars; i++)
                {
                    float angle = (float)random.NextDouble() * 360;
                    float dist = (float)random.NextDouble() * marble.Radius * 0.8f;
                    
                    float x = marble.Position.X + dist * (float)Math.Cos(angle * Math.PI / 180);
                    float y = marble.Position.Y + dist * (float)Math.Sin(angle * Math.PI / 180);
                    float size = (float)random.NextDouble() * 2 + 1;
                    
                    canvas.DrawCircle(x, y, size, paint);
                }
            }
        }
        
        /// <summary>
        /// Draw a glowing neon marble
        /// </summary>
        private void DrawGlowMarble(SKCanvas canvas, Marble marble)
        {
            using (var paint = new SKPaint())
            {
                paint.IsAntialias = true;
                
                // Draw outer glow if enabled
                if (_useGlow)
                {
                    paint.Color = marble.PrimaryColor.WithAlpha(100);
                    paint.ImageFilter = SKImageFilter.CreateBlur(25, 25);
                    canvas.DrawCircle(marble.Position.X, marble.Position.Y, marble.Radius * 1.3f, paint);
                    
                    paint.Color = marble.PrimaryColor.WithAlpha(150);
                    paint.ImageFilter = SKImageFilter.CreateBlur(15, 15);
                    canvas.DrawCircle(marble.Position.X, marble.Position.Y, marble.Radius * 1.15f, paint);
                    paint.ImageFilter = null;
                }
                
                // Core marble
                paint.Color = marble.PrimaryColor;
                canvas.DrawCircle(marble.Position.X, marble.Position.Y, marble.Radius, paint);
                
                // Inner glow
                paint.Color = SKColors.White.WithAlpha(180);
                canvas.DrawCircle(
                    marble.Position.X, 
                    marble.Position.Y, 
                    marble.Radius * 0.6f, 
                    paint
                );
                
                // Center highlight
                paint.Color = SKColors.White;
                canvas.DrawCircle(
                    marble.Position.X, 
                    marble.Position.Y, 
                    marble.Radius * 0.3f, 
                    paint
                );
            }
        }
        
        /// <summary>
        /// Draw the on-screen HUD with race information
        /// </summary>
        private void DrawHUD(SKCanvas canvas)
        {
            // Sort marbles by rank
            List<Marble> rankedMarbles = new List<Marble>(_marbles);
            rankedMarbles.Sort((a, b) => a.Rank.CompareTo(b.Rank));
            
            // Draw a translucent panel for the leaderboard
            int panelWidth = 300;
            int panelHeight = 50 + rankedMarbles.Count * 40;
            int panelX = 20;
            int panelY = 20;
            
            using (var paint = new SKPaint())
            {
                // Panel background
                paint.Color = new SKColor(0, 0, 0, 150);
                canvas.DrawRoundRect(new SKRect(panelX, panelY, panelX + panelWidth, panelY + panelHeight), 10, 10, paint);
                
                // Panel border
                paint.Style = SKPaintStyle.Stroke;
                paint.Color = new SKColor(255, 255, 255, 100);
                paint.StrokeWidth = 2;
                canvas.DrawRoundRect(new SKRect(panelX, panelY, panelX + panelWidth, panelY + panelHeight), 10, 10, paint);
                
                // Panel title
                paint.Style = SKPaintStyle.Fill;
                paint.Color = SKColors.White;
                paint.TextSize = 24;
                paint.Typeface = _typeface;
                paint.TextAlign = SKTextAlign.Center;
                canvas.DrawText("RANKINGS", panelX + panelWidth / 2, panelY + 35, paint);
                
                // Draw each marble in the leaderboard
                for (int i = 0; i < rankedMarbles.Count; i++)
                {
                    var marble = rankedMarbles[i];
                    int rowY = panelY + 70 + i * 40;
                    
                    // Rank number
                    paint.TextAlign = SKTextAlign.Left;
                    paint.TextSize = 20;
                    canvas.DrawText($"{marble.Rank}.", panelX + 20, rowY, paint);
                    
                    // Marble indicator (small colored circle)
                    paint.Color = marble.PrimaryColor;
                    canvas.DrawCircle(panelX + 50, rowY - 10, 10, paint);
                    
                    // Marble name
                    paint.Color = SKColors.White;
                    canvas.DrawText(marble.Name, panelX + 70, rowY, paint);
                    
                    // Finished time if applicable
                    if (marble.FinishTime >= 0)
                    {
                        paint.TextAlign = SKTextAlign.Right;
                        canvas.DrawText($"{marble.FinishTime:F2}s", panelX + panelWidth - 20, rowY, paint);
                    }
                }
            }
        }
        
        /// <summary>
        /// Draw the podium for the top 3 marbles
        /// </summary>
        private void DrawPodium(SKCanvas canvas, List<(Marble marble, int rank, float progress)> rankings)
        {
            // Get top 3 marbles
            var podiumMarbles = rankings.Take(Math.Min(3, rankings.Count)).ToList();
            
            // Draw podium platforms
            using (var paint = new SKPaint())
            {
                int podiumCenterX = _width / 2;
                int podiumY = 650;
                int podiumWidth = 120;
                int firstPlaceHeight = 180;
                int secondPlaceHeight = 140;
                int thirdPlaceHeight = 100;
                
                // First place (center)
                if (podiumMarbles.Count > 0)
                {
                    paint.Color = new SKColor(220, 220, 100); // Gold
                    canvas.DrawRect(
                        podiumCenterX - podiumWidth / 2,
                        podiumY - firstPlaceHeight,
                        podiumWidth,
                        firstPlaceHeight,
                        paint
                    );
                    
                    // Add text
                    paint.Color = SKColors.White;
                    paint.TextSize = 24;
                    paint.TextAlign = SKTextAlign.Center;
                    paint.Typeface = _typeface;
                    
                    canvas.DrawText("1ST", podiumCenterX, podiumY - 20, paint);
                    
                    // Draw the marble
                    DrawPodiumMarble(canvas, podiumMarbles[0].marble, new Vector2(podiumCenterX, podiumY - firstPlaceHeight - 50));
                }
                
                // Second place (left)
                if (podiumMarbles.Count > 1)
                {
                    paint.Color = new SKColor(200, 200, 200); // Silver
                    canvas.DrawRect(
                        podiumCenterX - podiumWidth * 1.5f,
                        podiumY - secondPlaceHeight,
                        podiumWidth,
                        secondPlaceHeight,
                        paint
                    );
                    
                    // Add text
                    paint.Color = SKColors.White;
                    canvas.DrawText("2ND", podiumCenterX - podiumWidth, podiumY - 20, paint);
                    
                    // Draw the marble
                    DrawPodiumMarble(canvas, podiumMarbles[1].marble, new Vector2(podiumCenterX - podiumWidth, podiumY - secondPlaceHeight - 50));
                }
                
                // Third place (right)
                if (podiumMarbles.Count > 2)
                {
                    paint.Color = new SKColor(180, 120, 60); // Bronze
                    canvas.DrawRect(
                        podiumCenterX + podiumWidth / 2,
                        podiumY - thirdPlaceHeight,
                        podiumWidth,
                        thirdPlaceHeight,
                        paint
                    );
                    
                    // Add text
                    paint.Color = SKColors.White;
                    canvas.DrawText("3RD", podiumCenterX + podiumWidth, podiumY - 20, paint);
                    
                    // Draw the marble
                    DrawPodiumMarble(canvas, podiumMarbles[2].marble, new Vector2(podiumCenterX + podiumWidth, podiumY - thirdPlaceHeight - 50));
                }
            }
        }
        
        /// <summary>
        /// Draw a marble on the podium with celebration effects
        /// </summary>
        private void DrawPodiumMarble(SKCanvas canvas, Marble marble, Vector2 position)
        {
            // Temporarily change the marble's position for drawing
            Vector2 originalPos = marble.Position;
            marble.Position = position;
            
            // Draw the marble
            DrawMarble(canvas, marble);
            
            // Draw celebration sparkles around the marble
            Random random = new Random();
            int sparkleCount = 12;
            
            using (var paint = new SKPaint())
            {
                paint.Color = SKColors.White;
                paint.IsAntialias = true;
                
                for (int i = 0; i < sparkleCount; i++)
                {
                    float angle = (float)i / sparkleCount * 360;
                    float distance = marble.Radius * 1.5f + random.Next(20);
                    
                    float x = position.X + distance * (float)Math.Cos(angle * Math.PI / 180);
                    float y = position.Y + distance * (float)Math.Sin(angle * Math.PI / 180);
                    
                    // Sparkle size varies
                    float size = 2 + (float)random.NextDouble() * 3;
                    
                    // Add glow if enabled
                    if (_useGlow)
                    {
                        paint.ImageFilter = SKImageFilter.CreateBlur(3, 3);
                    }
                    
                    // Draw the sparkle
                    canvas.DrawCircle(x, y, size, paint);
                    
                    // Small line emanating from the sparkle
                    float lineLength = 3 + (float)random.NextDouble() * 5;
                    float endX = x + lineLength * (float)Math.Cos(angle * Math.PI / 180);
                    float endY = y + lineLength * (float)Math.Sin(angle * Math.PI / 180);
                    
                    paint.StrokeWidth = 1;
                    canvas.DrawLine(x, y, endX, endY, paint);
                    
                    paint.ImageFilter = null;
                }
                
                // Add marble name and type with larger text
                paint.Color = SKColors.White;
                paint.TextSize = 28;
                paint.TextAlign = SKTextAlign.Center;
                paint.Typeface = _typeface;
                
                canvas.DrawText(marble.Name, position.X, position.Y + marble.Radius + 30, paint);
                
                paint.TextSize = 20;
                canvas.DrawText(marble.Type.ToString(), position.X, position.Y + marble.Radius + 55, paint);
            }
            
            // Restore original position
            marble.Position = originalPos;
        }
        
        /// <summary>
        /// Draw race rankings in a list format
        /// </summary>
        private void DrawRankings(SKCanvas canvas, List<(Marble marble, int rank, float progress)> rankings)
        {
            using (var paint = new SKPaint())
            {
                int startX = _width / 2 - 150;
                int startY = 900;
                int rowHeight = 40;
                
                // Header
                paint.Color = SKColors.White;
                paint.TextSize = 32;
                paint.TextAlign = SKTextAlign.Center;
                paint.Typeface = _typeface;
                canvas.DrawText("FINAL RANKINGS", _width / 2, startY - 20, paint);
                
                // Draw each row
                for (int i = 0; i < rankings.Count; i++)
                {
                    var (marble, rank, progress) = rankings[i];
                    int rowY = startY + i * rowHeight;
                    
                    // Rank
                    paint.TextSize = 24;
                    paint.TextAlign = SKTextAlign.Left;
                    canvas.DrawText($"{rank}.", startX, rowY, paint);
                    
                    // Marble color indicator
                    paint.Color = marble.PrimaryColor;
                    canvas.DrawCircle(startX + 40, rowY - 8, 12, paint);
                    
                    // Marble name and type
                    paint.Color = SKColors.White;
                    paint.TextAlign = SKTextAlign.Left;
                    canvas.DrawText(marble.Name, startX + 70, rowY, paint);
                    
                    // Finish time or progress
                    paint.TextAlign = SKTextAlign.Right;
                    if (marble.FinishTime >= 0)
                    {
                        canvas.DrawText($"{marble.FinishTime:F2}s", startX + 300, rowY, paint);
                    }
                    else
                    {
                        canvas.DrawText($"{progress * 100:F0}%", startX + 300, rowY, paint);
                    }
                }
            }
        }
        
        public void Dispose()
        {
            _bitmap?.Dispose();
            _typeface?.Dispose();
        }
    }
}
