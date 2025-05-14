#include "characters/character.h"
#include "physics/physics.h"
#include <stdlib.h>
#include <string.h>
#include <math.h>

// Forward declarations for ability functions
static void runner_ability(Character* self, Maze* maze);
static void smasher_ability(Character* self, Maze* maze);
static void climber_ability(Character* self, Maze* maze);
static void teleporter_ability(Character* self, Maze* maze);

// Base character creation function
Character* character_create(CharacterType type, const char* name, float x, float y) {
    Character* character = (Character*)malloc(sizeof(Character));
    if (!character) return NULL;
    
    // Initialize basic properties
    character->type = type;
    character->name = strdup(name);
    character->x = x;
    character->y = y;
    character->speed = 200.0f;
    character->size = 20.0f;
    character->cooldown = 5.0f;
    character->ability_cooldown_remaining = 0.0f;
    character->has_escaped = false;
    character->escape_time = 0.0f;
    character->state = STATE_IDLE;
    character->current_cell_x = (int)(x / 40.0f); // Assuming cell_size = 40
    character->current_cell_y = (int)(y / 40.0f);
    character->angle = 0.0f;
    character->animation_frame = 0.0f;
    character->sprite_index = 0;
    
    // Set default functions
    character->use_ability = NULL;
    character->update = NULL;
    character->render = NULL;
    
    // Physics body and shape will be initialized by specific character types
    character->body = NULL;
    character->shape = NULL;
    
    return character;
}

// Destroy a character
void character_destroy(Character* character) {
    if (character) {
        free(character->name);
        // Physics bodies and shapes are automatically cleaned up by the space
        free(character);
    }
}

// Basic character update function
void character_update(Character* character, Maze* maze, float dt) {
    // Update position from physics body
    if (character->body) {
        cpVect pos = cpBodyGetPosition(character->body);
        character->x = pos.x;
        character->y = pos.y;
    }
    
    // Update current cell position
    character->current_cell_x = (int)(character->x / maze->cell_size);
    character->current_cell_y = (int)(character->y / maze->cell_size);
    
    // Update ability cooldown
    if (character->ability_cooldown_remaining > 0) {
        character->ability_cooldown_remaining -= dt;
        if (character->ability_cooldown_remaining < 0) {
            character->ability_cooldown_remaining = 0;
        }
    }
    
    // Update animation frame
    character->animation_frame += dt * 10.0f;
    if (character->animation_frame >= 4.0f) {
        character->animation_frame = 0.0f;
    }
    
    // Update sprite index based on state
    character->sprite_index = (int)character->animation_frame;
    
    // Update angle based on velocity
    if (character->body) {
        cpVect vel = cpBodyGetVelocity(character->body);
        if (cpvlength(vel) > 10.0f) {
            character->angle = atan2(vel.y, vel.x);
            character->state = STATE_MOVING;
        } else {
            character->state = STATE_IDLE;
        }
    }
    
    // Call type-specific update function if available
    if (character->update) {
        character->update(character, maze, dt);
    } else {
        // Default AI behavior: move randomly
        if (rand() % 30 == 0) {  // Occasionally change direction
            float force_x = ((rand() % 200) - 100) * 5.0f;
            float force_y = ((rand() % 200) - 100) * 5.0f;
            character_apply_force(character, force_x, force_y);
        }
    }
}

// Apply force to character
void character_apply_force(Character* character, float force_x, float force_y) {
    if (character->body) {
        physics_apply_force(character->body, force_x, force_y);
    }
}

// Use character's special ability
void character_use_ability(Character* character, Maze* maze) {
    // Check if ability is on cooldown
    if (character->ability_cooldown_remaining > 0) {
        return;
    }
    
    // Call type-specific ability function
    if (character->use_ability) {
        character->use_ability(character, maze);
        character->ability_cooldown_remaining = character->cooldown;
        character->state = STATE_USING_ABILITY;
    }
}

