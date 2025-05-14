#ifndef MAZE_H
#define MAZE_H

#include <stdbool.h>
#include "chipmunk/chipmunk.h"

// Cell types
typedef enum {
    CELL_EMPTY = 0,
    CELL_WALL = 1,
    CELL_START = 2,
    CELL_EXIT = 3,
    CELL_BREAKABLE = 4,
    CELL_SPECIAL = 5
} CellType;

// Maze structure
typedef struct {
    int width;
    int height;
    CellType** cells;
    int* start_positions;  // [x1, y1, x2, y2, ...] for multiple characters
    int exit_x;
    int exit_y;
    int cell_size;         // Size in pixels
    cpSpace* physics_space; // Chipmunk physics space reference
} Maze;

// Function declarations
Maze* maze_create(int width, int height, int cell_size);
void maze_generate(Maze* maze, unsigned int seed);
void maze_destroy(Maze* maze);
bool maze_is_wall(Maze* maze, int x, int y);
void maze_set_cell(Maze* maze, int x, int y, CellType type);
CellType maze_get_cell(Maze* maze, int x, int y);
void maze_add_physics_bodies(Maze* maze, cpSpace* space);
void maze_break_wall(Maze* maze, int x, int y);
void maze_update(Maze* maze, float dt);
void maze_get_path_to_exit(Maze* maze, int start_x, int start_y, int** path, int* path_length);

#endif // MAZE_H
