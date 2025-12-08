#define COMPILE_TIME_ASSERT(cond, msg) \
    typedef char COMPILE_TIME_ASSERT_##msg[(cond) ? 1 : -1]

#define STATIC_ASSERT_MSG(cond, msg) \
    do { \
        extern int COMPILE_TIME_CHECK_##msg[1]; \
        extern int COMPILE_TIME_CHECK_##msg[(cond) ? 1 : 2]; \
    } while(0)

#define TYPE_TRAIT_CHECK(T, trait) \
    STATIC_ASSERT_MSG(trait<T>::value, T_must_support_##trait)

COMPILE_TIME_ASSERT(sizeof(int) == 4, int_size_check);