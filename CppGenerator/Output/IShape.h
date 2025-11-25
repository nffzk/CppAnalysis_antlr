/**
 * @noop Automatically Generated Header File
 * @noop Copyright (C) 2025 shareetech.com
 * 
 * @file IShape.h
 * @brief 
 * @author ShareE
 */

#ifndef _ISHAPE_H_
#define _ISHAPE_H_

#include <string>
#include <vector>



/**
 * @class IShape
 * @brief   //
 * @details 
 */
class IShape
{

public:
	
	/**
	* @brief 默认构造函数
	*/
	IShape();
	
	/**
	* @brief 默认析构函数
	*/
	virtual ~IShape();

	/**
	* @brief 
	* @return
	*/
	virtual double Area() = 0;

	/**
	* @brief 
	* @return
	*/
	double Perimeter();

};

#endif