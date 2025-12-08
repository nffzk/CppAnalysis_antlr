#define COMPLEX_MACRO(a, b, ...) \
    do { \
        int _a = (a); \
        int _b = (b); \
        printf("Values: %d, %d", _a, _b); \
        MACRO_VA_ARGS(__VA_ARGS__) \
    } while(0)

#define MACRO_VA_ARGS(first, ...) \
    printf(", Extra: %d", first); \
    MACRO_VA_ARGS_REST(__VA_ARGS__)

#define MACRO_VA_ARGS_REST(...) \
    __VA_OPT__(printf(" More: " __VA_ARGS__))

COMPLEX_MACRO(1, 2, 3, 4, 5);
COMPLEX_MACRO(1, 2);