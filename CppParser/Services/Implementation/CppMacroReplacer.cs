using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CppParser.Models;
using CppParser.Services.Interfaces;

namespace CppParser.Services.Implementation
{
    /// <summary>
    /// C++ 宏替换器实现
    /// 处理头文件字符串中的宏替换逻辑
    /// </summary>
    public class CppMacroReplacer : ICppMacroReplacer
    {
        public string ReplaceMacrosInHeader(string headerContent, CppMacroDefinitionCollection macros)
        {
            return ReplaceMacrosInHeader(headerContent, macros, new MacroReplacementOptions());
        }

        public string ReplaceMacrosInHeader(string headerContent, CppMacroDefinitionCollection macros, MacroReplacementOptions options)
        {
            if (string.IsNullOrEmpty(headerContent))
                return headerContent;

            if (macros == null || macros.Macros.Count == 0)
                return headerContent;

            try
            {
                var result = PerformMacroReplacement(headerContent, macros, options, 0);

                return result.Content;
            }
            catch (Exception ex)
            {
                if (options.StrictMode)
                    throw new CppMacroReplacementException("宏替换失败", ex);

                return headerContent; // 非严格模式下返回原始内容
            }
        }

        public bool ContainsMacro(string headerContent, string macroName)
        {
            if (string.IsNullOrEmpty(headerContent) || string.IsNullOrEmpty(macroName))
                return false;

            // 使用正则表达式匹配宏使用（排除定义和注释中的使用）
            string pattern = $@"\b{Regex.Escape(macroName)}\b(?!(?:\s*\([^)]*\))?\s*#define)";
            return Regex.IsMatch(headerContent, pattern, RegexOptions.Multiline);
        }

