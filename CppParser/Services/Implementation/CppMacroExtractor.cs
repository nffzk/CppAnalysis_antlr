using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using CppParser.Models;

namespace CppParser.Services
{
    /// <summary>
    /// 用预处理扫描方式从 C++ 头文件中提取宏定义。
    /// 自动处理：
    /// - 单行注释（//）
    /// - 多行注释（/* */）
    /// - 字符串字面量、字符字面量
    /// - 多行宏（\ 续行符）
    /// - 同一行多个预处理指令
    /// </summary>
    public static class CppMacroExtractor
    {

        /// <summary>
        /// 主入口：从原始代码中提取所有宏
        /// </summary>
        /// <param name="Content"></param>
        /// <returns></returns>
        public static CppMacroDefinitionCollection Extract(string Content)
        {
            var result = new CppMacroDefinitionCollection();

            if (string.IsNullOrEmpty(Content))
                return result;

            // --- STEP 1：预处理，移除注释/字符串（用空格占位保持行号） ---
            string cleaned = RemoveCommentsAndStrings(Content);

            // --- STEP 2：展开多行宏 ---
            List<string> logicalLines = JoinLinesWithBackslash(cleaned);

            // --- STEP 3：提取宏 ---
            foreach (var line in logicalLines)
            {
                var macros = ParseMacrosFromLine(line);
                foreach (var macro in macros)
                {
                    if (macro != null)
                        result.AddMacro(macro);
                }
            }

            return result;
        }


        /// <summary>
        /// STEP 1) 移除注释和字符串字面量（用空格占位以保持行号一致）
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        private static string RemoveCommentsAndStrings(string src)
        {
            var sb = new StringBuilder(src.Length);
            int n = src.Length;

            bool inString = false;
            bool inChar = false;
            bool inLineComment = false;
            bool inBlockComment = false;

            for (int i = 0; i < n; i++)
            {
                char c = src[i];
                char next = (i + 1 < n) ? src[i + 1] : '\0';

                if (inLineComment)
                {
                    if (c == '\n')
                    {
                        inLineComment = false;
                        sb.Append('\n');
                    }
                    else sb.Append(' ');
                    continue;
                }

                if (inBlockComment)
                {
                    if (c == '*' && next == '/')
                    {
                        inBlockComment = false;
                        i++;
                        sb.Append("  ");
                    }
                    else sb.Append(' ');
                    continue;
                }

                if (inString)
                {
                    if (c == '\\')
                    {
                        sb.Append("  ");
                        i++;
                        continue;
                    }
                    if (c == '"')
                    {
                        inString = false;
                        sb.Append(" ");
                    }
                    else sb.Append(" ");
                    continue;
                }

                if (inChar)
                {
                    if (c == '\\')
                    {
                        sb.Append("  ");
                        i++;
                        continue;
                    }
                    if (c == '\'')
                    {
                        inChar = false;
                        sb.Append(" ");
                    }
                    else sb.Append(" ");
                    continue;
                }

                // 进入注释/字符串检查
                if (c == '/' && next == '/')
                {
                    inLineComment = true;
                    sb.Append("  ");
                    i++;
                    continue;
                }

                if (c == '/' && next == '*')
                {
                    inBlockComment = true;
                    sb.Append("  ");
                    i++;
                    continue;
                }

                if (c == '"')
                {
                    inString = true;
                    sb.Append(" ");
                    continue;
                }

                if (c == '\'')
                {
                    inChar = true;
                    sb.Append(" ");
                    continue;
                }

                sb.Append(c);
            }

            return sb.ToString();
        }


        /// <summary>
        /// STEP 2) 处理多行宏定义（续行符 \ 连接的行）
        /// </summary>
        /// <param name="cleaned"></param>
        /// <returns></returns>
        private static List<string> JoinLinesWithBackslash(string cleaned)
        {
            var lines = cleaned.Split('\n');
            var result = new List<string>();
            var sb = new StringBuilder();

            foreach (var line in lines)
            {
                string trimmed = line.TrimEnd();

                if (trimmed.EndsWith("\\"))
                {
                    sb.Append(trimmed.TrimEnd('\\')).Append(" ");
                }
                else
                {
                    sb.Append(trimmed);
                    result.Add(sb.ToString());
                    sb.Clear();
                }
            }

            if (sb.Length > 0)
                result.Add(sb.ToString());

            return result;
        }


        /// <summary>
        /// STEP 3) 从单行中提取宏定义（可能有多个 #define 指令）
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private static List<CppMacroDefinition> ParseMacrosFromLine(string line)
        {
            var macros = new List<CppMacroDefinition>();

            if (string.IsNullOrWhiteSpace(line))
                return macros;

            // 查找所有的 #define 指令
            int searchStart = 0;
            while (searchStart < line.Length)
            {
                int defineIndex = line.IndexOf("#define", searchStart, StringComparison.Ordinal);
                if (defineIndex == -1)
                    break;

                // 找到 #define 后的内容起始位置
                int defineStart = defineIndex + "#define".Length;

                // 查找这个宏定义的结束位置（下一个 # 或行尾）
                int nextDefineIndex = line.IndexOf("#define", defineStart, StringComparison.Ordinal);
                int endOfLine = line.Length;

                int macroEnd = nextDefineIndex != -1 ? nextDefineIndex : endOfLine;

                // 提取宏定义内容
                string macroContent = line.Substring(defineStart, macroEnd - defineStart).Trim();

                // 解析单个宏定义
                var macro = ParseSingleMacroDefinition(macroContent);
                if (macro != null)
                {
                    macros.Add(macro);
                }

                // 移动到下一个搜索位置
                searchStart = macroEnd;
            }

            return macros;
        }


