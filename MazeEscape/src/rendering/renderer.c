#include "rendering/renderer.h"
#include <stdlib.h>
#include <string.h>
#include <math.h>

// Basic color constants
static const Color COLOR_BLACK = {0, 0, 0, 255};
static const Color COLOR_WHITE = {255, 255, 255, 255};
static const Color COLOR_RED = {255, 0, 0, 255};
static const Color COLOR_GREEN = {0, 255, 0, 255};
static const Color COLOR_BLUE = {0, 0, 255, 255};
static const Color COLOR_YELLOW = {255, 255, 0, 255};
static const Color COLOR_PURPLE = {128, 0, 128, 255};
static const Color COLOR_ORANGE = {255, 165, 0, 255};
static const Color COLOR_CYAN = {0, 255, 255, 255};
static const Color COLOR_MAGENTA = {255, 0, 255, 255};

// Particle structure
typedef struct {
    float x;
    float y;
    float vx;
    float vy;
    float lifetime;
    float max_lifetime;
    float size;
    Color color;
    ParticleType type;
    bool active;
} Particle;

// Maximum number of particles
#define MAX_PARTICLES 2000

// Local variables
static Particle particles[MAX_PARTICLES];
static int next_particle = 0;

// Create a renderer
Renderer* renderer_create(int width, int height, const char* title) {
    // Allocate renderer structure
    Renderer* renderer = (Renderer*)malloc(sizeof(Renderer));
    if (!renderer) return NULL;
    
    // Initialize SDL window and renderer
    renderer->window = SDL_CreateWindow(
        title,
        SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED,
        width, height,
        SDL_WINDOW_SHOWN
    );
    
    if (!renderer->window) {
        free(renderer);
        return NULL;
    }
    
    renderer->sdl_renderer = SDL_CreateRenderer(
        renderer->window, -1,
        SDL_RENDERER_ACCELERATED | SDL_RENDERER_PRESENTVSYNC
    );
    
    if (!renderer->sdl_renderer) {
        SDL_DestroyWindow(renderer->window);
        free(renderer);
        return NULL;
    }
    
    // Initialize renderer properties
    renderer->screen_width = width;
    renderer->screen_height = height;
    renderer->camera_x = 0;
    renderer->camera_y = 0;
    renderer->camera_zoom = 1.0f;
    renderer->show_debug = false;
    
    // Initialize textures to NULL
    for (int i = 0; i < TEXTURE_COUNT; i++) {
        renderer->textures[i] = NULL;
    }
    
    // Initialize particles
    for (int i = 0; i < MAX_PARTICLES; i++) {
        particles[i].active = false;
    }
    
    return renderer;
}

// Destroy a renderer
void renderer_destroy(Renderer* renderer) {
    if (!renderer) return;
    
    // Destroy textures
    for (int i = 0; i < TEXTURE_COUNT; i++) {
        if (renderer->textures[i]) {
            SDL_DestroyTexture(renderer->textures[i]);
        }
    }
    
    // Destroy SDL renderer and window
    if (renderer->sdl_renderer) {
        SDL_DestroyRenderer(renderer->sdl_renderer);
    }
    
    if (renderer->window) {
        SDL_DestroyWindow(renderer->window);
    }
    
    // Free renderer structure
    free(renderer);
}

// Clear the screen
void renderer_clear(Renderer* renderer, Color background) {
    SDL_SetRenderDrawColor(
        renderer->sdl_renderer,
        background.r, background.g, background.b, background.a
    );
    SDL_RenderClear(renderer->sdl_renderer);
}

// Present the rendered frame
void renderer_present(Renderer* renderer) {
    SDL_RenderPresent(renderer->sdl_renderer);
}

