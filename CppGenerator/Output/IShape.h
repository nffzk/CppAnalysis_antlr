#ifndef _ISHAPE_H_
#define _ISHAPE_H_

#include <string>
#include <vector>


class IShape
{

public:

    IShape(); 
    
    virtual ~IShape();

	virtual double Area() = 0;

	virtual double Perimeter();

};

#endif