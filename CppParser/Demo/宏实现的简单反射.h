#define RECURSE_IF(n, max) \
    RECURSE_##n##_IMPL(n, max)

#define RECURSE_0_IMPL(n, max) "Base"
#define RECURSE_1_IMPL(n, max) (n < max ? RECURSE_IF(n+1, max) : "End")
#define RECURSE_2_IMPL(n, max) (n < max ? RECURSE_IF(n+2, max) : "End2")

const char* r1 = RECURSE_IF(1, 5);