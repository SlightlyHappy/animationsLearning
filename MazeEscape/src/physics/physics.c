#include "physics/physics.h"
#include "characters/character.h"
#include "maze/maze.h"
#include <stdlib.h>

// Create a new physics space
cpSpace* physics_create_space(float gravity_x, float gravity_y) {
    cpSpace* space = cpSpaceNew();
    
    // Set gravity
    cpSpaceSetGravity(space, cpv(gravity_x, gravity_y));
    
    // Set iterations for more accurate simulation
    cpSpaceSetIterations(space, 10);
    
    // Set damping to simulate air resistance
    cpSpaceSetDamping(space, 0.9);
    
    return space;
}

// Destroy a physics space
void physics_destroy_space(cpSpace* space) {
    cpSpaceFree(space);
}

// Update physics simulation
void physics_update(cpSpace* space, float dt) {
    // Update physics with fixed time step
    cpSpaceStep(space, dt);
}

// Create a static body
cpBody* physics_create_static_body(cpSpace* space) {
    cpBody* body = cpSpaceGetStaticBody(space);
    return body;
}

// Create a dynamic body
cpBody* physics_create_dynamic_body(cpSpace* space, float mass, float moment, float x, float y) {
    cpBody* body = cpBodyNew(mass, moment);
    cpBodySetPosition(body, cpv(x, y));
    cpSpaceAddBody(space, body);
    return body;
}

// Add a box shape to a body
cpShape* physics_add_box(cpSpace* space, cpBody* body, float width, float height, float friction, CollisionType type) {
    cpFloat hw = width / 2.0f;
    cpFloat hh = height / 2.0f;
    
    // Create box shape
    cpShape* shape = cpBoxShapeNew(body, width, height, 0);
    
    // Set shape properties
    cpShapeSetFriction(shape, friction);
    cpShapeSetElasticity(shape, 0.1);
    
    // Set collision type
    cpShapeSetCollisionType(shape, (cpCollisionType)type);
    
    // Add shape to space
    cpSpaceAddShape(space, shape);
    
    return shape;
}

// Add a circle shape to a body
cpShape* physics_add_circle(cpSpace* space, cpBody* body, float radius, float friction, CollisionType type) {
    // Create circle shape
    cpShape* shape = cpCircleShapeNew(body, radius, cpvzero);
    
    // Set shape properties
    cpShapeSetFriction(shape, friction);
    cpShapeSetElasticity(shape, 0.2);
    
    // Set collision type
    cpShapeSetCollisionType(shape, (cpCollisionType)type);
    
    // Add shape to space
    cpSpaceAddShape(space, shape);
    
    return shape;
}

// Apply impulse to a body
void physics_apply_impulse(cpBody* body, float impulse_x, float impulse_y) {
    cpBodyApplyImpulseAtLocalPoint(body, cpv(impulse_x, impulse_y), cpvzero);
}

// Apply force to a body
void physics_apply_force(cpBody* body, float force_x, float force_y) {
    cpBodyApplyForceAtLocalPoint(body, cpv(force_x, force_y), cpvzero);
}

// Begin collision handler
int physics_begin_collision(cpArbiter* arb, cpSpace* space, void* data) {
    // Get colliding shapes
    CP_ARBITER_GET_SHAPES(arb, shape_a, shape_b);
    
    // Get collision types
    cpCollisionType type_a = cpShapeGetCollisionType(shape_a);
    cpCollisionType type_b = cpShapeGetCollisionType(shape_b);
    
    // Handle character-wall collision
    if ((type_a == COLLISION_CHARACTER && type_b == COLLISION_WALL) ||
        (type_a == COLLISION_WALL && type_b == COLLISION_CHARACTER)) {
        return 1; // Collide
    }
    
    // Handle character-exit collision
    if ((type_a == COLLISION_CHARACTER && type_b == COLLISION_EXIT) ||
        (type_a == COLLISION_EXIT && type_b == COLLISION_CHARACTER)) {
        // Get which shape is the character
        cpShape* char_shape = (type_a == COLLISION_CHARACTER) ? shape_a : shape_b;
        
        // Get character data from shape
        Character* character = (Character*)cpShapeGetUserData(char_shape);
        
        // Mark character as escaped
        if (character) {
            character->has_escaped = true;
        }
        
        return 0; // Don't collide with exit, just trigger
    }
    
    // Handle character-breakable wall collision
    if ((type_a == COLLISION_CHARACTER && type_b == COLLISION_BREAKABLE_WALL) ||
        (type_a == COLLISION_BREAKABLE_WALL && type_b == COLLISION_CHARACTER)) {
        
        // Get which shape is the character
        cpShape* char_shape = (type_a == COLLISION_CHARACTER) ? shape_a : shape_b;
        
        // Get character data from shape
        Character* character = (Character*)cpShapeGetUserData(char_shape);
        
        // Only smasher can break walls by default
        if (character && character->type == CHARACTER_SMASHER) {
            // TODO: Break the wall by getting its position and converting to grid coordinates
            
            // For now, we'll still collide with breakable walls
            return 1;
        }
        
        // Other characters collide with breakable walls
        return 1;
    }
    
    return 1; // Default: allow collision
}

// Register collision handlers
void physics_register_collision_handlers(cpSpace* space) {
    // Character-wall collision
    cpCollisionHandler* handler_char_wall = cpSpaceAddCollisionHandler(
        space, 
        COLLISION_CHARACTER, 
        COLLISION_WALL
    );
    handler_char_wall->beginFunc = physics_begin_collision;
    
    // Character-exit collision
    cpCollisionHandler* handler_char_exit = cpSpaceAddCollisionHandler(
        space, 
        COLLISION_CHARACTER, 
        COLLISION_EXIT
    );
    handler_char_exit->beginFunc = physics_begin_collision;
    
    // Character-breakable wall collision
    cpCollisionHandler* handler_char_breakable = cpSpaceAddCollisionHandler(
        space, 
        COLLISION_CHARACTER, 
        COLLISION_BREAKABLE_WALL
    );
    handler_char_breakable->beginFunc = physics_begin_collision;
}
