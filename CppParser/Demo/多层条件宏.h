#if defined(PLATFORM_WIN32)
#define OS_TYPE "Windows"
#if _WIN64
#define ARCH "x64"
#define MAX_PATH 260
#else
#define ARCH "x86"
#define MAX_PATH 260
#endif
#elif defined(PLATFORM_LINUX)
#define OS_TYPE "Linux"
#if __x86_64__
#define ARCH "x64"
#define MAX_PATH 4096
#elif __aarch64__
#define ARCH "ARM64"
#define MAX_PATH 4096
#endif
#else
#define OS_TYPE "Unknown"
#define ARCH "Unknown"
#define MAX_PATH 256
#endif

#if defined(DEBUG) && defined(ENABLE_LOGGING)
#define LOG_DEBUG(fmt, ...) \
        printf("[DEBUG][%s:%d] " fmt, __FILE__, __LINE__, ##__VA_ARGS__)
#else
#define LOG_DEBUG(fmt, ...)
#endif