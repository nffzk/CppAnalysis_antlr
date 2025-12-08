// 宏递归（通过延迟展开实现）
#define EMPTY()
#define DEFER(id) id EMPTY()
#define OBSTRUCT(...) __VA_ARGS__ DEFER(EMPTY)()
#define EXPAND(...) __VA_ARGS__

#define PRIMITIVE_CAT(x, y) x ## y
#define CAT(x, y) PRIMITIVE_CAT(x, y)

#define REPEAT_1(func, arg) func(arg)
#define REPEAT_2(func, arg) func(arg) REPEAT_1(func, arg)
#define REPEAT_3(func, arg) func(arg) REPEAT_2(func, arg)
#define REPEAT_4(func, arg) func(arg) REPEAT_3(func, arg)
#define REPEAT_5(func, arg) func(arg) REPEAT_4(func, arg)

#define REPEAT(n, func, arg) CAT(REPEAT_, n)(func, arg)

#define MAKE_VAR(n) int var##n = n;
REPEAT(5, MAKE_VAR, _)  // 生成 var0=0; var1=1; ... var4=4;