        #region 私有实现方法
        /// <summary>
        /// 执行递归宏展开，处理宏嵌套
        /// </summary>
        private ExpansionResult PerformRecursiveMacroExpansion(string content, CppMacroDefinitionCollection macros, MacroReplacementOptions options, int currentDepth)
        {
            if (currentDepth > options.MaxRecursionDepth)
            {
                throw new CppMacroReplacementException($"达到最大递归深度: {options.MaxRecursionDepth}");
            }

            Console.WriteLine($"[CppMacroReplacer] 递归深度 {currentDepth}，内容长度: {content.Length}");

            var result = new ExpansionResult
            {
                Content = content,
                RecursionDepth = currentDepth,
                TotalReplacements = 0
            };

            bool changed;
            int iteration = 0;
            const int maxIterations = 50;

            // 构建宏依赖图，确定替换顺序
            var dependencyGraph = BuildMacroDependencyGraph(macros);
            var replacementOrder = GetMacroReplacementOrder(dependencyGraph, macros);

            do
            {
                changed = false;
                iteration++;

                if (iteration > maxIterations)
                {
                    throw new CppMacroReplacementException($"达到最大迭代次数: {maxIterations}");
                }

                Console.WriteLine($"[CppMacroReplacer] 迭代 {iteration}，当前内容: {result.Content.Length} 字符");

                // 按依赖顺序替换宏
                foreach (var macroName in replacementOrder)
                {
                    if (!macros.Macros.TryGetValue(macroName, out var macro))
                        continue;

                    if (options.ExcludedMacros.Contains(macro.Name))
                        continue;

                    var replacementResult = ReplaceMacroWithExpansion(result.Content, macro, macros, options, currentDepth + 1);
                    if (replacementResult.Changed)
                    {
                        result.Content = replacementResult.Content;
                        result.TotalReplacements += replacementResult.ReplacementCount;
                        changed = true;

                        Console.WriteLine($"[CppMacroReplacer] 替换宏 '{macro.Name}'，次数: {replacementResult.ReplacementCount}");

                        // 记录统计信息
                        if (!result.Statistics.ContainsKey(macro.Name))
                            result.Statistics[macro.Name] = 0;
                        result.Statistics[macro.Name] += replacementResult.ReplacementCount;
                    }
                }

            } while (changed && options.RecursiveReplacement);

            // 如果还有变化，继续递归展开
            if (changed && currentDepth < options.MaxRecursionDepth)
            {
                var nextResult = PerformRecursiveMacroExpansion(result.Content, macros, options, currentDepth + 1);
                result.Content = nextResult.Content;
                result.TotalReplacements += nextResult.TotalReplacements;
                result.RecursionDepth = Math.Max(result.RecursionDepth, nextResult.RecursionDepth);

                // 合并统计信息
                foreach (var stat in nextResult.Statistics)
                {
                    if (!result.Statistics.ContainsKey(stat.Key))
                        result.Statistics[stat.Key] = 0;
                    result.Statistics[stat.Key] += stat.Value;
                }
            }

            return result;
        }        /// <summary>
                 /// 执行递归宏展开，处理宏嵌套
                 /// </summary>
        private ExpansionResult PerformRecursiveMacroExpansion(string content, CppMacroDefinitionCollection macros, MacroReplacementOptions options, int currentDepth)
        {
            if (currentDepth > options.MaxRecursionDepth)
            {
                throw new CppMacroReplacementException($"达到最大递归深度: {options.MaxRecursionDepth}");
            }

            Console.WriteLine($"[CppMacroReplacer] 递归深度 {currentDepth}，内容长度: {content.Length}");

            var result = new ExpansionResult
            {
                Content = content,
                RecursionDepth = currentDepth,
                TotalReplacements = 0
            };

            bool changed;
            int iteration = 0;
            const int maxIterations = 50;

            // 构建宏依赖图，确定替换顺序
            var dependencyGraph = BuildMacroDependencyGraph(macros);
            var replacementOrder = GetMacroReplacementOrder(dependencyGraph, macros);

            do
            {
                changed = false;
                iteration++;

                if (iteration > maxIterations)
                {
                    throw new CppMacroReplacementException($"达到最大迭代次数: {maxIterations}");
                }

                Console.WriteLine($"[CppMacroReplacer] 迭代 {iteration}，当前内容: {result.Content.Length} 字符");

                // 按依赖顺序替换宏
                foreach (var macroName in replacementOrder)
                {
                    if (!macros.Macros.TryGetValue(macroName, out var macro))
                        continue;

                    if (options.ExcludedMacros.Contains(macro.Name))
                        continue;

                    var replacementResult = ReplaceMacroWithExpansion(result.Content, macro, macros, options, currentDepth + 1);
                    if (replacementResult.Changed)
                    {
                        result.Content = replacementResult.Content;
                        result.TotalReplacements += replacementResult.ReplacementCount;
                        changed = true;

                        Console.WriteLine($"[CppMacroReplacer] 替换宏 '{macro.Name}'，次数: {replacementResult.ReplacementCount}");

                        // 记录统计信息
                        if (!result.Statistics.ContainsKey(macro.Name))
                            result.Statistics[macro.Name] = 0;
                        result.Statistics[macro.Name] += replacementResult.ReplacementCount;
                    }
                }

            } while (changed && options.RecursiveReplacement);

            // 如果还有变化，继续递归展开
            if (changed && currentDepth < options.MaxRecursionDepth)
            {
                var nextResult = PerformRecursiveMacroExpansion(result.Content, macros, options, currentDepth + 1);
                result.Content = nextResult.Content;
                result.TotalReplacements += nextResult.TotalReplacements;
                result.RecursionDepth = Math.Max(result.RecursionDepth, nextResult.RecursionDepth);

                // 合并统计信息
                foreach (var stat in nextResult.Statistics)
                {
                    if (!result.Statistics.ContainsKey(stat.Key))
                        result.Statistics[stat.Key] = 0;
                    result.Statistics[stat.Key] += stat.Value;
                }
            }

            return result;
        }
        /// <summary>
        /// 执行宏替换的核心逻辑
        /// </summary>
        /// <param name="content"></param>
        /// <param name="macros"></param>
        /// <param name="options"></param>
        /// <param name="currentDepth"></param>
        /// <returns> 宏替换结果 </returns>
        /// <exception cref="CppMacroReplacementException"></exception>
        private ReplacementResult PerformMacroReplacement(string content, CppMacroDefinitionCollection macros, MacroReplacementOptions options, int currentDepth)
        {
            if (currentDepth > options.MaxRecursionDepth)
            {
                throw new CppMacroReplacementException($"达到最大递归深度: {options.MaxRecursionDepth}");
            }

            var context = new ReplacementContext
            {
                ReplacedContent = content,
                RecursionDepth = currentDepth
            };

            bool changed;
            int iteration = 0;
            const int maxIterations = 100; // 防止无限循环

            do
            {
                changed = false;
                iteration++;

                if (iteration > maxIterations)
                {
                    throw new CppMacroReplacementException($"达到最大迭代次数: {maxIterations}");
                }

                // 对每个宏进行替换
                foreach (var macro in GetReplacementOrder(macros))
                {
                    if (options.ExcludedMacros.Contains(macro.Name))
                        continue;

                    if (context.ProcessedMacros.Contains(macro.Name))
                        continue;

                    var replacementResult = ReplaceSingleMacro(context.ReplacedContent, macro, options);
                    if (replacementResult.Changed)
                    {
                        context.ReplacedContent = replacementResult.Content;
                        context.ReplacementCount += replacementResult.ReplacementCount;

                        // 更新统计信息
                        if (!context.Statistics.ContainsKey(macro.Name))
                            context.Statistics[macro.Name] = 0;
                        context.Statistics[macro.Name] += replacementResult.ReplacementCount;

                        changed = true;
                        context.ProcessedMacros.Add(macro.Name);
                    }
                }

            } while (changed && options.RecursiveReplacement);

            return new ReplacementResult
            {
                Content = context.ReplacedContent,
                Changed = context.ReplacementCount > 0,
                ReplacementCount = context.ReplacementCount,
                Statistics = context.Statistics
            };
        }

