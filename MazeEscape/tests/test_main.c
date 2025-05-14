#include <stdio.h>
#include "maze_escape.h"

// Simple test to verify maze generation
void test_maze_generation() {
    printf("Testing maze generation...\n");
    
    Maze* maze = maze_create(20, 20, 40);
    maze_generate(maze, 12345);  // Use fixed seed for reproducible tests
    
    // Check that exit is properly placed
    printf("Exit position: (%d, %d)\n", maze->exit_x, maze->exit_y);
    if (maze->cells[maze->exit_x][maze->exit_y] != CELL_EXIT) {
        printf("FAIL: Exit cell not properly marked\n");
    } else {
        printf("PASS: Exit cell properly marked\n");
    }
    
    // Count different cell types
    int wall_count = 0;
    int empty_count = 0;
    int breakable_count = 0;
    int special_count = 0;
    
    for (int x = 0; x < maze->width; x++) {
        for (int y = 0; y < maze->height; y++) {
            switch (maze->cells[x][y]) {
                case CELL_WALL:
                    wall_count++;
                    break;
                case CELL_EMPTY:
                    empty_count++;
                    break;
                case CELL_BREAKABLE:
                    breakable_count++;
                    break;
                case CELL_SPECIAL:
                    special_count++;
                    break;
                default:
                    break;
            }
        }
    }
    
    printf("Cell counts: Wall=%d, Empty=%d, Breakable=%d, Special=%d\n",
        wall_count, empty_count, breakable_count, special_count);
    
    // Check that we have a reasonable distribution of cells
    if (empty_count < 100) {
        printf("FAIL: Not enough empty cells\n");
    } else {
        printf("PASS: Sufficient empty cells\n");
    }
    
    if (breakable_count < 5) {
        printf("FAIL: Not enough breakable walls\n");
    } else {
        printf("PASS: Sufficient breakable walls\n");
    }
    
    maze_destroy(maze);
    printf("Maze generation test complete\n\n");
}

// Test character creation and basic properties
void test_character_creation() {
    printf("Testing character creation...\n");
    
    Character* runner = runner_create("TestRunner", 100, 100);
    Character* smasher = smasher_create("TestSmasher", 200, 200);
    Character* climber = climber_create("TestClimber", 300, 300);
    Character* teleporter = teleporter_create("TestTeleporter", 400, 400);
    
    // Check types
    printf("Runner type: %d (expected %d)\n", runner->type, CHARACTER_RUNNER);
    printf("Smasher type: %d (expected %d)\n", smasher->type, CHARACTER_SMASHER);
    printf("Climber type: %d (expected %d)\n", climber->type, CHARACTER_CLIMBER);
    printf("Teleporter type: %d (expected %d)\n", teleporter->type, CHARACTER_TELEPORTER);
    
    // Check positions
    printf("Runner position: (%.1f, %.1f)\n", runner->x, runner->y);
    printf("Smasher position: (%.1f, %.1f)\n", smasher->x, smasher->y);
    printf("Climber position: (%.1f, %.1f)\n", climber->x, climber->y);
    printf("Teleporter position: (%.1f, %.1f)\n", teleporter->x, teleporter->y);
    
    // Check abilities
    if (runner->use_ability == NULL) {
        printf("FAIL: Runner has no ability\n");
    } else {
        printf("PASS: Runner has an ability\n");
    }
    
    if (smasher->use_ability == NULL) {
        printf("FAIL: Smasher has no ability\n");
    } else {
        printf("PASS: Smasher has an ability\n");
    }
    
    if (climber->use_ability == NULL) {
        printf("FAIL: Climber has no ability\n");
    } else {
        printf("PASS: Climber has an ability\n");
    }
    
    if (teleporter->use_ability == NULL) {
        printf("FAIL: Teleporter has no ability\n");
    } else {
        printf("PASS: Teleporter has an ability\n");
    }
    
    // Clean up
    character_destroy(runner);
    character_destroy(smasher);
    character_destroy(climber);
    character_destroy(teleporter);
    
    printf("Character creation test complete\n\n");
}

// Main test function
int main() {
    printf("Running MazeEscape tests...\n\n");
    
    test_maze_generation();
    test_character_creation();
    
    printf("All tests complete!\n");
    return 0;
}
