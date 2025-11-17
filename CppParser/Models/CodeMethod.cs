using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CppParser.Models
{
    public class CodeMethod : CodeElement
    {

        /// <summary>
        /// 方法返回类型(void+基础类型)
        /// </summary>
        public string ReturnType { get; set; }

        /// <summary>
        /// 方法返回类型（自定义类型、枚举类型、数据类型） ，
        /// </summary>
        public string CustomReturnType { get; set; }

        /// <summary>
        /// 方法返回值是否为指针
        /// </summary>
        public bool IsReturnPointer;

        /// <summary>
        /// 方法的底层返回类型（用于记录原始类型信息）。去除vector、std::<vector>、 * 、& 、&& 的returnType/customReturnType
        /// </summary>
        public string UnderlyingReturnType { get; set; }

        /// <summary>
        /// 方法参数列表
        /// </summary>
        public List<CodeMethodParameter> Parameters { get; set; } = new List<CodeMethodParameter>();

        /// <summary>
        /// 方法是否为虚函数
        /// </summary>
        public bool IsVirtual { get; set; }

        /// <summary>
        /// 方法是否为纯虚函数
        /// </summary>
        public bool IsPureVirtual { get; set; }

        /// <summary>
        /// 方法是否为静态函数
        /// </summary>
        public bool IsStatic { get; set; }

    }
}