        /// <summary>
        /// 获取宏的替换顺序，优先替换函数式宏，然后按名称长度降序排列
        /// </summary>
        /// <param name="macros"></param>
        /// <returns></returns>
        private IEnumerable<CppMacroDefinition> GetReplacementOrder(CppMacroDefinitionCollection macros)
        {
            // 优先替换函数式宏，然后替换对象式宏
            // 按名称长度降序排列，避免部分匹配
            return macros.Macros.Values
                .OrderByDescending(m => m.IsFunctionLike ? 1 : 0)
                .ThenByDescending(m => m.Name.Length)
                .ThenBy(m => m.Name);
        }

        /// <summary>
        /// 替换单个宏
        /// </summary>
        /// <param name="content"></param>
        /// <param name="macro"></param>
        /// <param name="options"></param>
        /// <returns>单个宏替换结果</returns>
        private SingleReplacementResult ReplaceSingleMacro(string content, CppMacroDefinition macro, MacroReplacementOptions options)
        {
            int replacementCount = 0;
            string result = content;

            if (macro.IsFunctionLike)
            {
                // 函数式宏替换
                result = ReplaceFunctionLikeMacro(content, macro, options, ref replacementCount);
            }
            else
            {
                // 对象式宏替换
                result = ReplaceObjectLikeMacro(content, macro, options, ref replacementCount);
            }

            return new SingleReplacementResult
            {
                Content = result,
                Changed = replacementCount > 0,
                ReplacementCount = replacementCount
            };
        }

