#ifndef MAZE_ESCAPE_H
#define MAZE_ESCAPE_H

#include <stdio.h>
#include <stdlib.h>
#include <stdbool.h>
#include <time.h>
#include <string.h>
#include <SDL.h>
#include "chipmunk/chipmunk.h"
#include "maze/maze.h"
#include "characters/character.h"
#include "physics/physics.h"
#include "rendering/renderer.h"
#include "video/encoder.h"

// Application settings
typedef struct {
    int maze_width;
    int maze_height;
    int cell_size;
    char* character_types;
    int simulation_duration;
    unsigned int random_seed;
    char* output_filename;
    int video_width;
    int video_height;
    int fps;
    float zoom_level;
    bool debug_mode;
} AppSettings;

// Global declarations
extern AppSettings app_settings;

// Function declarations
void parse_arguments(int argc, char* argv[]);
void initialize_simulation(void);
void run_simulation(void);
void cleanup_simulation(void);

#endif // MAZE_ESCAPE_H
