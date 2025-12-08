using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CppParser.Models
{
    /// <summary>
    /// 宏定义集合
    /// </summary>
    public class CppMacroDefinitionCollection
    {
        /// <summary>
        /// 宏定义字典，键为宏名称
        /// </summary>
        public Dictionary<string, CppMacroDefinition> Macros { get; set; } = new Dictionary<string, CppMacroDefinition>();

        /// <summary>
        /// 添加宏定义
        /// </summary>
        public void AddMacro(CppMacroDefinition macro)
        {
            // 如果宏名称重复，则覆盖
            if (macro != null && !string.IsNullOrEmpty(macro.Name))
            {
                Macros[macro.Name] = macro;
            }
        }

        /// <summary>
        /// 获取宏定义
        /// </summary>
        public CppMacroDefinition GetMacro(string name)
        {
            return Macros.ContainsKey(name) ? Macros[name] : null;
        }

        /// <summary>
        /// 检查宏是否存在
        /// </summary>
        public bool ContainsMacro(string name)
        {
            return Macros.ContainsKey(name);
        }
    }

    /// <summary>
    /// 单个宏定义
    /// </summary>
    public class CppMacroDefinition
    {
        /// <summary>
        /// 宏名称，如果带参数，参数列表不包含在内。例如：#define MAX(a,b) max(a,b)中的名称为 "MAX"
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 宏值（可为空）。例如：#define MAX(a,b) max(a,b)中的宏值为 max(a,b)
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// 完整的宏定义指令文本
        /// </summary>
        public string FullInstruction
        {
            get
            {
                if (IsFunctionLike)
                {
                    var paramsStr = string.Join(",", Parameters);
                    return $"#define {Name}({paramsStr}) {Value}";
                }
                else
                {
                    return $"#define {Name} {Value}";
                }
            }
        }

        /// <summary>
        /// 宏参数列表（对于带参数的宏）。例如：#define MAX(a,b) max(a,b)中的参数列表为 a,b
        /// </summary>
        public List<string> Parameters { get; set; } = new List<string>();

        /// <summary>
        /// 是否为函数式宏（带参数）
        /// </summary>
        public bool IsFunctionLike { get; set; } = false;
    }

}