// Load textures
void renderer_load_textures(Renderer* renderer) {
    // TODO: Load actual textures from files
    // For now, we'll create colored squares as placeholders
    
    // Create surface for wall
    SDL_Surface* wall_surface = SDL_CreateRGBSurface(0, 40, 40, 32, 0, 0, 0, 0);
    SDL_FillRect(wall_surface, NULL, SDL_MapRGB(wall_surface->format, 100, 100, 120));
    renderer->textures[TEXTURE_WALL] = SDL_CreateTextureFromSurface(renderer->sdl_renderer, wall_surface);
    SDL_FreeSurface(wall_surface);
    
    // Create surface for floor
    SDL_Surface* floor_surface = SDL_CreateRGBSurface(0, 40, 40, 32, 0, 0, 0, 0);
    SDL_FillRect(floor_surface, NULL, SDL_MapRGB(floor_surface->format, 200, 200, 220));
    renderer->textures[TEXTURE_FLOOR] = SDL_CreateTextureFromSurface(renderer->sdl_renderer, floor_surface);
    SDL_FreeSurface(floor_surface);
    
    // Create surface for exit
    SDL_Surface* exit_surface = SDL_CreateRGBSurface(0, 40, 40, 32, 0, 0, 0, 0);
    SDL_FillRect(exit_surface, NULL, SDL_MapRGB(exit_surface->format, 50, 200, 50));
    renderer->textures[TEXTURE_EXIT] = SDL_CreateTextureFromSurface(renderer->sdl_renderer, exit_surface);
    SDL_FreeSurface(exit_surface);
    
    // Create surface for breakable wall
    SDL_Surface* breakable_surface = SDL_CreateRGBSurface(0, 40, 40, 32, 0, 0, 0, 0);
    SDL_FillRect(breakable_surface, NULL, SDL_MapRGB(breakable_surface->format, 180, 120, 100));
    renderer->textures[TEXTURE_BREAKABLE] = SDL_CreateTextureFromSurface(renderer->sdl_renderer, breakable_surface);
    SDL_FreeSurface(breakable_surface);
    
    // Create surfaces for characters
    SDL_Surface* runner_surface = SDL_CreateRGBSurface(0, 30, 30, 32, 0, 0, 0, 0);
    SDL_FillRect(runner_surface, NULL, SDL_MapRGB(runner_surface->format, 50, 150, 255));
    renderer->textures[TEXTURE_CHARACTER_RUNNER] = SDL_CreateTextureFromSurface(renderer->sdl_renderer, runner_surface);
    SDL_FreeSurface(runner_surface);
    
    SDL_Surface* smasher_surface = SDL_CreateRGBSurface(0, 30, 30, 32, 0, 0, 0, 0);
    SDL_FillRect(smasher_surface, NULL, SDL_MapRGB(smasher_surface->format, 255, 50, 50));
    renderer->textures[TEXTURE_CHARACTER_SMASHER] = SDL_CreateTextureFromSurface(renderer->sdl_renderer, smasher_surface);
    SDL_FreeSurface(smasher_surface);
    
    SDL_Surface* climber_surface = SDL_CreateRGBSurface(0, 30, 30, 32, 0, 0, 0, 0);
    SDL_FillRect(climber_surface, NULL, SDL_MapRGB(climber_surface->format, 255, 200, 50));
    renderer->textures[TEXTURE_CHARACTER_CLIMBER] = SDL_CreateTextureFromSurface(renderer->sdl_renderer, climber_surface);
    SDL_FreeSurface(climber_surface);
    
    SDL_Surface* teleporter_surface = SDL_CreateRGBSurface(0, 30, 30, 32, 0, 0, 0, 0);
    SDL_FillRect(teleporter_surface, NULL, SDL_MapRGB(teleporter_surface->format, 200, 50, 255));
    renderer->textures[TEXTURE_CHARACTER_TELEPORTER] = SDL_CreateTextureFromSurface(renderer->sdl_renderer, teleporter_surface);
    SDL_FreeSurface(teleporter_surface);
    
    // Create surfaces for particles
    SDL_Surface* dust_surface = SDL_CreateRGBSurface(0, 8, 8, 32, 0, 0, 0, 0);
    SDL_FillRect(dust_surface, NULL, SDL_MapRGB(dust_surface->format, 200, 200, 200));
    renderer->textures[TEXTURE_PARTICLE_DUST] = SDL_CreateTextureFromSurface(renderer->sdl_renderer, dust_surface);
    SDL_FreeSurface(dust_surface);
    
    SDL_Surface* spark_surface = SDL_CreateRGBSurface(0, 8, 8, 32, 0, 0, 0, 0);
    SDL_FillRect(spark_surface, NULL, SDL_MapRGB(spark_surface->format, 255, 220, 150));
    renderer->textures[TEXTURE_PARTICLE_SPARK] = SDL_CreateTextureFromSurface(renderer->sdl_renderer, spark_surface);
    SDL_FreeSurface(spark_surface);
    
    // Celebration texture
    SDL_Surface* celebration_surface = SDL_CreateRGBSurface(0, 16, 16, 32, 0, 0, 0, 0);
    SDL_FillRect(celebration_surface, NULL, SDL_MapRGB(celebration_surface->format, 255, 255, 100));
    renderer->textures[TEXTURE_CELEBRATION] = SDL_CreateTextureFromSurface(renderer->sdl_renderer, celebration_surface);
    SDL_FreeSurface(celebration_surface);
    
    // Background texture
    SDL_Surface* background_surface = SDL_CreateRGBSurface(0, 100, 100, 32, 0, 0, 0, 0);
    SDL_FillRect(background_surface, NULL, SDL_MapRGB(background_surface->format, 20, 20, 40));
    renderer->textures[TEXTURE_BACKGROUND] = SDL_CreateTextureFromSurface(renderer->sdl_renderer, background_surface);
    SDL_FreeSurface(background_surface);
}

