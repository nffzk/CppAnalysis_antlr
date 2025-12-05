using System;
using System.Collections.Generic;
using CppParser.Models;

namespace CppParser.Services.Interfaces
{
    /// <summary>
    /// C++ 宏替换器接口
    /// 专门处理头文件字符串中的宏替换
    /// </summary>
    public interface ICppMacroReplacer
    {
        /// <summary>
        /// 对头文件字符串执行宏替换
        /// </summary>
        /// <param name="headerContent">C++ 头文件内容</param>
        /// <param name="macros">宏定义集合</param>
        /// <returns>替换后的头文件内容</returns>
        string ReplaceMacrosInHeader(string headerContent, CppMacroDefinitionCollection macros);

        /// <summary>
        /// 对头文件字符串执行宏替换（指定替换选项）
        /// </summary>
        /// <param name="headerContent">C++ 头文件内容</param>
        /// <param name="macros">宏定义集合</param>
        /// <param name="options">替换选项</param>
        /// <returns>替换后的头文件内容</returns>
        string ReplaceMacrosInHeader(string headerContent, CppMacroDefinitionCollection macros, MacroReplacementOptions options);


        /// <summary>
        /// 检查头文件内容中是否包含指定的宏
        /// </summary>
        /// <param name="headerContent">头文件内容</param>
        /// <param name="macroName">宏名称</param>
        /// <returns>是否包含</returns>
        bool ContainsMacro(string headerContent, string macroName);
    }

    /// <summary>
    /// 宏替换选项
    /// </summary>
    public class MacroReplacementOptions
    {
        /// <summary>
        /// 是否递归替换嵌套宏（默认：true）
        /// </summary>
        public bool RecursiveReplacement { get; set; } = true;

        /// <summary>
        /// 最大递归深度（默认：10）
        /// </summary>
        public int MaxRecursionDepth { get; set; } = 10;

        /// <summary>
        /// 是否保留注释中的宏（默认：false）
        /// </summary>
        public bool PreserveMacrosInComments { get; set; } = false;

        /// <summary>
        /// 是否保留字符串字面量中的宏（默认：false）
        /// </summary>
        public bool PreserveMacrosInStrings { get; set; } = false;

        /// <summary>
        /// 是否记录替换统计信息（默认：true）
        /// </summary>
        public bool LogStatistics { get; set; } = true;

        /// <summary>
        /// 是否启用严格模式（遇到未知宏时抛出异常）（默认：false）
        /// </summary>
        public bool StrictMode { get; set; } = false;

        /// <summary>
        /// 要排除的宏名称列表（不进行替换）
        /// </summary>
        public List<string> ExcludedMacros { get; set; } = new List<string>();
    }

}