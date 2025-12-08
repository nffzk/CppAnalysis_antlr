#define MAX(a, b) ((a) > (b) ? (a) : (b))
#define SQUARE(x) ((x) * (x))

int i = 1;
int j = MAX(i++, 5);  // 危险：i可能被多次递增
int k = SQUARE(i++);   // 更危险：i被递增两次