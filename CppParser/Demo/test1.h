//#include <iostream>
//#include "tttt"

#define SQUARE(x) ((x) * (x))
#define MAX(a, b) ((a) > (b) ? (a) : (b))
#define EMPTY()
class Test
{
    int x = 5;
    int y = SQUARE(x + 1);  // 展开为 ((x + 1) * (x + 1))
    int z = MAX(x, 10);
	int k = EMPTY;  // 展开为空
};

int main() {

    int x = 3;
#ifdef DEBUG
    std::cout << "Debug mode" << std::endl;
#endif

    return 0;
}