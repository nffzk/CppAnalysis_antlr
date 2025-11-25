class MyClass :public MyBaseClass
{
public:
	MyClass() :m_data(0) {}

	virtual ~MyClass() {}

	int getData() const {
		return m_data;
	}

	int getData() const {
		return m_data;
	}

	void setData(int data) {
		m_data = data;
	}

private:
	int m_data;

};
