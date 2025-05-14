cmake_minimum_required(VERSION 3.10)
project(maze_escape C)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -Wall -Wextra")

# Options for finding SDL2 on different platforms
if(WIN32)
    # For Windows, look in our external directory first
    set(SDL2_DIR "${CMAKE_SOURCE_DIR}/external/SDL2")
    if(EXISTS "${SDL2_DIR}")
        set(SDL2_INCLUDE_DIRS "${SDL2_DIR}/include")
        if(CMAKE_SIZEOF_VOID_P EQUAL 8)
            set(SDL2_LIBRARIES "${SDL2_DIR}/lib/x64/SDL2.lib;${SDL2_DIR}/lib/x64/SDL2main.lib")
        else()
            set(SDL2_LIBRARIES "${SDL2_DIR}/lib/x86/SDL2.lib;${SDL2_DIR}/lib/x86/SDL2main.lib")
        endif()
    else()
        # Fall back to find_package
        find_package(SDL2 REQUIRED)
    endif()
else()
    # For other platforms use find_package
    find_package(SDL2 REQUIRED)
endif()

include_directories(${SDL2_INCLUDE_DIRS})

# Source directories
include_directories(
    ${CMAKE_SOURCE_DIR}/include
    ${CMAKE_SOURCE_DIR}/src
)

# External dependencies
include_directories(${CMAKE_SOURCE_DIR}/external/chipmunk/include)
add_subdirectory(${CMAKE_SOURCE_DIR}/external/chipmunk)

# Source files
file(GLOB SOURCES 
    "src/*.c"
    "src/maze/*.c"
    "src/characters/*.c"
    "src/physics/*.c"
    "src/rendering/*.c"
    "src/video/*.c"
)

# Main executable
add_executable(maze_escape ${SOURCES})
target_link_libraries(maze_escape ${SDL2_LIBRARIES} chipmunk)

# Installation
install(TARGETS maze_escape DESTINATION bin)

# Copy resources
if(EXISTS "${CMAKE_SOURCE_DIR}/resources")
    file(COPY ${CMAKE_SOURCE_DIR}/resources DESTINATION ${CMAKE_BINARY_DIR})
endif()

# Copy SDL2 DLLs for Windows
if(WIN32 AND EXISTS "${SDL2_DIR}")
    if(CMAKE_SIZEOF_VOID_P EQUAL 8)
        file(COPY "${SDL2_DIR}/lib/x64/SDL2.dll" DESTINATION ${CMAKE_BINARY_DIR})
    else()
        file(COPY "${SDL2_DIR}/lib/x86/SDL2.dll" DESTINATION ${CMAKE_BINARY_DIR})
    endif()
endif()

# Add tests
enable_testing()
add_subdirectory(tests)
