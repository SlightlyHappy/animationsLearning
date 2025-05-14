#include <stdio.h>
#include <stdlib.h>
#include <time.h>
#include <stdbool.h>

// Cell types
typedef enum {
    CELL_EMPTY = 0,
    CELL_WALL = 1,
    CELL_START = 2,
    CELL_EXIT = 3,
    CELL_BREAKABLE = 4,
    CELL_SPECIAL = 5
} CellType;

// Simplified maze structure without dependencies
typedef struct {
    int width;
    int height;
    CellType** cells;
    int* start_positions;
    int exit_x;
    int exit_y;
} Maze;

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

// Function prototypes
Maze* maze_create(int width, int height);
void maze_generate(Maze* maze, unsigned int seed);
void maze_print(Maze* maze);
void maze_destroy(Maze* maze);
static void carve_passages_from(Maze* maze, int cx, int cy, unsigned int* seed);
static unsigned int random_next(unsigned int* seed);
static void shuffle_directions(int directions[4], unsigned int* seed);

// Create a new maze
Maze* maze_create(int width, int height) {
    Maze* maze = (Maze*)malloc(sizeof(Maze));
    if (!maze) return NULL;
    
    // Initialize maze properties
    maze->width = width;
    maze->height = height;
    
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

// Print the maze to the console
void maze_print(Maze* maze) {
    for (int y = 0; y < maze->height; y++) {
        for (int x = 0; x < maze->width; x++) {
            switch (maze->cells[x][y]) {
                case CELL_EMPTY:
                    printf(" ");  // Empty cell
                    break;
                case CELL_WALL:
                    printf("#");  // Wall
                    break;
                case CELL_START:
                    printf("S");  // Start
                    break;
                case CELL_EXIT:
                    printf("E");  // Exit
                    break;
                case CELL_BREAKABLE:
                    printf("B");  // Breakable wall
                    break;
                case CELL_SPECIAL:
                    printf("*");  // Special cell
                    break;
                default:
                    printf("?");  // Unknown
                    break;
            }
        }
        printf("\n");
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

// Count different cell types
void maze_count_cells(Maze* maze, int* wall_count, int* empty_count, int* breakable_count, int* special_count) {
    *wall_count = 0;
    *empty_count = 0;
    *breakable_count = 0;
    *special_count = 0;
    
    for (int x = 0; x < maze->width; x++) {
        for (int y = 0; y < maze->height; y++) {
            switch (maze->cells[x][y]) {
                case CELL_WALL:
                    (*wall_count)++;
                    break;
                case CELL_EMPTY:
                    (*empty_count)++;
                    break;
                case CELL_BREAKABLE:
                    (*breakable_count)++;
                    break;
                case CELL_SPECIAL:
                    (*special_count)++;
                    break;
                default:
                    break;
            }
        }
    }
}

// Main function
int main() {
    // Create a small maze
    int width = 20;
    int height = 10;
    unsigned int seed = 12345; // Fixed seed for reproducible results
    
    printf("Generating a %dx%d maze with seed %u\n", width, height, seed);
    
    // Create and generate maze
    Maze* maze = maze_create(width, height);
    maze_generate(maze, seed);
    
    // Print maze
    printf("\nMaze Layout:\n");
    maze_print(maze);
    
    // Count cell types
    int wall_count, empty_count, breakable_count, special_count;
    maze_count_cells(maze, &wall_count, &empty_count, &breakable_count, &special_count);
    
    // Display statistics
    printf("\nMaze Statistics:\n");
    printf("Total cells: %d\n", width * height);
    printf("Walls: %d (%.1f%%)\n", wall_count, 100.0 * wall_count / (width * height));
    printf("Empty: %d (%.1f%%)\n", empty_count, 100.0 * empty_count / (width * height));
    printf("Breakable: %d (%.1f%%)\n", breakable_count, 100.0 * breakable_count / (width * height));
    printf("Special: %d (%.1f%%)\n", special_count, 100.0 * special_count / (width * height));
    
    printf("\nExit position: (%d, %d)\n", maze->exit_x, maze->exit_y);
    printf("Start positions for characters:\n");
    for (int i = 0; i < 4; i++) {
        printf("Character %d: (%d, %d)\n", i+1, maze->start_positions[i*2], maze->start_positions[i*2+1]);
    }
    
    // Free memory
    maze_destroy(maze);
    
    return 0;
}