// Check if character has reached the exit
void character_check_escaped(Character* character, Maze* maze) {
    if (character->has_escaped) {
        return;
    }
    
    // Check if character is at the exit
    if (character->current_cell_x == maze->exit_x && 
        character->current_cell_y == maze->exit_y) {
        character->has_escaped = true;
        character->escape_time = 0.0f; // TODO: track global time
        character->state = STATE_ESCAPED;
    }
}

// Create a Runner character
Character* runner_create(const char* name, float x, float y) {
    // Create base character
    Character* runner = character_create(CHARACTER_RUNNER, name, x, y);
    
    // Set runner-specific properties
    runner->speed = 300.0f;  // Faster than other types
    runner->cooldown = 3.0f;
      // Create physics body
    if (runner->body == NULL && runner->shape == NULL && maze->physics_space) {
        cpSpace* space = maze->physics_space;
        
        // Create dynamic body
        runner->body = physics_create_dynamic_body(
            space, 
            10.0f,                  // mass
            cpMomentForCircle(10.0f, 0, runner->size, cpvzero), // moment
            x, y                    // position
        );
        
        // Create circle shape
        runner->shape = physics_add_circle(
            space, 
            runner->body, 
            runner->size,          // radius
            0.7f,                  // friction
            COLLISION_CHARACTER    // collision type
        );
        
        // Store character pointer in shape for collision callbacks
        cpShapeSetUserData(runner->shape, runner);
    }
    
    // Runner's special ability: temporary speed boost
    runner->use_ability = runner_ability;
    
    return runner;
}

// Create a Smasher character
Character* smasher_create(const char* name, float x, float y) {
    // Create base character
    Character* smasher = character_create(CHARACTER_SMASHER, name, x, y);
    
    // Set smasher-specific properties
    smasher->speed = 200.0f;
    smasher->size = 25.0f;  // Larger than other types
    smasher->cooldown = 5.0f;
      // Create physics body
    if (smasher->body == NULL && smasher->shape == NULL && maze->physics_space) {
        cpSpace* space = maze->physics_space;
        
        // Create dynamic body - smasher is heavier
        smasher->body = physics_create_dynamic_body(
            space, 
            20.0f,                  // mass (heavier than others)
            cpMomentForCircle(20.0f, 0, smasher->size, cpvzero), // moment
            x, y                    // position
        );
        
        // Create circle shape
        smasher->shape = physics_add_circle(
            space, 
            smasher->body, 
            smasher->size,          // radius
            0.8f,                   // friction (more grippy)
            COLLISION_CHARACTER     // collision type
        );
        
        // Store character pointer in shape for collision callbacks
        cpShapeSetUserData(smasher->shape, smasher);
    }
    
    // Smasher's special ability: break walls
    smasher->use_ability = smasher_ability;
    
    return smasher;
}

// Create a Climber character
Character* climber_create(const char* name, float x, float y) {
    // Create base character
    Character* climber = character_create(CHARACTER_CLIMBER, name, x, y);
    
    // Set climber-specific properties
    climber->speed = 180.0f;  // Slower than runner
    climber->cooldown = 8.0f;
      // Create physics body
    if (climber->body == NULL && climber->shape == NULL && maze->physics_space) {
        cpSpace* space = maze->physics_space;
        
        // Create dynamic body - climber is lighter
        climber->body = physics_create_dynamic_body(
            space, 
            8.0f,                   // mass (lighter than others)
            cpMomentForCircle(8.0f, 0, climber->size, cpvzero), // moment
            x, y                    // position
        );
        
        // Create circle shape
        climber->shape = physics_add_circle(
            space, 
            climber->body, 
            climber->size,          // radius
            0.6f,                   // friction
            COLLISION_CHARACTER     // collision type
        );
        
        // Store character pointer in shape for collision callbacks
        cpShapeSetUserData(climber->shape, climber);
    }
    
    // Climber's special ability: climb over walls
    climber->use_ability = climber_ability;
    
    return climber;
}

