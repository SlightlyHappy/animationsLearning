#ifndef RENDERER_H
#define RENDERER_H

#include <SDL.h>
#include "../maze/maze.h"
#include "../characters/character.h"

// Colors
typedef struct {
    Uint8 r;
    Uint8 g;
    Uint8 b;
    Uint8 a;
} Color;

// Particle effect types
typedef enum {
    PARTICLE_DUST,
    PARTICLE_SPARK,
    PARTICLE_CELEBRATION,
    PARTICLE_TELEPORT
} ParticleType;

// Texture IDs
typedef enum {
    TEXTURE_WALL,
    TEXTURE_FLOOR,
    TEXTURE_EXIT,
    TEXTURE_BREAKABLE,
    TEXTURE_CHARACTER_RUNNER,
    TEXTURE_CHARACTER_SMASHER,
    TEXTURE_CHARACTER_CLIMBER,
    TEXTURE_CHARACTER_TELEPORTER,
    TEXTURE_PARTICLE_DUST,
    TEXTURE_PARTICLE_SPARK,
    TEXTURE_CELEBRATION,
    TEXTURE_BACKGROUND,
    TEXTURE_COUNT
} TextureID;

// Renderer structure
typedef struct {
    SDL_Window* window;
    SDL_Renderer* sdl_renderer;
    SDL_Texture* textures[TEXTURE_COUNT];
    int screen_width;
    int screen_height;
    float camera_x;
    float camera_y;
    float camera_zoom;
    bool show_debug;
} Renderer;

// Function declarations
Renderer* renderer_create(int width, int height, const char* title);
void renderer_destroy(Renderer* renderer);
void renderer_clear(Renderer* renderer, Color background);
void renderer_present(Renderer* renderer);
void renderer_load_textures(Renderer* renderer);
void renderer_set_camera(Renderer* renderer, float x, float y, float zoom);
void renderer_draw_maze(Renderer* renderer, Maze* maze);
void renderer_draw_character(Renderer* renderer, Character* character);
void renderer_draw_debug_info(Renderer* renderer, int fps, int character_count);
void renderer_draw_text(Renderer* renderer, const char* text, int x, int y, Color color, float scale);
void renderer_add_particle_effect(Renderer* renderer, ParticleType type, float x, float y, int count);
void renderer_update_particles(Renderer* renderer, float dt);
void renderer_draw_particles(Renderer* renderer);
void renderer_draw_celebration(Renderer* renderer, Character* winner);

#endif // RENDERER_H
