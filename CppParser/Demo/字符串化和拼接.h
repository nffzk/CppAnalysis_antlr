#define STRINGIFY_DETAIL(x) #x
#define STRINGIFY(x) STRINGIFY_DETAIL(x)

#define CONCAT_DETAIL(a, b) a##b
#define CONCAT(a, b) CONCAT_DETAIL(a, b)

#define MAKE_UNIQUE_NAME(prefix) CONCAT(prefix, __LINE__)
#define MAKE_TYPED_NAME(type, name) type CONCAT(name, _t)

int MAKE_UNIQUE_NAME(var_) = 10;
MAKE_TYPED_NAME(int, MyVar) = 20;
const char* str = STRINGIFY(__LINE__);