// Create a Teleporter character
Character* teleporter_create(const char* name, float x, float y) {
    // Create base character
    Character* teleporter = character_create(CHARACTER_TELEPORTER, name, x, y);
    
    // Set teleporter-specific properties
    teleporter->speed = 150.0f;  // Slower than others
    teleporter->cooldown = 10.0f;
      // Create physics body
    if (teleporter->body == NULL && teleporter->shape == NULL && maze->physics_space) {
        cpSpace* space = maze->physics_space;
        
        // Create dynamic body
        teleporter->body = physics_create_dynamic_body(
            space, 
            7.0f,                   // mass (lightest)
            cpMomentForCircle(7.0f, 0, teleporter->size, cpvzero), // moment
            x, y                    // position
        );
        
        // Create circle shape
        teleporter->shape = physics_add_circle(
            space, 
            teleporter->body, 
            teleporter->size,       // radius
            0.5f,                   // friction (less grippy)
            COLLISION_CHARACTER     // collision type
        );
        
        // Store character pointer in shape for collision callbacks
        cpShapeSetUserData(teleporter->shape, teleporter);
    }
    
    // Teleporter's special ability: teleport a short distance
    teleporter->use_ability = teleporter_ability;
    
    return teleporter;
}

// Runner ability implementation
static void runner_ability(Character* self, Maze* maze) {
    // Double speed for a short time
    physics_apply_impulse(self->body, 
        cos(self->angle) * self->speed * 5.0f,
        sin(self->angle) * self->speed * 5.0f);
        
    // We don't need to use the maze parameter in this ability
    (void)maze;
}

// Smasher ability implementation
static void smasher_ability(Character* self, Maze* maze) {
    // Find direction character is facing
    int dx = (int)cos(self->angle);
    int dy = (int)sin(self->angle);
    
    // Check if there's a breakable wall in that direction
    int target_x = self->current_cell_x + dx;
    int target_y = self->current_cell_y + dy;
    
    if (maze_get_cell(maze, target_x, target_y) == CELL_BREAKABLE) {
        // Break the wall
        maze_break_wall(maze, target_x, target_y);
        
        // Apply recoil
        physics_apply_impulse(self->body, -dx * 1000.0f, -dy * 1000.0f);
    }
}

// Climber ability implementation
static void climber_ability(Character* self, Maze* maze) {
    // Find direction character is facing
    int dx = (int)cos(self->angle);
    int dy = (int)sin(self->angle);
    
    // Check if there's a wall in that direction
    int target_x = self->current_cell_x + dx;
    int target_y = self->current_cell_y + dy;
    
    if (maze_is_wall(maze, target_x, target_y)) {
        // Check if there's an empty space beyond the wall
        int beyond_x = target_x + dx;
        int beyond_y = target_y + dy;
        
        if (!maze_is_wall(maze, beyond_x, beyond_y)) {
            // Teleport to the other side of the wall
            float new_x = (beyond_x + 0.5f) * maze->cell_size;
            float new_y = (beyond_y + 0.5f) * maze->cell_size;
            
            cpBodySetPosition(self->body, cpv(new_x, new_y));
            cpBodySetVelocity(self->body, cpvzero);
            self->x = new_x;
            self->y = new_y;
        }
    }
}

// Teleporter ability implementation
static void teleporter_ability(Character* self, Maze* maze) {
    // Teleport in the direction character is facing
    float distance = 3.0f * maze->cell_size;
    float target_x = self->x + cos(self->angle) * distance;
    float target_y = self->y + sin(self->angle) * distance;
    
    // Convert to cell coordinates
    int cell_x = (int)(target_x / maze->cell_size);
    int cell_y = (int)(target_y / maze->cell_size);
    
    // Make sure the target is within bounds and not a wall
    if (cell_x >= 0 && cell_x < maze->width && 
        cell_y >= 0 && cell_y < maze->height &&
        !maze_is_wall(maze, cell_x, cell_y)) {
            
        // Adjust to center of cell
        target_x = (cell_x + 0.5f) * maze->cell_size;
        target_y = (cell_y + 0.5f) * maze->cell_size;
        
        // Teleport
        cpBodySetPosition(self->body, cpv(target_x, target_y));
        cpBodySetVelocity(self->body, cpvzero);
        self->x = target_x;
        self->y = target_y;
    }
}
