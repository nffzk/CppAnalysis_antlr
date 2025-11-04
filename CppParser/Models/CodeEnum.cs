using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CppParser.Models
{
    public class CodeEnum : CodeElement
    {

        /// <summary>
        /// key 枚举值，value 中文名称
        /// </summary>
        public Dictionary<string,string> Values { get; set; }

        /// <summary>
        /// 是否为 scoped enum（enum class）（没用到）
        /// </summary>
        public bool IsScoped { get; set; }

        /// <summary>
        /// 枚举的底层类型（如有指定int）（没用到）
        /// </summary>
        public string UnderlyingType { get; set; } 
    }
}