// Set camera position and zoom
void renderer_set_camera(Renderer* renderer, float x, float y, float zoom) {
    renderer->camera_x = x;
    renderer->camera_y = y;
    renderer->camera_zoom = zoom;
}

// Convert world coordinates to screen coordinates
static void world_to_screen(Renderer* renderer, float wx, float wy, int* sx, int* sy) {
    float zoom = renderer->camera_zoom;
    *sx = (int)((wx - renderer->camera_x) * zoom + renderer->screen_width / 2);
    *sy = (int)((wy - renderer->camera_y) * zoom + renderer->screen_height / 2);
}

// Draw maze
void renderer_draw_maze(Renderer* renderer, Maze* maze) {
    // Draw background pattern
    SDL_Rect bg_rect = {0, 0, 100, 100};
    for (int x = 0; x < renderer->screen_width; x += 100) {
        for (int y = 0; y < renderer->screen_height; y += 100) {
            bg_rect.x = x;
            bg_rect.y = y;
            SDL_RenderCopy(renderer->sdl_renderer, renderer->textures[TEXTURE_BACKGROUND], NULL, &bg_rect);
        }
    }
    
    // Calculate visible range of cells
    int cell_size = maze->cell_size;
    float zoom = renderer->camera_zoom;
    int scaled_cell_size = (int)(cell_size * zoom);
    
    int min_x = (int)((renderer->camera_x - renderer->screen_width / (2 * zoom)) / cell_size) - 1;
    int min_y = (int)((renderer->camera_y - renderer->screen_height / (2 * zoom)) / cell_size) - 1;
    int max_x = (int)((renderer->camera_x + renderer->screen_width / (2 * zoom)) / cell_size) + 1;
    int max_y = (int)((renderer->camera_y + renderer->screen_height / (2 * zoom)) / cell_size) + 1;
    
    // Clamp to maze bounds
    if (min_x < 0) min_x = 0;
    if (min_y < 0) min_y = 0;
    if (max_x >= maze->width) max_x = maze->width - 1;
    if (max_y >= maze->height) max_y = maze->height - 1;
    
    // Draw visible cells
    SDL_Rect cell_rect;
    for (int y = min_y; y <= max_y; y++) {
        for (int x = min_x; x <= max_x; x++) {
            // Convert world coords to screen coords
            int screen_x, screen_y;
            world_to_screen(renderer, x * cell_size, y * cell_size, &screen_x, &screen_y);
            
            cell_rect.x = screen_x;
            cell_rect.y = screen_y;
            cell_rect.w = scaled_cell_size + 1;  // +1 to avoid gaps
            cell_rect.h = scaled_cell_size + 1;
            
            // Choose texture based on cell type
            SDL_Texture* texture = NULL;
            switch (maze->cells[x][y]) {
                case CELL_WALL:
                    texture = renderer->textures[TEXTURE_WALL];
                    break;
                case CELL_EMPTY:
                    texture = renderer->textures[TEXTURE_FLOOR];
                    break;
                case CELL_START:
                    texture = renderer->textures[TEXTURE_FLOOR];
                    break;
                case CELL_EXIT:
                    texture = renderer->textures[TEXTURE_EXIT];
                    break;
                case CELL_BREAKABLE:
                    texture = renderer->textures[TEXTURE_BREAKABLE];
                    break;
                case CELL_SPECIAL:
                    texture = renderer->textures[TEXTURE_FLOOR];
                    
                    // Draw special marker
                    SDL_SetRenderDrawColor(renderer->sdl_renderer, 
                        COLOR_CYAN.r, COLOR_CYAN.g, COLOR_CYAN.b, COLOR_CYAN.a);
                    
                    SDL_Rect special_rect = cell_rect;
                    special_rect.x += cell_rect.w / 4;
                    special_rect.y += cell_rect.h / 4;
                    special_rect.w = cell_rect.w / 2;
                    special_rect.h = cell_rect.h / 2;
                    
                    SDL_RenderFillRect(renderer->sdl_renderer, &special_rect);
                    break;
            }
            
            if (texture) {
                SDL_RenderCopy(renderer->sdl_renderer, texture, NULL, &cell_rect);
            }
        }
    }
    
    // Draw exit marker
    int exit_screen_x, exit_screen_y;
    world_to_screen(renderer, 
        maze->exit_x * cell_size + cell_size / 2, 
        maze->exit_y * cell_size + cell_size / 2, 
        &exit_screen_x, &exit_screen_y);
    
    // Pulse effect for exit
    int pulse_size = (int)(sin(SDL_GetTicks() / 300.0f) * 5 + 20) * zoom;
    
    SDL_SetRenderDrawColor(renderer->sdl_renderer, 0, 255, 0, 100);
    for (int i = 0; i < 3; i++) {
        int size = pulse_size + i * 8 * zoom;
        SDL_Rect pulse_rect = {
            exit_screen_x - size / 2,
            exit_screen_y - size / 2,
            size, size
        };
        SDL_RenderFillRect(renderer->sdl_renderer, &pulse_rect);
    }
}

