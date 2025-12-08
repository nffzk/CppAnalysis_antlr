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
                var result = PerformRecursiveMacroExpansion(headerContent, macros, options, 0);
                return result.Content;
            }
            catch (Exception ex)
            {
                if (options.StrictMode)
                    throw new CppMacroReplacementException("宏替换失败", ex);

                return headerContent;
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

            var result = new ExpansionResult
            {
                Content = content,
                RecursionDepth = currentDepth,
                TotalReplacements = 0
            };

            bool changed;
            int iteration = 0;
            const int maxIterations = 10;

            // 构建宏依赖图，确定替换顺序
            var dependencyGraph = BuildMacroDependencyGraph(macros);
            var replacementOrder = GetMacroReplacementOrder(dependencyGraph, macros);

            do
            {
                changed = false;
                iteration++;

                if (iteration > maxIterations)
                {
                    // 达到最大迭代次数，直接返回当前结果，防止无限循环
                    return result;
                }

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
        /// 构建宏依赖图，用于确定替换顺序
        /// </summary>
        private Dictionary<string, HashSet<string>> BuildMacroDependencyGraph(CppMacroDefinitionCollection macros)
        {
            var graph = new Dictionary<string, HashSet<string>>();

            foreach (var macro in macros.Macros.Values)
            {
                graph[macro.Name] = new HashSet<string>();

                // 查找当前宏值中引用的其他宏
                var referencedMacros = FindReferencedMacros(macro.Value, macros, macro.Name);
                foreach (var refMacro in referencedMacros)
                {
                    // 只添加不同的宏作为依赖
                    if (refMacro != macro.Name)
                    {
                        graph[macro.Name].Add(refMacro);
                    }
                }
            }

            return graph;
        }

        /// <summary>
        /// 在文本中查找引用的宏（改进版本）
        /// </summary>
        private HashSet<string> FindReferencedMacros(string text, CppMacroDefinitionCollection macros, string currentMacroName = null)
        {
            var referenced = new HashSet<string>();

            if (string.IsNullOrEmpty(text))
                return referenced;

            foreach (var macro in macros.Macros.Values)
            {
                // 排除自身引用
                if (macro.Name == currentMacroName)
                    continue;

                // 更精确的宏引用检测
                if (IsMacroReferenced(text, macro))
                {
                    referenced.Add(macro.Name);
                }
            }

            return referenced;
        }

        /// <summary>
        /// 精确检测宏是否被引用
        /// </summary>
        private bool IsMacroReferenced(string text, CppMacroDefinition macro)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(macro.Name))
                return false;

            // 使用正则表达式精确匹配宏名称
            string pattern;
            if (macro.IsFunctionLike)
            {
                // 函数式宏：MACRO( 或 MACRO 后跟非字母数字
                pattern = $@"\b{Regex.Escape(macro.Name)}(?=\s*\()";
            }
            else
            {
                // 对象式宏：单词边界
                pattern = $@"\b{Regex.Escape(macro.Name)}\b";
            }

            return Regex.IsMatch(text, pattern);
        }

        /// <summary>
        /// 根据依赖关系获取宏替换顺序（改进的拓扑排序）
        /// </summary>
        private List<string> GetMacroReplacementOrder(Dictionary<string, HashSet<string>> dependencyGraph, CppMacroDefinitionCollection macros)
        {
            try
            {
                // 先尝试拓扑排序
                var order = new List<string>();
                var visited = new HashSet<string>();
                var temp = new HashSet<string>();

                foreach (var macro in macros.Macros.Values)
                {
                    if (!visited.Contains(macro.Name))
                    {
                        TopologicalSort(macro.Name, dependencyGraph, visited, temp, order);
                    }
                }

                // 反转得到正确的顺序（依赖者在前）
                order.Reverse();
                return order;
            }
            catch (CppMacroReplacementException ex)
            {
                // 如果检测到循环依赖，使用备用排序策略
                return GetFallbackReplacementOrder(macros, dependencyGraph);
            }
        }

        /// <summary>
        /// 改进的拓扑排序，容忍合法的宏嵌套
        /// </summary>
        private void TopologicalSort(string macroName, Dictionary<string, HashSet<string>> graph,
            HashSet<string> visited, HashSet<string> temp, List<string> result)
        {
            if (temp.Contains(macroName))
            {
                // 检查是否是真正的循环依赖还是合法的嵌套
                if (IsValidMacroNesting(macroName, graph, temp))
                {
                    // 合法的宏嵌套，继续处理
                    return;
                }
                else
                {
                    throw new CppMacroReplacementException($"检测到宏循环依赖: {macroName}");
                }
            }

            if (visited.Contains(macroName))
                return;

            visited.Add(macroName);
            temp.Add(macroName);

            if (graph.ContainsKey(macroName))
            {
                foreach (var dependency in graph[macroName])
                {
                    TopologicalSort(dependency, graph, visited, temp, result);
                }
            }

            temp.Remove(macroName);
            result.Add(macroName);
        }

        /// <summary>
        /// 检查是否是合法的宏嵌套（如 MAX 调用 MAX1）
        /// </summary>
        private bool IsValidMacroNesting(string macroName, Dictionary<string, HashSet<string>> graph, HashSet<string> currentPath)
        {
            // 简单的检查：如果依赖链不长，认为是合法嵌套
            if (currentPath.Count <= 3) // 允许3层嵌套
                return true;

            // 检查是否是常见的模式：A 依赖 B，B 不依赖 A 的其他形式
            var pathList = currentPath.ToList();
            int currentIndex = pathList.IndexOf(macroName);

            if (currentIndex > 0)
            {
                // 检查是否形成真正的循环：A->B->A
                string previousMacro = pathList[currentIndex - 1];
                if (graph.ContainsKey(previousMacro) && graph[previousMacro].Contains(macroName))
                {
                    // 真正的循环依赖
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 备用排序策略：处理合法的宏嵌套情况
        /// </summary>
        private List<string> GetFallbackReplacementOrder(CppMacroDefinitionCollection macros, Dictionary<string, HashSet<string>> graph)
        {
            // 策略1：按名称长度降序排列（避免部分匹配）
            var orderByLength = macros.Macros.Values
                .OrderByDescending(m => m.Name.Length)
                .ThenBy(m => m.Name)
                .Select(m => m.Name)
                .ToList();

            // 策略2：尝试根据依赖深度排序
            try
            {
                var depthMap = CalculateMacroDepth(graph);
                var orderByDepth = depthMap.OrderByDescending(kv => kv.Value)
                                          .ThenBy(kv => kv.Key)
                                          .Select(kv => kv.Key)
                                          .ToList();

                // 优先使用深度排序，如果失败则使用长度排序
                return orderByDepth.Count == macros.Macros.Count ? orderByDepth : orderByLength;
            }
            catch
            {
                return orderByLength;
            }
        }

        /// <summary>
        /// 计算宏的依赖深度
        /// </summary>
        private Dictionary<string, int> CalculateMacroDepth(Dictionary<string, HashSet<string>> graph)
        {
            var depthMap = new Dictionary<string, int>();
            var visited = new HashSet<string>();

            foreach (var macroName in graph.Keys)
            {
                if (!visited.Contains(macroName))
                {
                    CalculateDepthRecursive(macroName, graph, depthMap, visited, new HashSet<string>());
                }
            }

            return depthMap;
        }

        /// <summary>
        /// 递归计算依赖深度
        /// </summary>
        private int CalculateDepthRecursive(string macroName, Dictionary<string, HashSet<string>> graph,
            Dictionary<string, int> depthMap, HashSet<string> visited, HashSet<string> currentPath)
        {
            if (depthMap.ContainsKey(macroName))
                return depthMap[macroName];

            if (currentPath.Contains(macroName))
            {
                // 检测到可能的循环，返回当前最大深度
                return currentPath.Count;
            }

            currentPath.Add(macroName);
            int maxDepth = 0;

            if (graph.ContainsKey(macroName))
            {
                foreach (var dependency in graph[macroName])
                {
                    int depth = CalculateDepthRecursive(dependency, graph, depthMap, visited, currentPath);
                    maxDepth = Math.Max(maxDepth, depth + 1);
                }
            }

            currentPath.Remove(macroName);
            visited.Add(macroName);
            depthMap[macroName] = maxDepth;

            return maxDepth;
        }

        /// <summary>
        /// 替换单个宏，并递归展开结果中的其他宏
        /// </summary>
        private SingleExpansionResult ReplaceMacroWithExpansion(string content, CppMacroDefinition macro,
            CppMacroDefinitionCollection allMacros, MacroReplacementOptions options, int currentDepth)
        {
            int replacementCount = 0;
            string result = content;

            if (macro.IsFunctionLike)
            {
                result = ReplaceFunctionLikeMacroWithExpansion(content, macro, allMacros, options, ref replacementCount, currentDepth);
            }
            else
            {
                result = ReplaceObjectLikeMacroWithExpansion(content, macro, allMacros, options, ref replacementCount, currentDepth);
            }

            return new SingleExpansionResult
            {
                Content = result,
                Changed = replacementCount > 0,
                ReplacementCount = replacementCount
            };
        }

        /// <summary>
        /// 替换函数式宏，并递归展开结果
        /// </summary>
        private string ReplaceFunctionLikeMacroWithExpansion(string content, CppMacroDefinition macro,
            CppMacroDefinitionCollection allMacros, MacroReplacementOptions options, ref int replacementCount, int currentDepth)
        {
            string pattern = $@"\b{Regex.Escape(macro.Name)}\s*\(([^()]*(?:\((?<depth>)[^()]*\)[^()]*)*)\)";

            int localReplacementCount = 0;

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
                        // 1. 先展开参数中的宏
                        var expandedArguments = new List<string>();
                        for (int i = 0; i < arguments.Count; i++)
                        {
                            string expandedArg = RecursivelyExpandMacros(arguments[i], allMacros, options, currentDepth + 1);
                            expandedArguments.Add(expandedArg);
                        }

                        // 2. 替换参数
                        string replacedValue = macro.Value;
                        for (int i = 0; i < macro.Parameters.Count; i++)
                        {
                            string paramPattern = $@"\b{Regex.Escape(macro.Parameters[i])}\b";
                            replacedValue = Regex.Replace(replacedValue, paramPattern, expandedArguments[i]);
                        }

                        // 3. 递归展开结果中的其他宏
                        string finalValue = RecursivelyExpandMacros(replacedValue, allMacros, options, currentDepth + 1);

                        localReplacementCount++;
                        return finalValue;
                    }
                }
                catch (Exception ex)
                {
                    if (options.StrictMode)
                        throw new CppMacroReplacementException($"函数式宏替换失败: {macro.Name}", ex);
                }

                return match.Value;
            }, RegexOptions.Multiline);

            replacementCount += localReplacementCount;
            return result;
        }

        /// <summary>
        /// 替换对象式宏，并递归展开结果
        /// </summary>
        private string ReplaceObjectLikeMacroWithExpansion(string content, CppMacroDefinition macro,
            CppMacroDefinitionCollection allMacros, MacroReplacementOptions options, ref int replacementCount, int currentDepth)
        {
            string pattern = $@"\b{Regex.Escape(macro.Name)}\b";

            int localReplacementCount = 0;

            string result = Regex.Replace(content, pattern, match =>
            {
                if (ShouldSkipReplacement(match, content, options))
                    return match.Value;

                // 递归展开宏值中的其他宏
                string expandedValue = RecursivelyExpandMacros(macro.Value, allMacros, options, currentDepth + 1);

                localReplacementCount++;
                return expandedValue;
            }, RegexOptions.Multiline);

            replacementCount += localReplacementCount;
            return result;
        }

        /// <summary>
        /// 递归展开文本中的所有宏
        /// </summary>
        private string RecursivelyExpandMacros(string text, CppMacroDefinitionCollection allMacros,
            MacroReplacementOptions options, int currentDepth)
        {
            if (string.IsNullOrEmpty(text) || currentDepth > options.MaxRecursionDepth)
                return text;

            string result = text;
            bool changed;
            int iterations = 0;

            do
            {
                changed = false;
                iterations++;

                if (iterations > 10) // 防止无限循环
                    break;

                foreach (var macro in allMacros.Macros.Values.OrderByDescending(m => m.Name.Length))
                {
                    if (options.ExcludedMacros.Contains(macro.Name))
                        continue;

                    string temp = result;
                    if (macro.IsFunctionLike)
                    {
                        result = ReplaceFunctionLikeMacroSimple(result, macro, options);
                    }
                    else
                    {
                        result = ReplaceObjectLikeMacroSimple(result, macro, options);
                    }

                    if (result != temp)
                    {
                        changed = true;
                        break; // 重新开始循环，因为内容已改变
                    }
                }

            } while (changed);

            return result;
        }

        /// <summary>
        /// 简单的函数式宏替换（不递归展开）
        /// </summary>
        private string ReplaceFunctionLikeMacroSimple(string content, CppMacroDefinition macro, MacroReplacementOptions options)
        {
            string pattern = $@"\b{Regex.Escape(macro.Name)}\s*\(([^()]*(?:\((?<depth>)[^()]*\)[^()]*)*)\)";

            return Regex.Replace(content, pattern, match =>
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
                        for (int i = 0; i < macro.Parameters.Count; i++)
                        {
                            string paramPattern = $@"\b{Regex.Escape(macro.Parameters[i])}\b";
                            replacedValue = Regex.Replace(replacedValue, paramPattern, arguments[i]);
                        }
                        return replacedValue;
                    }
                }
                catch
                {
                    // 在简单替换中忽略错误
                }

                return match.Value;
            }, RegexOptions.Multiline);
        }

        /// <summary>
        /// 简单的对象式宏替换（不递归展开）
        /// </summary>
        private string ReplaceObjectLikeMacroSimple(string content, CppMacroDefinition macro, MacroReplacementOptions options)
        {
            string pattern = $@"\b{Regex.Escape(macro.Name)}\b";

            return Regex.Replace(content, pattern, match =>
            {
                if (ShouldSkipReplacement(match, content, options))
                    return match.Value;

                return macro.Value;
            }, RegexOptions.Multiline);
        }

        /// <summary>
        /// 检查是否应跳过替换（在注释、字符串或宏定义中）
        /// </summary>
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
        private bool IsInString(string content, int position)
        {
            // 简化实现：检查位置之前是否有未配对的引号
            string before = content.Substring(0, position);
            int quoteCount = before.Count(c => c == '"');
            return quoteCount % 2 == 1; // 奇数字符数表示在字符串中
        }

        /// <summary>
        /// 检查位置是否在宏定义中
        /// </summary>
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
        /// 宏展开结果
        /// </summary>
        private class ExpansionResult
        {
            public string Content { get; set; }
            public int RecursionDepth { get; set; }
            public int TotalReplacements { get; set; }
            public Dictionary<string, int> Statistics { get; set; } = new Dictionary<string, int>();
        }

        /// <summary>
        /// 单次宏展开结果
        /// </summary>
        private class SingleExpansionResult
        {
            public string Content { get; set; }
            public bool Changed { get; set; }
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