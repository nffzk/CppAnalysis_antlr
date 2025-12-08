#define SAFE_DELETE(p) \
    do { \
        if (p) { \
            delete (p); \
            (p) = nullptr; \
            LOG_DEBUG("Deleted at %s:%d", __FILE__, __LINE__); \
        } \
    } while(0)

#define ARRAY_SIZE(arr) (sizeof(arr) / sizeof((arr)[0]))

#define FOR_EACH(i, arr, size) \
    for (int _i = 0, _size = (size); _i < _size && ((i) = (arr)[_i], true); ++_i)

#define CHECK_RETURN(expr, ret_val) \
    do { \
        auto _result = (expr); \
        if (!(_result)) { \
            LOG_DEBUG("Check failed: %s", #expr); \
            return (ret_val); \
        } \
    } while(0)