using System;
using System.Collections.Generic;
using System.Linq;
using CppParser.Enums;
using CppParser.Models;

namespace CppGenerator.Services
{
    /// <summary>
    /// C++ 模型预处理器实现
    /// </summary>
    public sealed class CppModelPreprocessor : ICppModelPreprocessor
    {
        /// <summary>
        /// UML基础数据类型到C++类型的映射字典
        /// </summary>
        private static readonly Dictionary<string, string> _typeMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Boolean"] = "bool",
            ["Real"] = "double",
            ["String"] = "std::string",
            ["Double"] = "double",
            ["Float"] = "float",
            ["Char"] = "char",
            ["Long"] = "long",
            ["Short"] = "short",
            ["Byte"] = "std::byte",
            ["Integer"] = "int"
        };

        public CodeClass ProcessClass(CodeClass model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            // 1. 基础清洗和验证
            model.Name = SanitizeName(model.Name, "Unnamed");

            // 2. 处理属性、方法和关系
            ProcessProperties(model);
            ProcessMethods(model);
            ProcessRelationship(model);

            // 3. 排序
            SortMethods(model);
            SortProperties(model);

            // 4. 特殊类型处理（接口）
            ProcessInterfaceSpecifics(model);

            return model;
        }

        /// <summary>
        /// 处理类的关系
        /// </summary>
        /// <param name="model"></param>
        private static void ProcessRelationship(CodeClass model)
        {
            // 1. 处理继承、实现关系。如果TargetName和model.Name相同，移除该关系
            if (model.Generalizations != null)
            {
                model.Generalizations = model.Generalizations
                    .Where(b => !string.Equals(b.TargetName, model.Name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            if (model.Realizations != null)
            {
                model.Realizations = model.Realizations
                    .Where(b => !string.Equals(b.TargetName, model.Name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // 2. 处理单向关联关系。如果TargetName和model.Name相同，移除该关系
            if (model.UnidirectionalAssociations != null)
            {
                model.UnidirectionalAssociations = model.UnidirectionalAssociations
                    .Where(a => !string.Equals(a.TargetName, model.Name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // 3. 处理依赖关系。如果TargetName和model.Name相同，移除该关系
            if (model.Dependencies != null)
            {
                model.Dependencies = model.Dependencies
                    .Where(d => !string.Equals(d.TargetName, model.Name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // 4. 处理聚合、组合关系。如果TargetName和model.Name相同，移除该关系
            if (model.Associations != null)
            {
                model.Associations = model.Associations
                    .Where(a => !string.Equals(a.TargetName, model.Name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }


        public CodeEnum ProcessEnum(CodeEnum model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            model.Name = SanitizeName(model.Name, "UnnamedEnum");

            // 处理枚举值字典
            if (model.Values != null)
            {
                var cleanedValues = new Dictionary<string, string>();

                foreach (var kvp in model.Values)
                {
                    // 清理键（枚举值）
                    var cleanedKey = CleanupEnumKey(kvp.Key);
                    if (string.IsNullOrWhiteSpace(cleanedKey))
                    {
                        continue; // 跳过无效的键
                    }

                    // 清理值（中文名称）
                    var cleanedValue = CleanupEnumValue(kvp.Value);

                    // 避免重复键（使用不区分大小写的比较）
                    if (!cleanedValues.Keys.Any(k =>
                        string.Equals(k, cleanedKey, StringComparison.OrdinalIgnoreCase)))
                    {
                        cleanedValues[cleanedKey] = cleanedValue;
                    }
                }

                model.Values = cleanedValues;
            }
            else
            {
                model.Values = new Dictionary<string, string>();
            }

            // 处理底层类型
            if (string.IsNullOrWhiteSpace(model.UnderlyingType))
            {
                model.UnderlyingType = "int"; // 默认底层类型
            }
            else
            {
                model.UnderlyingType = model.UnderlyingType.Trim();
            }

            return model;
        }

        #region Private Helper Methods

        /// <summary>
        /// 清理名称，如果为空则使用默认名称
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultName"></param>
        /// <returns></returns>
        private static string SanitizeName(string name, string defaultName)
        {
            return string.IsNullOrWhiteSpace(name) ? defaultName : name.Trim();
        }

        /// <summary>
        /// 清理枚举键（枚举值），确保是有效的C++标识符
        /// </summary>
        private static string CleanupEnumKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            var cleaned = key.Trim();

            // 确保枚举键是有效的C++标识符
            if (!IsValidCppIdentifier(cleaned))
            {
                // 如果无效，尝试清理或使用默认名称
                cleaned = CleanupInvalidIdentifier(cleaned);
            }

            return cleaned;
        }

        /// <summary>
        /// 清理枚举值（中文名称），去除多余空白
        /// </summary>
        private static string CleanupEnumValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value.Trim();
        }

        /// <summary>
        /// 检查是否为有效的C++标识符
        /// </summary>
        private static bool IsValidCppIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return false;

            // 首字符必须是字母或下划线
            if (!char.IsLetter(identifier[0]) && identifier[0] != '_')
                return false;

            // 后续字符可以是字母、数字或下划线
            for (int i = 1; i < identifier.Length; i++)
            {
                if (!char.IsLetterOrDigit(identifier[i]) && identifier[i] != '_')
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 清理无效的标识符
        /// </summary>
        private static string CleanupInvalidIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return "INVALID";

            // 1、移除无效字符，只保留字母、数字和下划线
            var validChars = identifier.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray();
            var cleaned = new string(validChars);

            // 2、如果首字符不是字母或下划线，添加下划线前缀
            if (cleaned.Length > 0 && !char.IsLetter(cleaned[0]) && cleaned[0] != '_')
            {
                cleaned = "_" + cleaned;
            }

            // 3、如果清理后为空，使用默认名称
            if (string.IsNullOrEmpty(cleaned))
            {
                cleaned = "ENUM_VALUE";
            }

            return cleaned;
        }

        /// <summary>
        /// 处理属性，设置默认可见性为 private。将c#中的类型替换为cpp类型
        /// </summary>
        /// <param name="model"></param>
        private static void ProcessProperties(CodeClass model)
        {
            if (model.Properties == null) return;

            foreach (var property in model.Properties)
            {
                if (property.Visibility == EnumVisibility.None)
                    property.Visibility = EnumVisibility.Private;

                // 替换属性类型
                if (!string.IsNullOrWhiteSpace(property.Type))
                {
                    property.Type = MapTypeToCpp(property.Type.Trim());
                }
            }
        }

        /// <summary>
        /// 处理方法，设置默认可见性为 public
        /// </summary>
        /// <param name="model"></param>
        private static void ProcessMethods(CodeClass model)
        {
            if (model.Methods == null) return;

            foreach (var method in model.Methods)
            {
                if (method.Visibility == EnumVisibility.None)
                    method.Visibility = EnumVisibility.Public;

                // 替换返回类型
                if (!string.IsNullOrWhiteSpace(method.ReturnType))
                {
                    method.ReturnType = MapTypeToCpp(method.ReturnType.Trim());
                }

                // 替换参数类型
                if (method.Parameters != null)
                {
                    foreach (var parameter in method.Parameters)
                    {
                        if (!string.IsNullOrWhiteSpace(parameter.Type))
                        {
                            parameter.Type = MapTypeToCpp(parameter.Type.Trim());
                        }
                    }
                }

            }
        }

        /// <summary>
        /// 排序方法，静态方法排在前面
        /// </summary>
        /// <param name="model"></param>
        private static void SortMethods(CodeClass model)
        {
            if (model.Methods == null) return;

            // 静态方法排在前面
            model.Methods = model.Methods
                .OrderBy(m => m.IsStatic ? 1 : 0)
                .ToList();
        }

        /// <summary>
        /// 排序属性，静态属性排在前面
        /// </summary>
        /// <param name="model"></param>
        private static void SortProperties(CodeClass model)
        {
            if (model.Properties == null) return;

            //  静态属性排在前面
            model.Properties = model.Properties
                .OrderBy(p => p.IsStatic ? 1 : 0)
                .ToList();
        }

        /// <summary>
        /// 处理接口特性，确保所有方法为纯虚函数
        /// </summary>
        /// <param name="model"></param>
        private static void ProcessInterfaceSpecifics(CodeClass model)
        {
            if (model.Stereotype != EnumClassType.Interface) return;

            if (model.Methods != null)
            {
                foreach (var method in model.Methods)
                {
                    if (!method.IsPureVirtual)
                        method.IsPureVirtual = true;
                }
            }
        }

        /// <summary>
        /// 将UML基础数据类型映射为C++类型
        /// </summary>
        /// <param name="originalType">原始类型名称</param>
        /// <returns>映射后的C++类型</returns>
        private static string MapTypeToCpp(string originalType)
        {
            if (string.IsNullOrWhiteSpace(originalType))
                return originalType;

            // 检查是否是已知的基础数据类型
            if (_typeMapping.ContainsKey(originalType))
            {
                return _typeMapping[originalType];
            }

            // 如果不是已知的基础类型，保持原样（可能是自定义类型）
            return originalType;
        }

        #endregion
    }
}