#ifndef PHYSICS_H
#define PHYSICS_H

#include "chipmunk/chipmunk.h"

// Collision types
typedef enum {
    COLLISION_WALL = 1,
    COLLISION_CHARACTER = 2,
    COLLISION_EXIT = 3,
    COLLISION_BREAKABLE_WALL = 4
} CollisionType;

// Physics settings
typedef struct {
    float gravity_x;
    float gravity_y;
    int iterations;
    float damping;
    float friction;
    float elasticity;
} PhysicsSettings;

// Function declarations
cpSpace* physics_create_space(float gravity_x, float gravity_y);
void physics_destroy_space(cpSpace* space);
void physics_update(cpSpace* space, float dt);
cpBody* physics_create_static_body(cpSpace* space);
cpBody* physics_create_dynamic_body(cpSpace* space, float mass, float moment, float x, float y);
cpShape* physics_add_box(cpSpace* space, cpBody* body, float width, float height, float friction, CollisionType type);
cpShape* physics_add_circle(cpSpace* space, cpBody* body, float radius, float friction, CollisionType type);
void physics_apply_impulse(cpBody* body, float impulse_x, float impulse_y);
void physics_apply_force(cpBody* body, float force_x, float force_y);

// Collision handlers
void physics_register_collision_handlers(cpSpace* space);
int physics_collision_character_wall(cpArbiter* arb, cpSpace* space, void* data);
int physics_collision_character_exit(cpArbiter* arb, cpSpace* space, void* data);
int physics_collision_character_breakable(cpArbiter* arb, cpSpace* space, void* data);

#endif // PHYSICS_H
