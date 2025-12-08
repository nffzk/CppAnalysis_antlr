#ifdef max
#undef max
#define max(a, b) (((a) > (b)) ? (a) : (b))
#endif

#ifdef min
#undef min
#define min(a, b) (((a) < (b)) ? (a) : (b))
#endif

// 临时宏定义
#define TEMP_MACRO
#ifdef TEMP_MACRO
    // 使用临时宏
int temp_var = 1;
#endif
#undef TEMP_MACRO
// TEMP_MACRO 这里已不再定义