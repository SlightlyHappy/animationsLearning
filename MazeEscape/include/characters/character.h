#ifndef CHARACTER_H
#define CHARACTER_H

#include <stdbool.h>
#include "chipmunk/chipmunk.h"
#include "../maze/maze.h"

// Character types
typedef enum {
    CHARACTER_RUNNER = 0,
    CHARACTER_SMASHER = 1,
    CHARACTER_CLIMBER = 2,
    CHARACTER_TELEPORTER = 3
} CharacterType;

// Character state
typedef enum {
    STATE_IDLE,
    STATE_MOVING,
    STATE_USING_ABILITY,
    STATE_STUCK,
    STATE_ESCAPED,
    STATE_CELEBRATING
} CharacterState;

// Character structure
typedef struct Character {
    CharacterType type;
    char* name;
    float x;
    float y;
    float speed;
    float size;
    float cooldown;
    float ability_cooldown_remaining;
    int current_cell_x;
    int current_cell_y;
    bool has_escaped;
    float escape_time;
    CharacterState state;
    
    // Physics
    cpBody* body;
    cpShape* shape;
    
    // Animation properties
    float angle;
    float animation_frame;
    int sprite_index;
    
    // Special abilities
    void (*use_ability)(struct Character* self, Maze* maze);
    void (*update)(struct Character* self, Maze* maze, float dt);
    void (*render)(struct Character* self, void* renderer);
} Character;

// Function declarations
Character* character_create(CharacterType type, const char* name, float x, float y);
void character_destroy(Character* character);
void character_update(Character* character, Maze* maze, float dt);
void character_render(Character* character, void* renderer);
void character_apply_force(Character* character, float force_x, float force_y);
void character_use_ability(Character* character, Maze* maze);
void character_check_escaped(Character* character, Maze* maze);

// Character type-specific functions
Character* runner_create(const char* name, float x, float y);
Character* smasher_create(const char* name, float x, float y);
Character* climber_create(const char* name, float x, float y);
Character* teleporter_create(const char* name, float x, float y);

#endif // CHARACTER_H
