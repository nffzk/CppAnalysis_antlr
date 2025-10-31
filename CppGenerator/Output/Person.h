#ifndef _PERSON_H_
#define _PERSON_H_

#include <string>
#include <vector>


class Person
{

public:

    Person(); 
    
    virtual ~Person();

    std::string getName() const;

    void setName(const std::string& v);

    std::string name1 = "Tom";

private:

    int age;

    std::string name2 = "Tom1";

};

#endif
