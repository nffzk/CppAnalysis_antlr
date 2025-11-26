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
        /// 枚举值列表
        /// </summary>
        public List<CodeEnumValue> ValueList { get; set; } = [];

        /// <summary>
        /// 是否为 scoped enum（enum class）（没用到）
        /// </summary>
        public bool IsScoped { get; set; }

        /// <summary>
        /// 枚举的底层类型（如有指定int）（没用到）
        /// </summary>
        public string UnderlyingType { get; set; } 
    }

    public class CodeEnumValue
    {
        /// <summary>
        /// 枚举值名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 枚举值中文名称
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// 注释信息
        /// </summary>
        public string Comment { get; set; }
    }
}
