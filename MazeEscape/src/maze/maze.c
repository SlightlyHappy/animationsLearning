#include "maze/maze.h"
#include <stdlib.h>
#include <string.h>
#include <time.h>

// Directions for maze generation
typedef enum {
    DIR_NORTH = 0,
    DIR_EAST = 1,
    DIR_SOUTH = 2,
    DIR_WEST = 3
} Direction;

// Direction vectors
const int DIR_X[4] = {0, 1, 0, -1};
const int DIR_Y[4] = {-1, 0, 1, 0};

// Local function prototypes
static void carve_passages_from(Maze* maze, int cx, int cy, unsigned int* seed);
static unsigned int random_next(unsigned int* seed);
static void shuffle_directions(int directions[4], unsigned int* seed);

// Create a new maze
Maze* maze_create(int width, int height, int cell_size) {
    Maze* maze = (Maze*)malloc(sizeof(Maze));
    if (!maze) return NULL;
    
    // Initialize maze properties
    maze->width = width;
    maze->height = height;
    maze->cell_size = cell_size;
    maze->physics_space = NULL;
    
    // Allocate cell grid
    maze->cells = (CellType**)malloc(width * sizeof(CellType*));
    for (int x = 0; x < width; x++) {
        maze->cells[x] = (CellType*)malloc(height * sizeof(CellType));
        // Initialize all cells as walls
        for (int y = 0; y < height; y++) {
            maze->cells[x][y] = CELL_WALL;
        }
    }
    
    // Allocate start positions for characters (maximum 4 characters)
    maze->start_positions = (int*)malloc(8 * sizeof(int)); // x,y for 4 characters
    
    return maze;
}

// Generate a maze using recursive backtracking algorithm
void maze_generate(Maze* maze, unsigned int seed) {
    // If seed is 0, use current time
    if (seed == 0) {
        seed = (unsigned int)time(NULL);
    }
    
    // Start with all walls
    for (int x = 0; x < maze->width; x++) {
        for (int y = 0; y < maze->height; y++) {
            maze->cells[x][y] = CELL_WALL;
        }
    }
    
    // Carve passages starting from a random point
    int start_x = random_next(&seed) % (maze->width / 2) + maze->width / 4;
    int start_y = random_next(&seed) % (maze->height / 2) + maze->height / 4;
    carve_passages_from(maze, start_x, start_y, &seed);
    
    // Place entrance at the top of the maze
    int entrance_x = random_next(&seed) % (maze->width - 4) + 2;
    int entrance_y = 0;
    maze->cells[entrance_x][entrance_y] = CELL_START;
    maze->cells[entrance_x][entrance_y + 1] = CELL_EMPTY;
    
    // Place exit at the bottom of the maze
    int exit_x = random_next(&seed) % (maze->width - 4) + 2;
    int exit_y = maze->height - 1;
    maze->cells[exit_x][exit_y] = CELL_EXIT;
    maze->cells[exit_x][exit_y - 1] = CELL_EMPTY;
    
    // Store exit coordinates
    maze->exit_x = exit_x;
    maze->exit_y = exit_y;
    
    // Place starting positions for characters
    int char_count = 4; // Maximum 4 characters
    for (int i = 0; i < char_count; i++) {
        int offset_x = (i % 2 == 0) ? -1 : 1;
        int offset_y = (i / 2 == 0) ? 0 : 1;
        
        // Store starting positions
        maze->start_positions[i * 2] = entrance_x + offset_x;
        maze->start_positions[i * 2 + 1] = entrance_y + 1 + offset_y;
        
        // Make sure the starting positions are open
        maze->cells[entrance_x + offset_x][entrance_y + 1 + offset_y] = CELL_EMPTY;
    }
    
    // Add some breakable walls
    int breakable_count = (maze->width * maze->height) / 20; // 5% of cells are breakable
    for (int i = 0; i < breakable_count; i++) {
        int x = random_next(&seed) % (maze->width - 2) + 1;
        int y = random_next(&seed) % (maze->height - 2) + 1;
        
        // Only replace walls with breakable walls
        if (maze->cells[x][y] == CELL_WALL) {
            maze->cells[x][y] = CELL_BREAKABLE;
        }
    }
    
    // Add some special cells
    int special_count = (maze->width * maze->height) / 40; // 2.5% of cells are special
    for (int i = 0; i < special_count; i++) {
        int x = random_next(&seed) % (maze->width - 2) + 1;
        int y = random_next(&seed) % (maze->height - 2) + 1;
        
        // Only place special cells in empty spaces
        if (maze->cells[x][y] == CELL_EMPTY) {
            maze->cells[x][y] = CELL_SPECIAL;
        }
    }
}

// Free maze resources
void maze_destroy(Maze* maze) {
    if (!maze) return;
    
    // Free the cell grid
    for (int x = 0; x < maze->width; x++) {
        free(maze->cells[x]);
    }
    free(maze->cells);
    
    // Free start positions
    free(maze->start_positions);
    
    // Free maze structure
    free(maze);
}

