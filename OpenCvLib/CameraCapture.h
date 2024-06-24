#pragma once

#ifdef CAMERACAPTURE_EXPORTS
#define CAMERACAPTURE_API __declspec(dllexport)
#else
#define CAMERACAPTURE_API __declspec(dllimport)
#endif

extern "C" {
    CAMERACAPTURE_API void InitializeCamera();
    CAMERACAPTURE_API void ReleaseCamera();
    CAMERACAPTURE_API bool GetFrame(unsigned char* buffer, int* width, int* height);
}
