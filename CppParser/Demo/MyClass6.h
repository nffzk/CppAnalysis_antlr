#define MAX_BUFFER_SIZE 1024
#define DEFAULT_TIMEOUT 5000
#define MAX(a, b) ((a) > (b) ? (a) : (b))
#define MIN(a, b) ((a) < (b) ? (a) : (b))
#define A B
#define B A
//#define SQUARE(x) ((x) * (x))
/*
#define SQUARE(x) \
	do { \
		(x) * (x); \
	} while (0)
*/

#define SQUARE1(x) \
	do { \
		(x) * (x); \
	} while (0)


#include <string>  #define PI 3.14159  #define MAX1(a,b) ((a)>(b)?(a):(b))
class Buffer {
private:
    char data[MAX_BUFFER_SIZE];
    int timeout = DEFAULT_TIMEOUT;

public:
    int getMaxSize() {
       return MAX(MAX_BUFFER_SIZE, 2048);
	}
//
//    int calculate(int x, int y) {
//        return MAX(x, MIN(y, MAX_BUFFER_SIZE));
//    }
//
//	int square(int x) {
//		return SQUARE1(x);
//	}

	void teststring()
	{
		std::string str = "This is a test string with a macro SQUARE1(5) inside.";
	}
	int AA() {
		return B;
	}
};