// Draw a character
void renderer_draw_character(Renderer* renderer, Character* character) {
    // Skip if character has escaped
    if (character->has_escaped) return;
    
    // Convert world position to screen position
    int screen_x, screen_y;
    world_to_screen(renderer, character->x, character->y, &screen_x, &screen_y);
    
    // Calculate size based on zoom
    int size = (int)(character->size * renderer->camera_zoom);
    
    // Destination rectangle
    SDL_Rect dest_rect = {
        screen_x - size / 2,
        screen_y - size / 2,
        size, size
    };
    
    // Choose texture based on character type
    SDL_Texture* texture = NULL;
    switch (character->type) {
        case CHARACTER_RUNNER:
            texture = renderer->textures[TEXTURE_CHARACTER_RUNNER];
            break;
        case CHARACTER_SMASHER:
            texture = renderer->textures[TEXTURE_CHARACTER_SMASHER];
            break;
        case CHARACTER_CLIMBER:
            texture = renderer->textures[TEXTURE_CHARACTER_CLIMBER];
            break;
        case CHARACTER_TELEPORTER:
            texture = renderer->textures[TEXTURE_CHARACTER_TELEPORTER];
            break;
    }
    
    if (texture) {
        // Create rotation and center point for rotation
        double angle = character->angle * 180.0 / M_PI;
        SDL_RenderCopyEx(
            renderer->sdl_renderer, texture,
            NULL, &dest_rect,
            angle, NULL, SDL_FLIP_NONE
        );
    }
    
    // Draw state indicator
    if (character->state == STATE_USING_ABILITY) {
        // Draw ability cooldown effect
        float cooldown_pct = character->ability_cooldown_remaining / character->cooldown;
        int indicator_size = size / 3;
        
        SDL_Rect indicator_rect = {
            screen_x - indicator_size / 2,
            screen_y - size / 2 - indicator_size * 2,
            indicator_size, indicator_size
        };
        
        SDL_SetRenderDrawColor(renderer->sdl_renderer, 255, 255, 255, 200);
        SDL_RenderFillRect(renderer->sdl_renderer, &indicator_rect);
        
        // Cooldown bar
        SDL_Rect cooldown_rect = {
            screen_x - size / 2,
            screen_y + size / 2 + 5,
            (int)(size * (1.0f - cooldown_pct)), 3
        };
        
        SDL_SetRenderDrawColor(renderer->sdl_renderer, 50, 255, 50, 200);
        SDL_RenderFillRect(renderer->sdl_renderer, &cooldown_rect);
    }
    
    // Draw character name
    // This would use renderer_draw_text in a full implementation
}

