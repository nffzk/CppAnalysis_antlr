using CppParser.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CppParser.Models
{
    public class CodeProperty : CodeElement
    {
        /// <summary>
        /// 属性类型  导入导出时记录：基础类型。
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 属性类型（自定义类型、枚举类型、数据类型） ，
        /// </summary>
        public string CustomType { get; set; }

        /// <summary>
        /// 多重性，用于记录数组维度（只考虑一维，如果是多维参考rhapsody类型放在customType）
        /// </summary>
        public EnumCppMultiplicity Multiplicity { get; set; }

        /// <summary>
        /// 记录原型中多重性的字符串表示
        /// </summary>
        public string MarkMultiplicity { get; set; }

        /// <summary>
        /// 是否为静态属性
        /// </summary>
        public bool IsStatic { get; set; }

        /// <summary>
        /// 默认值
        /// </summary>
        public string DefaultValue { get; set; }

    }
}