        /// <summary>
        /// 解析单个宏定义
        /// </summary>
        /// <param name="macroContent"></param>
        /// <returns></returns>
        private static CppMacroDefinition? ParseSingleMacroDefinition(string macroContent)
        {
            if (string.IsNullOrWhiteSpace(macroContent))
                return null;

            // 函数式宏？ 例如 LOG(x, y)
            int parenIndex = macroContent.IndexOf('(');
            int spaceIndex = macroContent.IndexOf(' ');

            bool isFunctionLike =
                parenIndex > 0 &&
                (spaceIndex < 0 || parenIndex < spaceIndex);

            if (isFunctionLike)
            {
                return ParseFunctionLikeMacro(macroContent);
            }
            else
            {
                return ParseObjectLikeMacro(macroContent);
            }
        }


        /// <summary>
        /// 解析函数式宏
        /// </summary>
        /// <param name="macroContent"></param>
        /// <returns></returns>
        private static CppMacroDefinition? ParseFunctionLikeMacro(string macroContent)
        {
            int parenIndex = macroContent.IndexOf('(');
            if (parenIndex <= 0)
                return null;

            // 提取宏名称
            string name = macroContent.Substring(0, parenIndex).Trim();
            if (string.IsNullOrWhiteSpace(name))
                return null;

            // 查找匹配的右括号
            int depth = 0;
            int closeIndex = -1;

            for (int i = parenIndex; i < macroContent.Length; i++)
            {
                char c = macroContent[i];
                if (c == '(') depth++;
                else if (c == ')') depth--;

                if (depth == 0)
                {
                    closeIndex = i;
                    break;
                }
            }

            if (closeIndex == -1)
                return null; // 括号不匹配

            // 提取参数部分
            string paramsPart = macroContent.Substring(parenIndex + 1, closeIndex - parenIndex - 1).Trim();

            // 提取值部分（右括号之后的内容）
            string value = "";
            if (closeIndex + 1 < macroContent.Length)
            {
                value = macroContent.Substring(closeIndex + 1).Trim();
            }

            var macro = new CppMacroDefinition
            {
                Name = name,
                Value = value,
                IsFunctionLike = true,
            };

            // 解析参数
            if (!string.IsNullOrWhiteSpace(paramsPart))
            {
                var parameters = ParseMacroParameters(paramsPart);
                foreach (var param in parameters)
                {
                    if (!string.IsNullOrWhiteSpace(param))
                        macro.Parameters.Add(param.Trim());
                }
            }

            return macro;
        }

        /// <summary>
        ///  解析对象式宏
        /// </summary>
        /// <param name="macroContent"></param>
        /// <returns></returns>
        private static CppMacroDefinition? ParseObjectLikeMacro(string macroContent)
        {
            // 查找第一个空格或制表符来分割名称和值
            int separatorIndex = -1;
            for (int i = 0; i < macroContent.Length; i++)
            {
                if (char.IsWhiteSpace(macroContent[i]))
                {
                    separatorIndex = i;
                    break;
                }
            }

            string name, value;

            if (separatorIndex == -1)
            {
                // 没有值，只有名称
                name = macroContent.Trim();
                value = "";
            }
            else
            {
                // 有名称和值
                name = macroContent.Substring(0, separatorIndex).Trim();
                value = macroContent.Substring(separatorIndex + 1).Trim();
            }

            if (string.IsNullOrWhiteSpace(name))
                return null;

            return new CppMacroDefinition
            {
                Name = name,
                Value = value,
                IsFunctionLike = false
            };
        }


        /// <summary>
        /// 解析宏参数（处理逗号分隔，考虑嵌套括号）
        /// </summary>
        /// <param name="paramsText"></param>
        /// <returns></returns>
        private static List<string> ParseMacroParameters(string paramsText)
        {
            var parameters = new List<string>();
            if (string.IsNullOrWhiteSpace(paramsText))
                return parameters;

            int depth = 0;
            int start = 0;

            for (int i = 0; i < paramsText.Length; i++)
            {
                char c = paramsText[i];

                if (c == '(') depth++;
                else if (c == ')') depth--;
                else if (c == ',' && depth == 0)
                {
                    // 找到参数分隔符
                    string param = paramsText.Substring(start, i - start).Trim();
                    if (!string.IsNullOrWhiteSpace(param))
                        parameters.Add(param);
                    start = i + 1;
                }
            }

            // 添加最后一个参数
            string lastParam = paramsText.Substring(start).Trim();
            if (!string.IsNullOrWhiteSpace(lastParam))
                parameters.Add(lastParam);

            return parameters;
        }

    }
}