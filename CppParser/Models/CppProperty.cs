using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CppParser.Models
{
    public class CppProperty : CppElement
    {
        public string Type { get; set; }
        public string FullType { get; set; } // 包含所有修饰符的完整类型
        public bool IsStatic { get; set; }
        public string DefaultValue { get; set; }
        public bool IsConst { get; set; }
        public bool IsVolatile { get; set; }
        public bool IsMutable { get; set; }
        public bool IsSigned { get; set; }
        public bool IsUnsigned { get; set; }
        public bool IsShort { get; set; }
        public bool IsLong { get; set; }
        public bool IsPointer { get; set; }
        public bool IsReference { get; set; }
        public bool IsArray { get; set; }
        public string ArraySize { get; set; } // 数组大小
    }
}
