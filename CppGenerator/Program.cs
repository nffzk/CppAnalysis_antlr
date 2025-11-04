using System;
using System.Collections.Generic;
using System.IO;
using CppParser.Enums;
using CppParser.Models;
using CppGenerator.Services;

namespace CppGenerator
{
    internal class Program
    {
        static void Main()
        {
            // 0) 依赖组装（模板路径按你的工程实际位置调整）
            var pre = new CppModelPreprocessor();
            var tpl = new CppTemplateProvider(
                classHeaderTemplatePath: @"D:\work\learn\tools\vs\CppAnalysis_antlr\CppGenerator\Templates\class_header.sbn",
                classSourceTemplatePath: @"D:\work\learn\tools\vs\CppAnalysis_antlr\CppGenerator\Templates\class_source.sbn",
                enumHeaderTemplatePath: @"D:\work\learn\tools\vs\CppAnalysis_antlr\CppGenerator\Templates\enum_header.sbn",
                interfaceHeaderTemplatePath: @"D:\work\learn\tools\vs\CppAnalysis_antlr\CppGenerator\Templates\interface_header.sbn",
                structHeaderTemplatePath: @"D:\work\learn\tools\vs\CppAnalysis_antlr\CppGenerator\Templates\class_header.sbn");

            var renderer = new CppCodeRenderer(tpl);
            var generator = new CppCodeGenerator(pre, renderer);

            string outputDir = @"D:\work\learn\tools\vs\CppAnalysis_antlr\CppGenerator\Output";
            Directory.CreateDirectory(outputDir);

            // 1) 生成“类”的示例
            GenerateClassSample(outputDir, generator);

            // 2) 生成“枚举”的示例
            GenerateEnumSample(outputDir, generator);

            // 3) 生成“接口”的示例
            GenerateInterfaceSample(outputDir, generator);

            // 4) 生成“数据类型”的示例
            GenerateStructeSample(outputDir, generator);

            Console.WriteLine("Done.");
        }

        /// <summary>演示：把 UML 中一个“类”生成 .h/.cpp 字符串并写到 Output。</summary>
        private static void GenerateClassSample(string outputDir, ICppCodeGenerator generator)
        {
            var cppClass = new CodeClass
            {
                Name = "Person",
                Stereotype = EnumClassType.Class,
                Properties = new List<CodeProperty>
                {
                    new CodeProperty { Name = "name1", Type = "std::string", Visibility = EnumVisibility.Public,  DefaultValue = "\"Tom\"" },
                    new CodeProperty { Name = "age",  Type = "int",          Visibility = EnumVisibility.Private },
                    new CodeProperty { Name = "name2", Type = "std::string", Visibility = EnumVisibility.Private,  DefaultValue = "\"Tom1\"" },
                },
                Methods = new List<CodeMethod>
                {
                    new CodeMethod { Name = "getName", ReturnType = "std::string", Visibility = EnumVisibility.Public},
                    new CodeMethod {
                        Name = "setName", ReturnType = "void", Visibility = EnumVisibility.Public,
                        Parameters = new List<CodeMethodParameter> {
                            new() { Name = "v", Type = "const std::string&" }
                        }
                    },
                    new CodeMethod { Name = "staticfun", ReturnType = "std::string", Visibility = EnumVisibility.Public , IsStatic = true},

                },
                Generalizations = new List<CodeGeneralization>
                {
                    new CodeGeneralization { Target = "LivingBeing1" },
                    new CodeGeneralization { Target = "LivingBeing2" }
                },

                Realizations = new List<CodeRealization>
                {
                    new CodeRealization { Target = "Realization1" },
                    new CodeRealization { Target = "Realization1" }
                },

                Aggregations = new List<CodeAggregation>
                {
                    new CodeAggregation {Target = "Address", Multiplicity = EnumCppMultiplicity.ToMany }
                },

                Associations = new List<CodeAssociation>
                {
                    new CodeAssociation {Target = "Company", Multiplicity = EnumCppMultiplicity.ToFixed, FixedSize=1, RoleName="employer", Visibility= EnumVisibility.Public }
                }



            };

            var result = generator.GenerateClass(cppClass); // 只返回字符串

            File.WriteAllText(Path.Combine(outputDir, $"{cppClass.Name}.h"), result.HeaderCode);
            File.WriteAllText(Path.Combine(outputDir, $"{cppClass.Name}.cpp"), result.SourceCode);

            Console.WriteLine($"[Class]  Generated: {Path.Combine(outputDir, $"{cppClass.Name}.h")}");
            Console.WriteLine($"[Class]  Generated: {Path.Combine(outputDir, $"{cppClass.Name}.cpp")}");
        }

        /// <summary>演示：把 UML 中一个“枚举”生成 .h 字符串并写到 Output。</summary>
        private static void GenerateEnumSample(string outputDir, ICppCodeGenerator generator)
        {
            var cppEnum = new CodeEnum
            {
                Name = "Color",
                IsScoped = true, // enum class
                // UnderlyingType = "uint8_t",  // 如需要可指定底层类型
                Values = new List<string> { "Red", "Green", "Blue" }
            };

            string enumHeader = generator.GenerateEnum(cppEnum);

            File.WriteAllText(Path.Combine(outputDir, $"{cppEnum.Name}.h"), enumHeader);
            Console.WriteLine($"[Enum]   Generated: {Path.Combine(outputDir, $"{cppEnum.Name}.h")}");
        }

        /// <summary>演示：把 UML 中一个“接口(Interface)”生成 .h 字符串并写到 Output。</summary>
        private static void GenerateInterfaceSample(string outputDir, ICppCodeGenerator generator)
        {
            // 接口用 CppClass 承载，但 Stereotype=Interface，且只包含纯虚函数
            var iface = new CodeClass
            {
                Name = "IShape",
                Stereotype = EnumClassType.Interface,
                Methods = new List<CodeMethod>
                {
                    new CodeMethod { Name = "Area",    ReturnType = "double", Visibility = EnumVisibility.Public, IsPureVirtual = true },
                    new CodeMethod { Name = "Perimeter", ReturnType = "double", Visibility = EnumVisibility.Public,IsVirtual = true}
                }
                // 接口不应含有数据成员；若模型里带了属性，预处理会尽量温和处理/模板会忽略
            };

            string ifaceHeader = generator.GenerateInterface(iface);

            File.WriteAllText(Path.Combine(outputDir, $"{iface.Name}.h"), ifaceHeader);
            Console.WriteLine($"[Iface]  Generated: {Path.Combine(outputDir, $"{iface.Name}.h")}");
        }

        private static void GenerateStructeSample(string outputDir, ICppCodeGenerator generator)
        {
            // 接口用 CppClass 承载，但 Stereotype=Interface，且只包含纯虚函数
            var cppstruct = new CodeClass
            {
                Name = "Node",
                Stereotype = EnumClassType.Struct,
                Properties = new List<CodeProperty>
                {
                    new CodeProperty { Name = "left", Type = "Node", Visibility = EnumVisibility.Public},
                    new CodeProperty { Name = "right", Type = "Node", Visibility = EnumVisibility.Public},
                },

                // 接口不应含有数据成员；若模型里带了属性，预处理会尽量温和处理/模板会忽略
            };

            string cppstrcutHeader = generator.GenerateStruct(cppstruct);

            File.WriteAllText(Path.Combine(outputDir, $"{cppstruct.Name}.h"), cppstrcutHeader);
            Console.WriteLine($"[Iface]  Generated: {Path.Combine(outputDir, $"{cppstruct.Name}.h")}");
        }

    }
}
