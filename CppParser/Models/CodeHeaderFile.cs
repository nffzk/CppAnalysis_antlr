using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CppParser.Models
{
    public class CodeHeaderFile
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 文件中的类列表
        /// </summary>
        public List<CodeClass> Classes { get; set; } = new List<CodeClass>();

        /// <summary>
        /// 文件中的枚举列表
        /// </summary>
        public List<CodeEnum> Enums { get; set; } = new List<CodeEnum>();

        /// <summary>
        /// 文件中的宏定义集合
        /// </summary>
        public CppMacroDefinitionCollection MacroDefinitions { get; set; } = new CppMacroDefinitionCollection();
    }
}