        /// <summary>
        /// 替换函数式宏
        /// </summary>
        /// <param name="content"></param>
        /// <param name="macro"></param>
        /// <param name="options"></param>
        /// <param name="replacementCount"></param>
        /// <returns>替换后的内容</returns>
        /// <exception cref="CppMacroReplacementException"></exception>
        private string ReplaceFunctionLikeMacro(string content, CppMacroDefinition macro, MacroReplacementOptions options, ref int replacementCount)
        {
            // 函数式宏的模式：MACRO(arg1, arg2, ...)
            string pattern = $@"\b{Regex.Escape(macro.Name)}\s*\(([^()]*(?:\((?<depth>)[^()]*\)[^()]*)*)\)";

            int localReplacementCount = 0; // 使用局部变量替代 ref 参数

            string result = Regex.Replace(content, pattern, match =>
            {
                if (ShouldSkipReplacement(match, content, options))
                    return match.Value;

                try
                {
                    string argsText = match.Groups[1].Value;
                    var arguments = ParseFunctionArguments(argsText);

                    if (arguments.Count == macro.Parameters.Count)
                    {
                        string replacedValue = macro.Value;

                        // 替换参数
                        for (int i = 0; i < macro.Parameters.Count; i++)
                        {
                            string paramPattern = $@"\b{Regex.Escape(macro.Parameters[i])}\b";
                            replacedValue = Regex.Replace(replacedValue, paramPattern, arguments[i]);
                        }

                        localReplacementCount++; // 使用局部变量计数
                        return replacedValue;
                    }
                }
                catch (Exception ex)
                {
                    if (options.StrictMode)
                        throw new CppMacroReplacementException($"函数式宏替换失败: {macro.Name}", ex);
                }

                return match.Value;
            }, RegexOptions.Multiline);

            replacementCount += localReplacementCount; // 在匿名方法外部更新计数
            return result;
        }

        /// <summary>
        /// 替换对象式宏
        /// </summary>
        /// <param name="content"></param>
        /// <param name="macro"></param>
        /// <param name="options"></param>
        /// <param name="replacementCount"></param>
        /// <returns>替换后的内容</returns>
        private string ReplaceObjectLikeMacro(string content, CppMacroDefinition macro, MacroReplacementOptions options, ref int replacementCount)
        {
            // 对象式宏的模式：MACRO（单词边界）
            string pattern = $@"\b{Regex.Escape(macro.Name)}\b";

            int localReplacementCount = 0; // 使用局部变量替代 ref 参数

            string result = Regex.Replace(content, pattern, match =>
            {
                if (ShouldSkipReplacement(match, content, options))
                    return match.Value;

                localReplacementCount++; // 使用局部变量计数
                return macro.Value;
            }, RegexOptions.Multiline);

            replacementCount += localReplacementCount; // 在匿名方法外部更新计数
            return result;
        }

        /// <summary>
        /// 检查是否应跳过替换（在注释、字符串或宏定义中）
        /// </summary>
        /// <param name="match"></param>
        /// <param name="content"></param>
        /// <param name="options"></param>
        /// <returns>true表示要跳过</returns>
        private bool ShouldSkipReplacement(Match match, string content, MacroReplacementOptions options)
        {
            int position = match.Index;

            // 检查是否在注释中
            if (!options.PreserveMacrosInComments && IsInComment(content, position))
                return true;

            // 检查是否在字符串中
            if (!options.PreserveMacrosInStrings && IsInString(content, position))
                return true;

            // 检查是否在宏定义中
            if (IsInMacroDefinition(content, position))
                return true;

            return false;
        }

        /// <summary>
        /// 检查位置是否在注释中
        /// </summary>
        /// <param name="content"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private bool IsInComment(string content, int position)
        {
            // 简化实现：检查位置之前是否有 //
            string before = content.Substring(0, position);
            int lineComment = before.LastIndexOf("//");
            int blockCommentStart = before.LastIndexOf("/*");
            int blockCommentEnd = before.LastIndexOf("*/");

            if (lineComment > 0)
            {
                int newlineAfterComment = before.IndexOf('\n', lineComment);
                if (newlineAfterComment == -1 || newlineAfterComment > position)
                    return true;
            }

            if (blockCommentStart > 0 && (blockCommentEnd == -1 || blockCommentStart > blockCommentEnd))
                return true;

            return false;
        }

