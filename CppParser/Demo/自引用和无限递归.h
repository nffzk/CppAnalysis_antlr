// 危险：自引用宏
#define SELF_REF SELF_REF
// #define INFINITE_RECURSE INFINITE_RECURSE + 1  // 编译器会报错

// 复杂条件自引用
#ifdef A
#define B A
#else
#define A B
#endif

// 宏重载（通过不同参数数量）
#define FOO_1(x) (x + 1)
#define FOO_2(x, y) (x + y)
#define GET_MACRO(_1, _2, NAME, ...) NAME
#define FOO(...) GET_MACRO(__VA_ARGS__, FOO_2, FOO_1)(__VA_ARGS__)