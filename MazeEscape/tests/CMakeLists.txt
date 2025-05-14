cmake_minimum_required(VERSION 3.10)

# Test executable
add_executable(maze_escape_tests 
    test_main.c
)

# Link against the project sources
target_link_libraries(maze_escape_tests
    ${SDL2_LIBRARIES}
    chipmunk
)

# Include directories
target_include_directories(maze_escape_tests PRIVATE
    ${CMAKE_SOURCE_DIR}/include
)

# Add test
add_test(NAME maze_escape_tests COMMAND maze_escape_tests)
