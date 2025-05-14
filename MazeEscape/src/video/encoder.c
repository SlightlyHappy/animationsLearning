#include "video/encoder.h"
#include <stdlib.h>
#include <string.h>

// FFmpeg context structure
typedef struct {
    int frame_count;
    float duration;
    char* cmd;
    FILE* pipe;
    SDL_Surface* temp_surface;
} FFmpegContext;

// Create a new video encoder
VideoEncoder* encoder_create(const char* filename, int width, int height, int fps, int bitrate) {
    VideoEncoder* encoder = (VideoEncoder*)malloc(sizeof(VideoEncoder));
    if (!encoder) return NULL;
    
    // Initialize encoder properties
    encoder->output_filename = strdup(filename);
    encoder->width = width;
    encoder->height = height;
    encoder->framerate = fps;
    encoder->bitrate = bitrate;
    encoder->recording = false;
    
    // Initialize FFmpeg context
    FFmpegContext* ctx = (FFmpegContext*)malloc(sizeof(FFmpegContext));
    ctx->frame_count = 0;
    ctx->duration = 0.0f;
    ctx->cmd = NULL;
    ctx->pipe = NULL;
    ctx->temp_surface = NULL;
    encoder->ffmpeg_context = ctx;
    
    return encoder;
}

// Destroy a video encoder
void encoder_destroy(VideoEncoder* encoder) {
    if (!encoder) return;
    
    // Stop recording if active
    if (encoder->recording) {
        encoder_stop(encoder);
    }
    
    // Clean up FFmpeg context
    if (encoder->ffmpeg_context) {
        FFmpegContext* ctx = (FFmpegContext*)encoder->ffmpeg_context;
        if (ctx->cmd) free(ctx->cmd);
        if (ctx->temp_surface) SDL_FreeSurface(ctx->temp_surface);
        free(ctx);
    }
    
    // Free encoder resources
    free(encoder->output_filename);
    free(encoder);
}

// Start recording
bool encoder_start(VideoEncoder* encoder) {
    if (!encoder || encoder->recording) return false;
    
    FFmpegContext* ctx = (FFmpegContext*)encoder->ffmpeg_context;
    
    // Build FFmpeg command
    char cmd[1024];
    snprintf(cmd, sizeof(cmd), 
        "ffmpeg -y -f rawvideo -pix_fmt bgr24 -s %dx%d -r %d "
        "-i - -c:v libx264 -preset fast -crf 22 -pix_fmt yuv420p "
        "-b:v %d \"%s\"",
        encoder->width, encoder->height, encoder->framerate,
        encoder->bitrate, encoder->output_filename
    );
    
    ctx->cmd = strdup(cmd);
    
    // Open pipe to FFmpeg
#ifdef _WIN32
    ctx->pipe = _popen(cmd, "wb");
#else
    ctx->pipe = popen(cmd, "w");
#endif

    if (!ctx->pipe) {
        fprintf(stderr, "Error opening FFmpeg pipe\n");
        return false;
    }
    
    // Create temporary surface for pixel data
    ctx->temp_surface = SDL_CreateRGBSurface(
        0, encoder->width, encoder->height, 24,
        0x0000FF, 0x00FF00, 0xFF0000, 0
    );
    
    if (!ctx->temp_surface) {
        fprintf(stderr, "Error creating temporary surface: %s\n", SDL_GetError());
#ifdef _WIN32
        _pclose(ctx->pipe);
#else
        pclose(ctx->pipe);
#endif
        ctx->pipe = NULL;
        return false;
    }
    
    // Reset frame count and duration
    ctx->frame_count = 0;
    ctx->duration = 0.0f;
    
    encoder->recording = true;
    return true;
}

// Encode a frame from a surface
bool encoder_encode_frame(VideoEncoder* encoder, SDL_Surface* surface) {
    if (!encoder || !encoder->recording || !surface) return false;
    
    FFmpegContext* ctx = (FFmpegContext*)encoder->ffmpeg_context;
    if (!ctx->pipe || !ctx->temp_surface) return false;
    
    // Convert surface to correct format if needed
    SDL_BlitSurface(surface, NULL, ctx->temp_surface, NULL);
    
    // Write pixel data to FFmpeg pipe
    fwrite(ctx->temp_surface->pixels, 
        encoder->width * encoder->height * 3, 1, 
        ctx->pipe);
    
    // Increment frame count
    ctx->frame_count++;
    ctx->duration = (float)ctx->frame_count / encoder->framerate;
    
    return true;
}

// Encode a frame from a renderer
bool encoder_encode_renderer(VideoEncoder* encoder, SDL_Renderer* renderer) {
    if (!encoder || !encoder->recording || !renderer) return false;
    
    FFmpegContext* ctx = (FFmpegContext*)encoder->ffmpeg_context;
    if (!ctx->pipe || !ctx->temp_surface) return false;
    
    // Create a temporary texture to read pixels from the renderer
    SDL_Texture* texture = SDL_CreateTexture(
        renderer,
        SDL_PIXELFORMAT_RGB24,
        SDL_TEXTUREACCESS_TARGET,
        encoder->width, encoder->height
    );
    
    if (!texture) {
        fprintf(stderr, "Error creating temporary texture: %s\n", SDL_GetError());
        return false;
    }
    
    // Copy renderer to texture
    SDL_SetRenderTarget(renderer, texture);
    SDL_RenderReadPixels(
        renderer, NULL,
        SDL_PIXELFORMAT_RGB24,
        ctx->temp_surface->pixels,
        ctx->temp_surface->pitch
    );
    SDL_SetRenderTarget(renderer, NULL);
    
    // Write pixel data to FFmpeg pipe
    fwrite(
        ctx->temp_surface->pixels,
        encoder->width * encoder->height * 3,
        1, ctx->pipe
    );
    
    // Clean up
    SDL_DestroyTexture(texture);
    
    // Increment frame count
    ctx->frame_count++;
    ctx->duration = (float)ctx->frame_count / encoder->framerate;
    
    return true;
}

// Stop recording
void encoder_stop(VideoEncoder* encoder) {
    if (!encoder || !encoder->recording) return;
    
    FFmpegContext* ctx = (FFmpegContext*)encoder->ffmpeg_context;
    if (ctx->pipe) {
#ifdef _WIN32
        _pclose(ctx->pipe);
#else
        pclose(ctx->pipe);
#endif
        ctx->pipe = NULL;
    }
    
    encoder->recording = false;
    
    printf("Video saved to %s (duration: %.2f seconds)\n", 
        encoder->output_filename, ctx->duration);
}

// Check if recording is active
bool encoder_is_recording(VideoEncoder* encoder) {
    return encoder && encoder->recording;
}

// Get current recording duration
float encoder_get_duration(VideoEncoder* encoder) {
    if (!encoder) return 0.0f;
    
    FFmpegContext* ctx = (FFmpegContext*)encoder->ffmpeg_context;
    return ctx->duration;
}

// Add text overlay (stub implementation)
void encoder_add_text_overlay(VideoEncoder* encoder, const char* text, int x, int y, float duration) {
    // This would be implemented using FFmpeg filters in a real implementation
    (void)encoder;
    (void)text;
    (void)x;
    (void)y;
    (void)duration;
}

// Add transition effect (stub implementation)
void encoder_add_transition_effect(VideoEncoder* encoder, const char* effect_name) {
    // This would be implemented using FFmpeg filters in a real implementation
    (void)encoder;
    (void)effect_name;
}