// Draw debug information
void renderer_draw_debug_info(Renderer* renderer, int fps, int character_count) {
    char buffer[256];
    sprintf(buffer, "FPS: %d | Characters: %d | Camera: %.1f, %.1f (%.1fx)", 
        fps, character_count, renderer->camera_x, renderer->camera_y, renderer->camera_zoom);
    
    // This would use renderer_draw_text in a full implementation
    // For now, just draw a debug box
    SDL_Rect debug_rect = {10, 10, 300, 20};
    SDL_SetRenderDrawColor(renderer->sdl_renderer, 0, 0, 0, 128);
    SDL_RenderFillRect(renderer->sdl_renderer, &debug_rect);
}

// Draw text - stub implementation
void renderer_draw_text(Renderer* renderer, const char* text, int x, int y, Color color, float scale) {
    // In a full implementation, this would use SDL_ttf
    // For now, just draw a placeholder rectangle
    SDL_SetRenderDrawColor(renderer->sdl_renderer, color.r, color.g, color.b, color.a);
    SDL_Rect text_rect = {x, y, (int)(strlen(text) * 8 * scale), (int)(16 * scale)};
    SDL_RenderDrawRect(renderer->sdl_renderer, &text_rect);
}

// Add particle effect
void renderer_add_particle_effect(Renderer* renderer, ParticleType type, float x, float y, int count) {
    for (int i = 0; i < count; i++) {
        // Find an inactive particle
        Particle* p = &particles[next_particle];
        next_particle = (next_particle + 1) % MAX_PARTICLES;
        
        // Reuse this particle slot
        p->active = true;
        p->x = x;
        p->y = y;
        p->type = type;
        
        // Set properties based on type
        switch (type) {
            case PARTICLE_DUST:
                p->vx = ((rand() % 100) - 50) / 50.0f * 30.0f;
                p->vy = ((rand() % 100) - 50) / 50.0f * 30.0f;
                p->lifetime = p->max_lifetime = 0.5f + (rand() % 100) / 100.0f * 0.5f;
                p->size = 3.0f + (rand() % 100) / 100.0f * 3.0f;
                p->color = COLOR_WHITE;
                p->color.a = 128;
                break;
                
            case PARTICLE_SPARK:
                p->vx = ((rand() % 100) - 50) / 50.0f * 80.0f;
                p->vy = ((rand() % 100) - 50) / 50.0f * 80.0f;
                p->lifetime = p->max_lifetime = 0.3f + (rand() % 100) / 100.0f * 0.2f;
                p->size = 2.0f + (rand() % 100) / 100.0f * 2.0f;
                p->color = COLOR_YELLOW;
                break;
                
            case PARTICLE_CELEBRATION:
                p->vx = ((rand() % 100) - 50) / 50.0f * 50.0f;
                p->vy = ((rand() % 100) - 60) / 50.0f * 80.0f; // More upward bias
                p->lifetime = p->max_lifetime = 1.0f + (rand() % 100) / 100.0f * 2.0f;
                p->size = 5.0f + (rand() % 100) / 100.0f * 5.0f;
                
                // Random festive color
                switch (rand() % 6) {
                    case 0: p->color = COLOR_RED; break;
                    case 1: p->color = COLOR_GREEN; break;
                    case 2: p->color = COLOR_BLUE; break;
                    case 3: p->color = COLOR_YELLOW; break;
                    case 4: p->color = COLOR_PURPLE; break;
                    case 5: p->color = COLOR_CYAN; break;
                }
                break;
                
            case PARTICLE_TELEPORT:
                p->vx = ((rand() % 100) - 50) / 50.0f * 30.0f;
                p->vy = ((rand() % 100) - 50) / 50.0f * 30.0f;
                p->lifetime = p->max_lifetime = 0.3f + (rand() % 100) / 100.0f * 0.2f;
                p->size = 4.0f + (rand() % 100) / 100.0f * 4.0f;
                p->color = COLOR_MAGENTA;
                break;
        }
    }
}

