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
 * @brief 
 * @details 
 */
class IShape
{

public:

    IShape(); 
    
    virtual ~IShape();

	/**
	* @brief 
	* @return
	*/
	double Area();

	/**
	* @brief 
	* @return
	*/
	virtual double Perimeter();

};

#endif