// Check if a cell is a wall
bool maze_is_wall(Maze* maze, int x, int y) {
    // Check bounds
    if (x < 0 || x >= maze->width || y < 0 || y >= maze->height) {
        return true;
    }
    
    return maze->cells[x][y] == CELL_WALL || maze->cells[x][y] == CELL_BREAKABLE;
}

// Set the type of a cell
void maze_set_cell(Maze* maze, int x, int y, CellType type) {
    // Check bounds
    if (x < 0 || x >= maze->width || y < 0 || y >= maze->height) {
        return;
    }
    
    maze->cells[x][y] = type;
}

// Get the type of a cell
CellType maze_get_cell(Maze* maze, int x, int y) {
    // Check bounds
    if (x < 0 || x >= maze->width || y < 0 || y >= maze->height) {
        return CELL_WALL;
    }
    
    return maze->cells[x][y];
}

// Add physics bodies for maze walls
void maze_add_physics_bodies(Maze* maze, cpSpace* space) {
    // Create static body for all walls
    cpBody* static_body = physics_create_static_body(space);
    
    // Add each wall as a box shape
    for (int x = 0; x < maze->width; x++) {
        for (int y = 0; y < maze->height; y++) {
            if (maze->cells[x][y] == CELL_WALL || maze->cells[x][y] == CELL_BREAKABLE) {
                float px = x * maze->cell_size;
                float py = y * maze->cell_size;
                
                // Add collision based on cell type
                CollisionType type = (maze->cells[x][y] == CELL_WALL) 
                    ? COLLISION_WALL 
                    : COLLISION_BREAKABLE_WALL;
                
                physics_add_box(
                    space, static_body, 
                    maze->cell_size, maze->cell_size,
                    1.0f, type
                );
                
                // Position the shape
                cpBodySetPosition(static_body, cpv(
                    px + maze->cell_size / 2.0f,
                    py + maze->cell_size / 2.0f
                ));
            } else if (maze->cells[x][y] == CELL_EXIT) {
                float px = x * maze->cell_size;
                float py = y * maze->cell_size;
                
                // Add exit sensor
                cpShape* sensor = physics_add_box(
                    space, static_body,
                    maze->cell_size, maze->cell_size,
                    0.0f, COLLISION_EXIT
                );
                
                // Mark as sensor (doesn't block movement)
                cpShapeSetSensor(sensor, true);
                
                // Position the shape
                cpBodySetPosition(static_body, cpv(
                    px + maze->cell_size / 2.0f,
                    py + maze->cell_size / 2.0f
                ));
            }
        }
    }
}

// Break a wall in the maze
void maze_break_wall(Maze* maze, int x, int y) {
    // Check bounds
    if (x < 0 || x >= maze->width || y < 0 || y >= maze->height) {
        return;
    }
    
    // Only breakable walls can be broken
    if (maze->cells[x][y] == CELL_BREAKABLE) {
        maze->cells[x][y] = CELL_EMPTY;
        
        // TODO: Remove physics body for this wall
    }
}

// Update maze state
void maze_update(Maze* maze, float dt) {
    // Currently no dynamic updates needed
    (void)dt;
    (void)maze;
}

// Find path to exit using A* algorithm
void maze_get_path_to_exit(Maze* maze, int start_x, int start_y, int** path, int* path_length) {
    // TODO: Implement A* pathfinding algorithm
    // This is a placeholder that would need to be implemented for AI behavior
    
    // Just to avoid compiler warnings
    (void)maze;
    (void)start_x;
    (void)start_y;
    *path = NULL;
    *path_length = 0;
}

// Helper: Carve passages using recursive backtracking
static void carve_passages_from(Maze* maze, int cx, int cy, unsigned int* seed) {
    // Mark current cell as empty
    maze->cells[cx][cy] = CELL_EMPTY;
    
    // Shuffle directions
    int directions[4] = {DIR_NORTH, DIR_EAST, DIR_SOUTH, DIR_WEST};
    shuffle_directions(directions, seed);
    
    // Try each direction
    for (int i = 0; i < 4; i++) {
        int nx = cx + DIR_X[directions[i]] * 2;
        int ny = cy + DIR_Y[directions[i]] * 2;
        
        // Check if new position is valid
        if (nx >= 0 && nx < maze->width && ny >= 0 && ny < maze->height 
            && maze->cells[nx][ny] == CELL_WALL) {
            
            // Carve passage by making intermediate cell empty
            maze->cells[cx + DIR_X[directions[i]]][cy + DIR_Y[directions[i]]] = CELL_EMPTY;
            
            // Recursively carve from new position
            carve_passages_from(maze, nx, ny, seed);
        }
    }
}

// Helper: Generate next random number
static unsigned int random_next(unsigned int* seed) {
    *seed = (*seed * 1103515245 + 12345) & 0x7fffffff;
    return *seed;
}

// Helper: Shuffle array of directions
static void shuffle_directions(int directions[4], unsigned int* seed) {
    for (int i = 3; i > 0; i--) {
        int j = random_next(seed) % (i + 1);
        int temp = directions[i];
        directions[i] = directions[j];
        directions[j] = temp;
    }
}
