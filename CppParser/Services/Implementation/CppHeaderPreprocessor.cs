using CppParser.Enums;
using CppParser.Models;
using CppParser.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CppParser.Services.Implementation
{
    /// <summary>
    /// 头文件预处理器
    /// </summary>
    public class CppHeaderPreprocessor : ICppHeaderPreprocessor
    {
        /// <summary>
        /// C++类型到UML基础数据类型的映射字典
        /// </summary>
        private static readonly Dictionary<string, string> _typeMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["bool"] = "Boolean",
            ["double"] = "Real",
            ["std::string"] = "String",
            ["string"] = "String",
            ["double"] = "Double",
            ["float"] = "Float",
            ["char"] = "Char",
            ["long"] = "Long",
            ["short"] = "Short",
            ["std::byte"] = "Byte",
            ["byte"] = "Byte",
            ["int"] = "Integer"
        };

        /// <summary>
        /// 预处理头文件模型
        /// </summary>
        /// <param name="headerFile"></param>
        /// <returns>返回预处理之后的CodeHeaderFile</returns>
        public CodeHeaderFile Preprocess(CodeHeaderFile headerFile)
        {
            if (headerFile == null) return null;

            try
            {
                // 预处理类
                if (headerFile.Classes != null)
                {
                    foreach (var codeClass in headerFile.Classes)
                    {
                        PreprocessClass(codeClass);
                    }
                }

                return headerFile;
            }
            catch (Exception ex)
            {
                // 记录错误但不中断处理
                System.Diagnostics.Debug.WriteLine($"预处理头文件时出错: {ex.Message}");
                return headerFile;
            }
        }

        /// <summary>
        /// 预处理类模型
        /// </summary>
        /// <param name="codeClass"></param>
        private void PreprocessClass(CodeClass codeClass)
        {
            if (codeClass == null) return;

            // 预处理属性
            if (codeClass.Properties != null)
            {
                foreach (var property in codeClass.Properties)
                {
                    PreprocessProperty(property);
                }
            }

            // 预处理方法
            if (codeClass.Methods != null)
            {
                foreach (var method in codeClass.Methods)
                {
                    PreprocessMethod(method);
                }
                // 删除重复的方法
                RemoveDuplicateMethods(codeClass);
            }

            // 获取除了构造函数和析构函数之外的所有方法
            var nonConstructorDestructorMethods = codeClass.Methods
                .Where(m => !IsConstructor(codeClass,m) && !IsDestructor(m))
                .ToList();
            // 统计纯虚方法的数量
            int pureVirtualCount = nonConstructorDestructorMethods
                .Count(m => m.IsPureVirtual);

            int totalMethods = nonConstructorDestructorMethods.Count;

            // 情况1：所有方法都是纯虚函数 -> 设置为interface类型
            if (pureVirtualCount == totalMethods && totalMethods > 0)
            {
                codeClass.Stereotype = EnumClassType.Interface;
            }
            // 情况2：部分方法是纯虚函数 -> 设置为abstract class类型
            else if (pureVirtualCount > 0 && pureVirtualCount < totalMethods)
            {
                codeClass.Stereotype = EnumClassType.AbstractClass;
            }
        }

        /// <summary>
        /// 预处理属性模型
        /// </summary>
        /// <param name="property"></param>
        private void PreprocessProperty(CodeProperty property)
        {
            if (property == null) return;

            // 检查并处理一维数组
            if (IsOneDimensionalArray(property.Type, out string elementType, out string arraySize))
            {
                // 对于一维数组，提取元素类型并设置多重性
                property.Type = elementType;
                property.CustomType = string.Empty;
                property.Multiplicity = new Tuple<string, string>(arraySize, arraySize); // 下限、上限为数组大小
                property.MarkMultiplicity = string.Empty;
            }
            // 如果类型为 vector<T> 或者 std:vector<T> 也算是一维度数组，将多重性设置为 *..*
            else if (IsVectorType(property.Type))
            {
                string type = property.UnderlyingType.FirstOrDefault() ?? string.Empty;
                if(type != null)
                {
                    property.Type = type;
                    property.CustomType = string.Empty;
                }
                // 对于vector类型，提取元素类型并设置多重性为 "*..*"
                property.Multiplicity = new Tuple<string, string>("0", "*"); // 下限0、上限为*
                property.MarkMultiplicity = string.Empty;
            }
            else
            {
                property.Multiplicity = new Tuple<string, string>("1", "1"); // 默认单值属性
                property.MarkMultiplicity = string.Empty;
            }

            // 获取映射后的类型信息
            (property.Type, property.CustomType) = GetMappedTypeInfo(property.Type);

            // 预处理UnderlyingType
            if (property.Type != string.Empty)
            {
                property.UnderlyingType = new List<string> { property.Type };
            }
        }

        /// <summary>
        /// 检查类型是否为vector类型，并提取元素类型
        /// </summary>
        /// <param name="type"></param>
        /// <param name="elementType"></param>
        /// <returns></returns>
        private bool IsVectorType(string type)
        {

            if (string.IsNullOrWhiteSpace(type)) return false;

            // 使用正则表达式匹配vector类型
            var match = System.Text.RegularExpressions.Regex.Match(type.Trim(),
                @"^(std::)?vector\s*<\s*([^>]+)\s*>$");

            if (match.Success)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查类型是否为一维数组，并提取元素类型和数组大小
        /// </summary>
        /// <param name="type"></param>
        /// <param name="elementType"></param>
        /// <param name="arraySize"></param>
        /// <returns></returns>
        private bool IsOneDimensionalArray(string type, out string elementType, out string arraySize)
        {
            elementType = string.Empty;
            arraySize = string.Empty;

            if (string.IsNullOrWhiteSpace(type)) return false;

            // 使用正则表达式匹配一维数组模式：类型[大小]
            var match = System.Text.RegularExpressions.Regex.Match(type.Trim(), @"^([^\[\]]+)\[(\d+)\]$");
            if (match.Success)
            {
                elementType = match.Groups[1].Value.Trim();
                arraySize = match.Groups[2].Value.Trim();

                // 确保不是多维数组（检查是否包含多个方括号）
                int openBracketCount = type.Count(c => c == '[');
                int closeBracketCount = type.Count(c => c == ']');

                return openBracketCount == 1 && closeBracketCount == 1;
            }

            return false;
        }


        /// <summary>
        /// 获取映射后的类型信息
        /// </summary>
        /// <param name="originalType"></param>
        /// <returns></returns>
        private (string type, string customType) GetMappedTypeInfo(string originalType)
        {
            string mappedType = MapTypeToUML(originalType);
            return string.IsNullOrEmpty(mappedType)
                ? (string.Empty, originalType)    // 自定义类型
                : (mappedType, string.Empty);     // 基础类型
        }

        /// <summary>
        /// 预处理方法模型
        /// </summary>
        /// <param name="method"></param>
        private void PreprocessMethod(CodeMethod method)
        {
            if (method == null) return;
            (method.ReturnType, method.CustomReturnType) = GetMappedTypeInfo(method.ReturnType);

            // 预处理UnderlyingReturnType
            if (method.ReturnType != string.Empty)
            {
                method.UnderlyingReturnType = new List<string> { method.ReturnType };
            }

            // 预处理参数
            if (method.Parameters != null)
            {
                foreach (var parameter in method.Parameters)
                {
                    PreprocessParameter(parameter);
                }
            }
        }

        /// <summary>
        /// 删除重复的方法（同名且参数列表完全一样）
        /// </summary>
        /// <param name="model"></param>
        private static void RemoveDuplicateMethods(CodeClass model)
        {
            if (model.Methods == null || model.Methods.Count == 0) return;

            var uniqueMethods = new List<CodeMethod>();
            var seenMethodSignatures = new HashSet<string>();

            foreach (var method in model.Methods)
            {
                // 生成方法的唯一签名：方法名 + 参数类型列表
                string methodSignature = GetMethodSignature(method);

                // 如果这个签名还没有出现过，就保留这个方法
                if (!seenMethodSignatures.Contains(methodSignature))
                {
                    uniqueMethods.Add(method);
                    seenMethodSignatures.Add(methodSignature);
                }
            }

            // 用去重后的列表替换原列表
            model.Methods = uniqueMethods;
        }

        /// <summary>
        /// 生成方法的唯一签名
        /// </summary>
        /// <param name="method"></param>
        /// <returns>方法签名字符串</returns>
        private static string GetMethodSignature(CodeMethod method)
        {
            if (method == null) return string.Empty;

            // 方法名
            string signature = method.Name ?? "unknown";

            // 参数类型列表
            if (method.Parameters != null && method.Parameters.Count > 0)
            {
                var paramTypes = method.Parameters
                    .Select(p =>
                    {
                        // 优先使用Type，如果Type为空则使用CustomType
                        if (!string.IsNullOrWhiteSpace(p.Type))
                            return p.Type.Trim();
                        else if (!string.IsNullOrWhiteSpace(p.CustomType))
                            return p.CustomType.Trim();
                        else
                            return "void"; // 默认类型
                    })
                    .ToArray();
                signature += "(" + string.Join(",", paramTypes) + ")";
            }
            else
            {
                signature += "()";
            }

            return signature;
        }

        /// <summary>
        /// 预处理方法参数模型
        /// </summary>
        /// <param name="parameter"></param>
        private void PreprocessParameter(CodeMethodParameter parameter)
        {
            if (parameter == null) return;
            (parameter.Type, parameter.CustomType) = GetMappedTypeInfo(parameter.Type);

            // 预处理UnderlyingType
            if (parameter.Type != string.Empty)
            {
                parameter.UnderlyingType = new List<string> { parameter.Type };
            }
        }

        /// <summary>
        /// 将C++类型映射为UML基础数据类型
        /// </summary>
        /// <param name="originalType">原始类型名称</param>
        /// <returns>映射后的C++类型，如果不是已知的基础类型返回 string.Empty</returns>
        private static string MapTypeToUML(string originalType)
        {
            if (string.IsNullOrWhiteSpace(originalType))
                return originalType;

            // 检查是否是已知的基础数据类型
            if (_typeMapping.ContainsKey(originalType))
            {
                return _typeMapping[originalType];
            }

            // 如果不是已知的基础类型，返回空字符串表示自定义类型
            return string.Empty;
        }

        /// <summary>
        /// 判断方法是否为构造函数
        /// </summary>
        /// <param name="method">方法对象</param>
        /// <returns>是否为构造函数</returns>
        private bool IsConstructor(CodeClass codeClass,CodeMethod method)
        {
            if (method == null) return false;

            // 构造函数的判断逻辑：
            // 1. 方法名与类名相同（可能需要从上下文获取类名）
            // 2. 没有返回类型
            // 3. 或者是标记为构造函数的特定类型
            return (string.IsNullOrEmpty(method.ReturnType) && string.IsNullOrEmpty(method.CustomReturnType)) ||
                   (method.Name?.Equals(codeClass?.Name, StringComparison.Ordinal) == true);
        }

        /// <summary>
        /// 判断方法是否为析构函数
        /// </summary>
        /// <param name="method">方法对象</param>
        /// <returns>是否为析构函数</returns>
        private bool IsDestructor(CodeMethod method)
        {
            if (method == null) return false;

            // 析构函数的判断逻辑：
            // 1. 方法名以~开头
            // 2. 或者是标记为析构函数的特定类型
            return (method.Name?.StartsWith("~") == true);
        }
    }
}
