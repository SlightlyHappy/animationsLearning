# Script to setup MazeEscape project

# Create build directory
mkdir -p build

# Download and setup Chipmunk2D physics library
git clone https://github.com/slembcke/Chipmunk2D.git external/chipmunk
cd external/chipmunk
git checkout master
cd ../..

# Get SDL2 (for Windows we'd typically download prebuilt libraries)
# This is a simplified approach - in a real project you would handle different platforms

# Configure and build
cd build
cmake ..
cmake --build . --config Release

# Run the application
cd ..
./build/maze_escape
