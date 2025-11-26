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
            // 1. 处理继承、实现关系。
            if (model.Generalizations != null)
            {
                // 如果TargetName和model.Name相同，移除该关系
                model.Generalizations = model.Generalizations
                    .Where(b => !string.Equals(b.TargetName, model.Name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            if (model.Realizations != null)
            {
                // 如果TargetName和model.Name相同，移除该关系
                model.Realizations = model.Realizations
                    .Where(b => !string.Equals(b.TargetName, model.Name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // 2. 处理单向关联关系。
            if (model.UnidirectionalAssociations != null)
            {
                // 获取所有需要移除的关系
                var UnidirectionalAssociationsToRemove = model.UnidirectionalAssociations
                    .Where(a =>
                        // 条件1：TargetName和model.Name相同
                        string.Equals(a.TargetName, model.Name, StringComparison.OrdinalIgnoreCase) ||
                        // 条件2：TargetName等于属性中的Type/CustomType且TargetRoleName等于属性中的Name
                        (model.Properties != null && model.Properties.Any(p =>
                            (string.Equals(p.Type, a.TargetName, StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(p.CustomType, a.TargetName, StringComparison.OrdinalIgnoreCase)) &&
                            string.Equals(p.Name, a.TargetRoleName, StringComparison.OrdinalIgnoreCase)))
                    )
                    .ToList();

                // 移除匹配的关系
                foreach (var UnidirectionalAssociationToRemove in UnidirectionalAssociationsToRemove)
                {
                    model.UnidirectionalAssociations.Remove(UnidirectionalAssociationToRemove);
                }
            }

            // 3. 处理依赖关系。
            if (model.Dependencies != null)
            {
                // 如果TargetName和model.Name相同，移除该关系
                model.Dependencies = model.Dependencies
                    .Where(d => !string.Equals(d.TargetName, model.Name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // 4. 处理聚合、组合关系。
            if (model.Compositions != null)
            {
                // 如果TargetName和model.Name相同，移除该关系
                var compositionsToRemove = model.Compositions
                    .Where(a =>
                        // 条件1：TargetName和model.Name相同
                        string.Equals(a.TargetName, model.Name, StringComparison.OrdinalIgnoreCase) ||
                        // 条件2：TargetName等于属性中的Type/CustomType且TargetRoleName等于属性中的Name
                        (model.Properties != null && model.Properties.Any(p =>
                            (string.Equals(p.Type, a.TargetName, StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(p.CustomType, a.TargetName, StringComparison.OrdinalIgnoreCase)) &&
                            string.Equals(p.Name, a.TargetRoleName, StringComparison.OrdinalIgnoreCase)))
                    )
                    .ToList();
                // 移除匹配的关系
                foreach (var compositionToRemove in compositionsToRemove)
                {
                    model.Compositions.Remove(compositionToRemove);
                }
            }
            if (model.Aggregations != null)
            {
                // 如果TargetName和model.Name相同，移除该关系
                var aggregationsToRemove = model.Aggregations
                   .Where(a =>
                        // 条件1：TargetName和model.Name相同
                        string.Equals(a.TargetName, model.Name, StringComparison.OrdinalIgnoreCase) ||
                        // 条件2：TargetName等于属性中的Type/CustomType且TargetRoleName等于属性中的Name
                        (model.Properties != null && model.Properties.Any(p =>
                            (string.Equals(p.Type, a.TargetName, StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(p.CustomType, a.TargetName, StringComparison.OrdinalIgnoreCase)) &&
                            string.Equals(p.Name, a.TargetRoleName, StringComparison.OrdinalIgnoreCase)))
                    )
                    .ToList();
                // 移除匹配的关系
                foreach (var aggregationToRemove in aggregationsToRemove)
                {
                    model.Aggregations.Remove(aggregationToRemove);
                }
            }

            // 5、处理关联关系。
            if (model.Associations != null)
            {
                // 获取所有需要移除的关系，SourceName等于当前model的Name
                var associationsToRemove = model.Associations
                    .Where(a =>
                        // 先检查SourceName是否等于当前model的Name
                        string.Equals(a.SourceName, model.Name, StringComparison.OrdinalIgnoreCase) &&
                        // 条件1：TargetName等于属性中的Type/CustomType且TargetRoleName等于属性中的Name
                        (model.Properties != null && model.Properties.Any(p =>
                            (string.Equals(p.Type, a.TargetName, StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(p.CustomType, a.TargetName, StringComparison.OrdinalIgnoreCase)) &&
                            string.Equals(p.Name, a.TargetRoleName, StringComparison.OrdinalIgnoreCase)))
                    )
                    .ToList();
                // 移除匹配的关系
                foreach (var associationToRemove in associationsToRemove)
                {
                    model.Associations.Remove(associationToRemove);
                }

                // 获取所有需要移除的关系，Target等于当前model的Name
                associationsToRemove = model.Associations
                    .Where(a =>
                        // 先检查TargetName是否等于当前model的Name
                        string.Equals(a.TargetName, model.Name, StringComparison.OrdinalIgnoreCase) &&
                        // 条件1：SourceName等于属性中的Type/CustomType且SourceRoleName等于属性中的Name
                        (model.Properties != null && model.Properties.Any(p =>
                            (string.Equals(p.Type, a.SourceName, StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(p.CustomType, a.SourceName, StringComparison.OrdinalIgnoreCase)) &&
                            string.Equals(p.Name, a.SourceRoleName, StringComparison.OrdinalIgnoreCase)))
                    )
                    .ToList();
                // 移除匹配的关系
                foreach (var associationToRemove in associationsToRemove)
                {
                    model.Associations.Remove(associationToRemove);
                }
            }
        }


        public CodeEnum ProcessEnum(CodeEnum model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            model.Name = SanitizeName(model.Name, "UnnamedEnum");

            // 处理枚举值字典
            if (model.ValueList != null)
            {
                var cleanedValueList = new List<CodeEnumValue>();
                for (int i = 0; i < model.ValueList.Count; i++)
                {
                    var enumValue = model.ValueList[i];
                    if (enumValue == null) continue;
                    var cleanedKey = CleanupEnumKey(enumValue.Name);
                    if (cleanedKey == null) continue; // 跳过无效的键
                    var cleanedValue = CleanupEnumValue(enumValue.Label);
                    cleanedValueList.Add(new CodeEnumValue
                    {
                        Name = cleanedKey,
                        Label = cleanedValue,
                        Comment = enumValue.Comment // 保留注释
                    });
                }
                model.ValueList = cleanedValueList;
            }
            else
            {
                model.ValueList = new List<CodeEnumValue>();
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
                // 处理默认值的格式化
                FormatPropertyDefaultValue(property);
            }
        }

        /// <summary>
        /// 格式化属性的默认值（根据类型添加引号等）
        /// </summary>
        /// <param name="property">要处理的属性</param>
        private static void FormatPropertyDefaultValue(CodeProperty property)
        {
            if (property == null || string.IsNullOrWhiteSpace(property.DefaultValue))
                return;

            string defaultValue = property.DefaultValue.Trim();

            if (property.Type == "string" || property.Type == "std::string")
            {
                property.DefaultValue = $"\"{defaultValue}\"";
            }
            else if (property.Type == "char")
            {
                property.DefaultValue = $"\'{defaultValue}\'";
            }
        }

        /// <summary>
        /// 处理方法，设置默认可见性为 public
        /// </summary>
        /// <param name="model"></param>
        private static void ProcessMethods(CodeClass model)
        {
            if (model.Methods == null) return;

            // 删除重复的方法（同名且参数列表完全一样）
            RemoveDuplicateMethods(model);

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
                        FormatPropertyDefaultValue(parameter);
                    }
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