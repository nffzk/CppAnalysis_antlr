#define ENUM_CASE(name) case name: return #name
#define ENUM_TO_STRING(enum_type) \
    const char* enum_type##_to_string(enum_type value) { \
        switch (value) {

#define END_ENUM_TO_STRING(default_return) \
        default: return default_return; \
        } \
    }

enum Color { RED, GREEN, BLUE };

ENUM_TO_STRING(Color)
ENUM_CASE(RED);
ENUM_CASE(GREEN);
ENUM_CASE(BLUE);
END_ENUM_TO_STRING("UNKNOWN")

#define MAKE_GETTER(type, name) \
    type get_##name() const { return m_##name; }

#define MAKE_SETTER(type, name) \
    void set_##name(type name) { m_##name = name; }

#define PROPERTY(type, name) \
private: \
    type m_##name; \
public: \
    MAKE_GETTER(type, name) \
    MAKE_SETTER(type, name)