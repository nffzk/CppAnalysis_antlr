using System;
using System.IO;
using System.Linq;
using CppParser.Services;
using CppParser.Models;
using CppParser.Services.Implementation;

class Program
{
    static void Main(string[] args)
    {
        // 测试文件路径
        string testHeaderPath = @"D:\work\learn\tools\vs\CppAnalysis_antlr\CppParser\Demo\MyClass5.h";

        if (!File.Exists(testHeaderPath))
        {
            Console.WriteLine($"Test file not found: {testHeaderPath}");
            Console.WriteLine("Please create a test C++ header file first.");
            return;
        }

        try
        {
            var parser = new CppHeaderParser();
            var headerFile = parser.ParseHeaderFile(testHeaderPath);

            DisplayHeaderFileInfo(headerFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing header file: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    static void DisplayHeaderFileInfo(CodeHeaderFile headerFile)
    {
        Console.WriteLine($"=== C++ Header File Analysis ===");
        Console.WriteLine($"File: {headerFile.FileName}");
        Console.WriteLine();

        // 显示枚举
        if (headerFile.Enums.Any())
        {
            Console.WriteLine("ENUMS:");
            foreach (var enumItem in headerFile.Enums)
            {
                Console.WriteLine($"  {enumItem.Name} {(enumItem.IsScoped ? "(scoped)" : "")}");
                if (!string.IsNullOrEmpty(enumItem.UnderlyingType))
                    Console.WriteLine($"    Underlying Type: {enumItem.UnderlyingType}");
                if (enumItem.Values.Any())
                    Console.WriteLine($"    Values: {string.Join(", ", enumItem.Values)}");
                Console.WriteLine();
            }
        }

        // 显示类
        if (headerFile.Classes.Any())
        {
            Console.WriteLine("CLASSES/STRUCTS:");
            foreach (var classItem in headerFile.Classes)
            {
                Console.WriteLine($"  {classItem.Stereotype} {classItem.Name}");

                // 显示继承关系
                if (classItem.Generalizations.Any())
                {
                    Console.WriteLine("    Inherits from:");
                    foreach (var baseClass in classItem.Generalizations)
                    {
                        Console.WriteLine($"      {baseClass.TargetName}");
                    }
                }

                // 显示属性
                if (classItem.Properties.Any())
                {
                    Console.WriteLine("    Properties:");
                    foreach (var property in classItem.Properties)
                    {
                        var modifiers = new System.Collections.Generic.List<string>();
                        if (property.IsStatic) modifiers.Add("static");
                        var type = property.Type == string.Empty ? property.CustomType : property.Type;
                        var modText = modifiers.Any() ? $" [{string.Join(" ", modifiers)}]" : "";
                        Console.WriteLine($"      {property.Visibility} {type} {property.Name}{modText}");
                        if (!string.IsNullOrEmpty(property.DefaultValue))
                            Console.WriteLine($"        Default: {property.DefaultValue}");
                    }
                }

                // 显示方法
                if (classItem.Methods.Any())
                {
                    Console.WriteLine("    Methods:");
                    foreach (var method in classItem.Methods)
                    {
                        var modifiers = new System.Collections.Generic.List<string>();
                        if (method.IsStatic) modifiers.Add("static");
                        if (method.IsVirtual) modifiers.Add("virtual");
                        if (method.IsPureVirtual) modifiers.Add("= 0");

                        var modText = modifiers.Any() ? $" [{string.Join(" ", modifiers)}]" : "";

                        // 这里输出参数类型的时候，如果p.Type为string.empty，则使用p.CustomType
                        var parameters = method.Parameters.Any()
                            ? string.Join(", ", method.Parameters.Select(p => $"{(p.Type == string.Empty ? p.CustomType : p.Type)} {p.Name}"))
                            : "";


                        var returnType = method.ReturnType == string.Empty ? method.CustomReturnType : method.ReturnType;
                        Console.WriteLine($"      {method.Visibility} {returnType} {method.Name}({parameters}){modText}");
                    }
                }

                // 显示嵌套枚举
                if (classItem.Enums.Any())
                {
                    Console.WriteLine("    Nested Enums:");
                    foreach (var enumItem in classItem.Enums)
                    {
                        Console.WriteLine($"  {enumItem.Name} {(enumItem.IsScoped ? "(scoped)" : "")}");
                        if (!string.IsNullOrEmpty(enumItem.UnderlyingType))
                            Console.WriteLine($"    Underlying Type: {enumItem.UnderlyingType}");
                        if (enumItem.Values.Any())
                            Console.WriteLine($"    Values: {string.Join(", ", enumItem.Values)}");
                        Console.WriteLine();
                    }
                }

                Console.WriteLine();
            }
        }

        if (!headerFile.Enums.Any() && !headerFile.Classes.Any())
        {
            Console.WriteLine("No classes or enums found in the header file.");
        }
    }
}