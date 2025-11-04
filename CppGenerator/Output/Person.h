#ifndef _PERSON_H_
#define _PERSON_H_

#include <string>
#include <vector>

// 泛化关系
#include "LivingBeing1"
#include "LivingBeing2"
// 实现关系
#include "Realization1"
#include "Realization1"
// 聚合关系
#include "Address"

// 关联关系
class Company;
class Person : public LivingBeing1, public LivingBeing2, public Realization1, public Realization1
{

public:

    Person(); 
    
    virtual ~Person();

    std::string getName();

    void setName(const std::string& v);

    static std::string staticfun();

    std::string name1 = "Tom";

private:

    int age;

    std::string name2 = "Tom1";

};

#endif
