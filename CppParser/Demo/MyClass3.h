// 只用于测试函数声明，函数定义后续再测试

class MyClass :public MyBaseClass
{
public:
	//MyClass();
	//virtual ~MyClass();
	//std::string toString() const;
	//static void initialize();
	//void setValue(int val);
	//int* test();
	//const ccc& test(int &a, std::string b, int c[10], xx* c);
	virtual double compute(double x, double y) const;

	int test(int a = 10);
	//函数指针例子
	//typedef void (MyClass::* FuncPtrType)(int x , std::string str);
	
//private:
	int m_value[10][10];

};