// Update particles
void renderer_update_particles(Renderer* renderer, float dt) {
    for (int i = 0; i < MAX_PARTICLES; i++) {
        if (!particles[i].active) continue;
        
        // Update lifetime
        particles[i].lifetime -= dt;
        if (particles[i].lifetime <= 0) {
            particles[i].active = false;
            continue;
        }
        
        // Update position
        particles[i].x += particles[i].vx * dt;
        particles[i].y += particles[i].vy * dt;
        
        // Apply gravity for some particles
        if (particles[i].type == PARTICLE_CELEBRATION) {
            particles[i].vy += 50.0f * dt;
        }
    }
}

// Draw particles
void renderer_draw_particles(Renderer* renderer) {
    for (int i = 0; i < MAX_PARTICLES; i++) {
        if (!particles[i].active) continue;
        
        // Convert world position to screen position
        int screen_x, screen_y;
        world_to_screen(renderer, particles[i].x, particles[i].y, &screen_x, &screen_y);
        
        // Calculate alpha based on lifetime
        float alpha_factor = particles[i].lifetime / particles[i].max_lifetime;
        Uint8 alpha = (Uint8)(particles[i].color.a * alpha_factor);
        
        // Calculate size based on lifetime and zoom
        float size_factor = 0.5f + 0.5f * alpha_factor;
        int size = (int)(particles[i].size * size_factor * renderer->camera_zoom);
        
        // Set color
        SDL_SetRenderDrawColor(
            renderer->sdl_renderer,
            particles[i].color.r,
            particles[i].color.g,
            particles[i].color.b,
            alpha
        );
        
        // Draw particle
        SDL_Rect particle_rect = {
            screen_x - size / 2,
            screen_y - size / 2,
            size, size
        };
        SDL_RenderFillRect(renderer->sdl_renderer, &particle_rect);
    }
}

// Draw celebration effect
void renderer_draw_celebration(Renderer* renderer, Character* winner) {
    // Generate celebration particles
    if ((SDL_GetTicks() % 100) < 20) {
        float x = winner->x + ((rand() % 100) - 50) / 50.0f * 30.0f;
        float y = winner->y + ((rand() % 100) - 50) / 50.0f * 30.0f;
        renderer_add_particle_effect(renderer, PARTICLE_CELEBRATION, x, y, 10);
    }
    
    // Draw winner banner
    SDL_Rect banner_rect = {
        renderer->screen_width / 4,
        renderer->screen_height / 4,
        renderer->screen_width / 2,
        renderer->screen_height / 8
    };
    
    SDL_SetRenderDrawColor(renderer->sdl_renderer, 0, 0, 0, 200);
    SDL_RenderFillRect(renderer->sdl_renderer, &banner_rect);
    
    SDL_SetRenderDrawColor(renderer->sdl_renderer, 255, 215, 0, 255);
    SDL_Rect border_rect = banner_rect;
    border_rect.x -= 3;
    border_rect.y -= 3;
    border_rect.w += 6;
    border_rect.h += 6;
    SDL_RenderDrawRect(renderer->sdl_renderer, &border_rect);
    
    // Draw text "WINNER" in the banner
    // In a full implementation, this would use SDL_ttf
    // For now, just render a placeholder
    char buffer[64];
    sprintf(buffer, "%s WINS!", winner->name);
    renderer_draw_text(
        renderer,
        buffer,
        renderer->screen_width / 2 - strlen(buffer) * 8 / 2,
        renderer->screen_height / 4 + 20,
        COLOR_WHITE,
        2.0f
    );
}