        /// <summary>
        /// 检查位置是否在字符串中
        /// </summary>
        /// <param name="content"></param>
        /// <param name="position"></param>
        /// <returns>true表示在字符中</returns>
        private bool IsInString(string content, int position)
        {
            // 简化实现：检查位置之前是否有未配对的引号
            string before = content.Substring(0, position);
            int quoteCount = before.Count(c => c == '"');
            return quoteCount % 2 == 1; // 奇数字符数表示在字符串中
        }

        /// <summary>
        /// 检查位置是否在宏定义中，宏定义中的宏不进行替换
        /// </summary>
        /// <param name="content"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private bool IsInMacroDefinition(string content, int position)
        {
            string before = content.Substring(0, position);
            int definePos = before.LastIndexOf("#define");
            if (definePos >= 0)
            {
                int newlineAfterDefine = before.IndexOf('\n', definePos);
                if (newlineAfterDefine == -1 || newlineAfterDefine > position)
                    return true;
            }
            return false;
        }

        private List<string> ParseFunctionArguments(string argsText)
        {
            var arguments = new List<string>();
            int depth = 0;
            int start = 0;

            for (int i = 0; i < argsText.Length; i++)
            {
                char c = argsText[i];

                if (c == '(') depth++;
                else if (c == ')') depth--;
                else if (c == ',' && depth == 0)
                {
                    string arg = argsText.Substring(start, i - start).Trim();
                    if (!string.IsNullOrEmpty(arg))
                        arguments.Add(arg);
                    start = i + 1;
                }
            }

            // 添加最后一个参数
            string lastArg = argsText.Substring(start).Trim();
            if (!string.IsNullOrEmpty(lastArg))
                arguments.Add(lastArg);

            return arguments;
        }

        #endregion

        #region 辅助类

        /// <summary>
        /// 替换上下文信息
        /// </summary>
        private class ReplacementContext
        {
            /// <summary>
            /// 替换后的内容
            /// </summary>
            public string ReplacedContent { get; set; }

            /// <summary>
            /// 总替换次数
            /// </summary>
            public int ReplacementCount { get; set; }

            /// <summary>
            /// 当前递归深度
            /// </summary>
            public int RecursionDepth { get; set; }

            /// <summary>
            /// 替换统计信息，键为宏名称，值为替换次数
            /// </summary>
            public Dictionary<string, int> Statistics { get; set; } = new Dictionary<string, int>();

            /// <summary>
            /// 已处理的宏名称集合
            /// </summary>
            public HashSet<string> ProcessedMacros { get; set; } = new HashSet<string>();
        }

        /// <summary>
        /// 宏替换结果
        /// </summary>
        private class ReplacementResult
        {
            /// <summary>
            /// 替换后的内容
            /// </summary>
            public string Content { get; set; }

            /// <summary>
            /// 内容是否发生变化
            /// </summary>
            public bool Changed { get; set; }

            /// <summary>
            /// 总替换次数
            /// </summary>
            public int ReplacementCount { get; set; }

            /// <summary>
            /// 替换统计信息，键为宏名称，值为替换次数
            /// </summary>
            public Dictionary<string, int> Statistics { get; set; }
        }

        /// <summary>
        /// 单次宏替换结果
        /// </summary>
        private class SingleReplacementResult
        {
            /// <summary>
            /// 替换后的内容
            /// </summary>
            public string Content { get; set; }

            /// <summary>
            /// 内容是否发生变化
            /// </summary>
            public bool Changed { get; set; }

            /// <summary>
            /// 替换次数
            /// </summary>
            public int ReplacementCount { get; set; }
        }

        #endregion
    }

    /// <summary>
    /// 宏替换异常
    /// </summary>
    public class CppMacroReplacementException : Exception
    {
        public CppMacroReplacementException(string message) : base(message) { }
        public CppMacroReplacementException(string message, Exception innerException) : base(message, innerException) { }
    }
}