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


// 单向关联关系
class Class1;

/**
 * @struct Person
 * @brief   //
 * @details 
 */
struct Person
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
	* @param   //
	* @return
	*/
	void setName(int name);

	/**
	* @brief 
	* @param   //
	* @return
	*/
	void setName(const std::string& v);
	
	/**
	* @brief   //
	*/
	Class1 c1 = "Tom";
	
	/**
	* @brief 单向关联关系成员变量 
	*/
	std::vector<Class1*> c2;

};

#endif