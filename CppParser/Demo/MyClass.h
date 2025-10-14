// MyClass.h - 测试用的C++头文件
#pragma once
#include <string>
#include <vector>

enum Color {
    RED ,
    GREEN,
    BLUE
};

class MyBaseClass
{
public:
    int b = 1;

	enum status { SUCCESS, FAILURE, PENDING };
	int* x;
	int* c;
private:
	int m_id;
	std::string m_name; // 成员变量
};


class MyClass : public MyBaseClass {
protected:
    double calculatedValue;
    int a[12];
    int* c;
public:
    explicit MyClass(int id);

    void setName(const std::string& name);
    

    virtual void doSomething(int param[10]);
    virtual int calculate() const = 0;

private:
    void internalHelper();

protected:
    double calculatedValue2;
};