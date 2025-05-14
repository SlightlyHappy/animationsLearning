#ifndef VIDEO_ENCODER_H
#define VIDEO_ENCODER_H

#include <SDL.h>
#include <stdbool.h>

// Video encoder settings
typedef struct {
    char* output_filename;
    int width;
    int height;
    int framerate;
    int bitrate;
    bool recording;
    void* ffmpeg_context; // Abstract context for FFmpeg integration
} VideoEncoder;

// Function declarations
VideoEncoder* encoder_create(const char* filename, int width, int height, int fps, int bitrate);
void encoder_destroy(VideoEncoder* encoder);
bool encoder_start(VideoEncoder* encoder);
bool encoder_encode_frame(VideoEncoder* encoder, SDL_Surface* surface);
bool encoder_encode_renderer(VideoEncoder* encoder, SDL_Renderer* renderer);
void encoder_stop(VideoEncoder* encoder);
bool encoder_is_recording(VideoEncoder* encoder);
float encoder_get_duration(VideoEncoder* encoder);
void encoder_add_text_overlay(VideoEncoder* encoder, const char* text, int x, int y, float duration);
void encoder_add_transition_effect(VideoEncoder* encoder, const char* effect_name);

#endif // VIDEO_ENCODER_H
