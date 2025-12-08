#if defined(_WIN32)
#define DLL_EXPORT __declspec(dllexport)
#define DLL_IMPORT __declspec(dllimport)
#define STDCALL __stdcall
#define CDECL __cdecl
#else
#define DLL_EXPORT __attribute__((visibility("default")))
#define DLL_IMPORT
#define STDCALL
#define CDECL
#endif

#ifdef BUILD_DLL
#define API DLL_EXPORT
#else
#define API DLL_IMPORT
#endif

#define CALLBACK STDCALL
#define APIENTRY STDCALL
#define WINAPI STDCALL