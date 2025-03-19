/**
 * @noop Automatically Generated Header File
 * @noop Copyright (C) 2025 shareetech.com
 * 
 * @file Node.h
 * @brief 
 * @author ShareE
 */

#ifndef _NODE_H_
#define _NODE_H_

#include <string>
#include <vector>


// 关联关系
struct Node2;
// 单向关联关系
struct Node1;

/**
 * @struct Node
 * @brief 
 * @details 
 */
struct Node
{

public:
	
	/**
	* @brief 默认构造函数
	*/
	Node();
	
	/**
	* @brief 默认析构函数
	*/
	virtual ~Node();
	
	/**
	* @brief  Node 
	*/
	Node left;
	
	/**
	* @brief  Node 
	*/
	Node right;
	
	/**
	* @brief 关联关系成员变量 prev
	*/
	Node2* prev;
	
	/**
	* @brief 单向关联关系成员变量 next
	*/
	Node1* next;

};

#endif