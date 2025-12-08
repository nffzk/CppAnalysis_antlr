#define LEVEL1 10
#define LEVEL2 (LEVEL1 + 20)
#define LEVEL3 (LEVEL2 * 2)
#define LEVEL4 ((LEVEL3) / 5 + LEVEL1)
#define LEVEL5 LEVEL4##_SUFFIX
#define CONCAT_LEVEL5(x) x##LEVEL4

int test1 = LEVEL4;  // 展开链: (( ( (10 + 20) * 2) / 5 + 10)
auto var = CONCAT_LEVEL5(prefix_);  // prefix_16