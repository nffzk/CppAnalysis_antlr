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

enum test
{
	t,
	t2,
}

class Company2;
class Company3;
class Company4;
class Person : public LivingBeing1, public LivingBeing2, public Realization1, public Realization1
{

public:

    Person(); 
    
    virtual ~Person();

	std::string getName();

	void setName(const std::string& v);

	virtual std::string vfun() = 0;

	static std::string staticfun();

	std::string name1; = "Tom";

	std::string name3[3];

	std::vector<std::string> name4;

	static std::vector<std::string> name5;

	static std::string name6;

	static std::string name7[3];

	std::vector<Address> employer1;

	Company2* employer1[0];

	std::vector<Company3*> employer2;

	Company4* employer3[3];

	std::vector<Company5*> employer4;

	Company6* employer5[4];

	test t1;

private:

	int age;;

	std::string name2; = "Tom1";

};

#endif