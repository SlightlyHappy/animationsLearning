#include "maze_escape.h"

// Global variables
AppSettings app_settings = {
    .maze_width = 20,
    .maze_height = 30,
    .cell_size = 40,
    .character_types = "runner,smasher,climber,teleporter",
    .simulation_duration = 30,
    .random_seed = 0,
    .output_filename = "maze_escape.mp4",
    .video_width = 720,
    .video_height = 1280, // 9:16 aspect ratio for TikTok
    .fps = 60,
    .zoom_level = 1.0f,
    .debug_mode = false
};

// Local variables
static Maze* maze = NULL;
static Character** characters = NULL;
static int character_count = 0;
static Renderer* renderer = NULL;
static VideoEncoder* encoder = NULL;
static cpSpace* physics_space = NULL;
static bool simulation_running = true;
static Character* winner = NULL;
static float simulation_time = 0.0f;

// Function to parse command-line arguments
void parse_arguments(int argc, char* argv[]) {
    for (int i = 1; i < argc; i++) {
        if (strcmp(argv[i], "--width") == 0 && i + 1 < argc) {
            app_settings.maze_width = atoi(argv[++i]);
        } else if (strcmp(argv[i], "--height") == 0 && i + 1 < argc) {
            app_settings.maze_height = atoi(argv[++i]);
        } else if (strcmp(argv[i], "--characters") == 0 && i + 1 < argc) {
            app_settings.character_types = argv[++i];
        } else if (strcmp(argv[i], "--duration") == 0 && i + 1 < argc) {
            app_settings.simulation_duration = atoi(argv[++i]);
        } else if (strcmp(argv[i], "--seed") == 0 && i + 1 < argc) {
            app_settings.random_seed = (unsigned int)atoi(argv[++i]);
        } else if (strcmp(argv[i], "--output") == 0 && i + 1 < argc) {
            app_settings.output_filename = argv[++i];
        } else if (strcmp(argv[i], "--debug") == 0) {
            app_settings.debug_mode = true;
        }
    }
    
    // Use current time as seed if not specified
    if (app_settings.random_seed == 0) {
        app_settings.random_seed = (unsigned int)time(NULL);
    }
}

// Function to initialize the simulation
void initialize_simulation(void) {
    // Initialize SDL
    if (SDL_Init(SDL_INIT_VIDEO | SDL_INIT_TIMER) != 0) {
        fprintf(stderr, "Error initializing SDL: %s\n", SDL_GetError());
        exit(EXIT_FAILURE);
    }
    
    // Create physics space
    physics_space = physics_create_space(0.0f, 100.0f); // Low gravity for interesting physics
    
    // Create and generate maze
    maze = maze_create(app_settings.maze_width, app_settings.maze_height, app_settings.cell_size);
    maze_generate(maze, app_settings.random_seed);
    maze->physics_space = physics_space;
    
    // Add physics bodies for maze walls
    maze_add_physics_bodies(maze, physics_space);
    
    // Create characters based on specified types
    char* types_copy = strdup(app_settings.character_types);
    char* token = strtok(types_copy, ",");
    
    // Count how many characters we'll need
    while (token) {
        character_count++;
        token = strtok(NULL, ",");
    }
    
    // Allocate character array
    characters = (Character**)malloc(character_count * sizeof(Character*));
    
    // Reset and create characters
    strcpy(types_copy, app_settings.character_types);
    token = strtok(types_copy, ",");
    
    int char_index = 0;
    while (token && char_index < character_count) {
        // Get start position from maze
        int start_x = maze->start_positions[char_index * 2];
        int start_y = maze->start_positions[char_index * 2 + 1];
        
        // Convert grid coords to pixel coords
        float pixel_x = (start_x + 0.5f) * app_settings.cell_size;
        float pixel_y = (start_y + 0.5f) * app_settings.cell_size;
        
        // Create appropriate character type
        if (strcmp(token, "runner") == 0) {
            characters[char_index] = runner_create("Runner", pixel_x, pixel_y);
        } else if (strcmp(token, "smasher") == 0) {
            characters[char_index] = smasher_create("Smasher", pixel_x, pixel_y);
        } else if (strcmp(token, "climber") == 0) {
            characters[char_index] = climber_create("Climber", pixel_x, pixel_y);
        } else if (strcmp(token, "teleporter") == 0) {
            characters[char_index] = teleporter_create("Teleporter", pixel_x, pixel_y);
        }
        
        char_index++;
        token = strtok(NULL, ",");
    }
    
    free(types_copy);
    
    // Create renderer
    renderer = renderer_create(
        app_settings.video_width, 
        app_settings.video_height, 
        "Maze Escape Simulation"
    );
    renderer_load_textures(renderer);
    
    // Create video encoder
    encoder = encoder_create(
        app_settings.output_filename,
        app_settings.video_width,
        app_settings.video_height,
        app_settings.fps,
        5000000 // 5 Mbps bitrate
    );
    
    // Register collision handlers
    physics_register_collision_handlers(physics_space);
    
    // Start video recording
    encoder_start(encoder);
}

