using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CppParser.Models
{
    public class CppMethod : CppElement
    {
        public string ReturnType { get; set; }
        public bool ReturnTypeIsPointer { get; set; }
        public bool ReturnTypeIsReference { get; set; }
        public List<CppMethodParameter> Parameters { get; set; } = new List<CppMethodParameter>();
        public bool IsVirtual { get; set; }
        public bool IsPureVirtual { get; set; }
        public bool IsReturnConst { get; set; }
        public bool IsStatic { get; set; }
        public bool IsExplicit { get; set; }
        public bool IsInline { get; set; }
        public bool IsFriend { get; set; }
        public bool IsConstexpr { get; set; }
        public bool IsConst { get; set; }
        public bool IsDefaultImplementation { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsOverride { get; set; }
        public bool IsFinal { get; set; }
    }
}
