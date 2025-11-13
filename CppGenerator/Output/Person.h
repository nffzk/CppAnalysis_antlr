/**
 * @noop Automatically Generated Header File
 * @noop Copyright (C) 2025 shareetech.com
 * 
 * @file Person.h
 * @brief 
 * @author ShareE
 */

#ifndef _PERSON_H_
#define _PERSON_H_

#include <string>
#include <vector>

// 泛化关系
#include "LivingBeing1"
// 实现关系
#include "Realization1"
#include "Realization1"
// 聚合关系
#include "Address"

// 关联关系
class Company2;
class Company3;
class Company4;
// 单向关联关系
class Company5;

/**
 * @struct Person
 * @brief 
 * @details 
 */
struct Person : public LivingBeing1, public Realization1, public Realization1
{

public:
	
	/**
	* @brief 默认构造函数
	*/
	Person();
	
	/**
	* @brief 默认析构函数
	*/
	virtual ~Person();

	/**
	* @brief 
	* @return
	*/
	std::string getName();

	/**
	* @brief 
	* @param v 
	* @return
	*/
	void setName(const std::string& v);

	/**
	* @brief 
	* @return
	*/
	virtual std::string vfun() = 0;

	/**
	* @brief 
	* @return
	*/
	static std::string staticfun();
	
	/**
	* @brief 
	*/
	std::string name1 = "Tom";
	
	/**
	* @brief 
	*/
	std::string name3[3];
	
	/**
	* @brief 
	*/
	std::vector<std::string> name4;
	
	/**
	* @brief 
	*/
	static std::vector<std::string> name5;
	
	/**
	* @brief 
	*/
	static std::string name6;
	
	/**
	* @brief 
	*/
	static std::string name7[3];
	
	/**
	* @brief 组合关系和聚合关系作为成员变量 employer1
	*/
	std::vector<Address> employer1;
	
	/**
	* @brief 关联关系成员变量 employer1
	*/
	Company2* employer1;
	
	/**
	* @brief 关联关系成员变量 employer2
	*/
	Company3* employer2;
	
	/**
	* @brief 单向关联关系成员变量 employer4
	*/
	std::vector<Company5*> employer4;

private:
	
	/**
	* @brief 
	*/
	int age;
	
	/**
	* @brief 
	*/
	bool name2 = "Tom1";

};

#endif