// Function to update simulation
void update_simulation(float dt) {
    // Update physics
    physics_update(physics_space, dt);
    
    // Update maze
    maze_update(maze, dt);
    
    // Update characters
    for (int i = 0; i < character_count; i++) {
        character_update(characters[i], maze, dt);
        
        // Check if character has escaped
        character_check_escaped(characters[i], maze);
        
        // Check for winner
        if (characters[i]->has_escaped && !winner) {
            winner = characters[i];
            printf("Winner: %s escaped in %.2f seconds!\n", winner->name, winner->escape_time);
        }
    }
    
    // Update simulation time
    simulation_time += dt;
    
    // Check if simulation should end
    if (winner || simulation_time >= app_settings.simulation_duration) {
        if (!winner) {
            printf("Simulation ended with no winner after %.2f seconds.\n", simulation_time);
        }
        simulation_running = false;
    }
}

// Function to render simulation
void render_simulation(void) {
    // Clear screen
    Color bg_color = {30, 30, 50, 255}; // Dark blue-ish background
    renderer_clear(renderer, bg_color);
    
    // Set camera to follow characters (average position)
    float avg_x = 0, avg_y = 0;
    int active_chars = 0;
    
    for (int i = 0; i < character_count; i++) {
        if (!characters[i]->has_escaped) {
            avg_x += characters[i]->x;
            avg_y += characters[i]->y;
            active_chars++;
        }
    }
    
    if (active_chars > 0) {
        avg_x /= active_chars;
        avg_y /= active_chars;
        renderer_set_camera(renderer, avg_x, avg_y, app_settings.zoom_level);
    }
    
    // Draw maze
    renderer_draw_maze(renderer, maze);
    
    // Draw characters
    for (int i = 0; i < character_count; i++) {
        renderer_draw_character(renderer, characters[i]);
    }
    
    // Draw particles
    renderer_draw_particles(renderer);
    
    // Draw debug info if enabled
    if (app_settings.debug_mode) {
        renderer_draw_debug_info(renderer, app_settings.fps, character_count);
    }
    
    // Draw celebration if there's a winner
    if (winner) {
        renderer_draw_celebration(renderer, winner);
    }
    
    // Present rendering
    renderer_present(renderer);
    
    // Encode frame to video
    encoder_encode_renderer(encoder, renderer->sdl_renderer);
}

// Function to run the simulation
void run_simulation(void) {
    Uint32 last_time = SDL_GetTicks();
    Uint32 current_time;
    float dt;
    
    // Main simulation loop
    SDL_Event event;
    while (simulation_running) {
        // Handle SDL events
        while (SDL_PollEvent(&event)) {
            if (event.type == SDL_QUIT) {
                simulation_running = false;
            } else if (event.type == SDL_KEYDOWN) {
                if (event.key.keysym.sym == SDLK_ESCAPE) {
                    simulation_running = false;
                }
            }
        }
        
        // Calculate delta time
        current_time = SDL_GetTicks();
        dt = (current_time - last_time) / 1000.0f;
        last_time = current_time;
        
        // Limit dt to prevent physics issues
        if (dt > 0.05f) dt = 0.05f;
        
        // Update simulation
        update_simulation(dt);
        
        // Render simulation
        render_simulation();
        
        // Cap frame rate
        SDL_Delay(1000 / app_settings.fps);
    }
    
    // Render a few more frames of celebration if there's a winner
    if (winner) {
        for (int i = 0; i < 5 * app_settings.fps; i++) { // 5 seconds of celebration
            render_simulation();
            SDL_Delay(1000 / app_settings.fps);
        }
    }
    
    // Stop video recording
    encoder_stop(encoder);
}

// Function to clean up resources
void cleanup_simulation(void) {
    // Clean up characters
    for (int i = 0; i < character_count; i++) {
        character_destroy(characters[i]);
    }
    free(characters);
    
    // Clean up maze
    maze_destroy(maze);
    
    // Clean up physics
    physics_destroy_space(physics_space);
    
    // Clean up renderer
    renderer_destroy(renderer);
    
    // Clean up encoder
    encoder_destroy(encoder);
    
    // Quit SDL
    SDL_Quit();
}

// Main function
int main(int argc, char* argv[]) {
    // Parse command-line arguments
    parse_arguments(argc, argv);
    
    // Initialize simulation
    initialize_simulation();
    
    // Run simulation
    run_simulation();
    
    // Clean up resources
    cleanup_simulation();
    
    return EXIT_SUCCESS;
}
