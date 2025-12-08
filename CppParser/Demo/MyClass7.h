


#define MAX(a,b,c) MAX1(MAX1(a,b),c)  // 宏嵌套使用

#define MAX1(a,b) ((a)>(b)?(a):(b))
class Buffer {
	double area = MAX(5,12,4);  
	//double pi = PI;
};


