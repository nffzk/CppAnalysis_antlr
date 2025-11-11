using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CppParser.Enums
{
    public enum EnumClassType
    {
        /// <summary>
        /// 无类型
        /// </summary>
        None = 0,

        /// <summary>
        /// 类
        /// </summary>
        Class,

        /// <summary>
        /// 结构体
        /// </summary>
        Struct,

        /// <summary>
        /// 接口
        /// </summary>
        Interface,

        /// <summary>
        /// 抽象类
        /// </summary>
        AbstractClass,

        /// <summary>
        /// 枚举，只在处理关系的时候用到
        /// </summary>
        Enum,

        /// <summary>
        /// 联合体，只在读取 C++ 代码时用到
        /// </summary>
        Union,
    }
}
