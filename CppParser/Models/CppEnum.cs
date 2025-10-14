using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CppParser.Models
{
    public class CppEnum : CppElement
    {
        public List<string> Values { get; set; } = new List<string>();
        public bool IsScoped { get; set; } // 是否为 enum class 或 enum struct
        public string UnderlyingType { get; set; } // 基础类型，如 int, char 等
    }
}
