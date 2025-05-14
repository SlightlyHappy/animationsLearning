@echo off
REM Script to setup MazeEscape project for Windows

REM Create build directory
if not exist build mkdir build

REM Download and setup Chipmunk2D physics library
if not exist external\chipmunk (
    echo Cloning Chipmunk2D...
    git clone https://github.com/slembcke/Chipmunk2D.git external\chipmunk
    cd external\chipmunk
    git checkout master
    cd ..\..
)

REM For SDL2, download prebuilt binaries
if not exist external\SDL2 (
    echo Downloading SDL2...
    REM In a real scenario, you'd download prebuilt SDL2 binaries
    REM and extract them to external\SDL2
    echo Please download SDL2 development libraries for Windows from:
    echo https://www.libsdl.org/download-2.0.php
    echo Extract to external\SDL2 directory
    pause
)

REM Configure and build
cd build
cmake -G "Visual Studio 16 2019" -A x64 ..
cmake --build . --config Release
cd ..

REM Run the application
echo.
echo Build complete. Run the application with:
echo build\Release\maze_escape.exe
