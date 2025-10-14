// Program.cs
using System;
using System.IO;
using System.Linq;
using System.Text;
using CppParser.Services;
using CppParser.Models;

class Program
{
    static void Main()
    {
        string filePath = "D:\\work\\learn\\tools\\vs\\CppAnalysis_antlr\\CppParser\\Demo\\MyClass2.h";

        if (!File.Exists(filePath))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[Warn] File not found: {filePath}");
            Console.ResetColor();
            return;
        }

        try
        {
            var source = File.ReadAllText(filePath, Encoding.UTF8);

            var parser = new HeaderParser();
            var model = parser.BuildHeaderModel(Path.GetFileName(filePath), source);

            PrintHeader(model);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[Error] Failed to parse header:");
            Console.WriteLine(ex.ToString());
            Console.ResetColor();
        }
    }

    static void PrintHeader(CppHeaderFile header)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"=== Header: {header.FileName} ===");
        Console.ResetColor();

        // 顶层枚举
        if (header.Enums?.Any() == true)
        {
            Console.WriteLine("Enums:");
            foreach (var e in header.Enums)
            {
                Console.WriteLine($"  - {e.Name}  (Scoped={e.IsScoped}, UnderlyingType={e.UnderlyingType ?? "N/A"})");
                if (e.Values?.Any() == true)
                {
                    Console.WriteLine($"      Values: {string.Join(", ", e.Values)}");
                }
            }
        }

        // 顶层类/结构/联合
        if (header.Classes?.Any() == true)
        {
            Console.WriteLine("Types:");
            foreach (var c in header.Classes)
            {
                PrintClass(c, indent: 0);
            }
        }
    }

    static void PrintClass(CppClass c, int indent)
    {
        var pad = new string(' ', indent * 2);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"{pad}- {c.Stereotype} {c.Name}");
        Console.ResetColor();

        if (c.BaseClasses?.Any() == true)
        {
            Console.WriteLine($"{pad}  Bases: {string.Join(", ", c.BaseClasses)}");
        }

        // 类内枚举
        if (c.Enums?.Any() == true)
        {
            Console.WriteLine($"{pad}  Enums:");
            foreach (var e in c.Enums)
            {
                Console.WriteLine($"{pad}    - {e.Name}  (Scoped={e.IsScoped}, UnderlyingType={e.UnderlyingType ?? "N/A"})");
                if (e.Values?.Any() == true)
                {
                    Console.WriteLine($"{pad}        Values: {string.Join(", ", e.Values)}");
                }
            }
        }

        // 成员变量
        if (c.Properties?.Any() == true)
        {
            Console.WriteLine($"{pad}  Properties:");
            foreach (var p in c.Properties)
            {
                var flags = string.Join(", ", new[]
                {
                    p.IsStatic ? "static" : null,
                    p.IsConst ? "const" : null,
                    p.IsVolatile ? "volatile" : null,
                    p.IsPointer ? "*" : null,
                    p.IsReference ? "&" : null,
                    p.IsArray ? "[]" : null,
                    p.IsSigned ? "signed" : null,
                    p.IsUnsigned ? "unsigned" : null,
                    p.IsShort ? "short" : null,
                    p.IsLong ? "long" : null,
                    p.IsMutable ? "mutable" : null
                }.Where(s => s != null));

                var typeText = !string.IsNullOrWhiteSpace(p.FullType) ? p.FullType : p.Type ?? "(unknown)";
                Console.WriteLine($"{pad}    - [{p.Visibility}] {typeText} {p.Name}{(p.IsArray ? $"[{p.ArraySize}]" : "")}{(string.IsNullOrEmpty(p.DefaultValue) ? "" : $" = {p.DefaultValue}")}{(string.IsNullOrEmpty(flags) ? "" : $"  {{{flags}}}")}");
            }
        }

        // 成员函数
        if (c.Methods?.Any() == true)
        {
            Console.WriteLine($"{pad}  Methods:");
            foreach (var m in c.Methods)
            {
                var returnType = new StringBuilder();
                if (m.IsReturnConst) returnType.Append("const ");
                returnType.Append(string.IsNullOrWhiteSpace(m.ReturnType) ? "/*ctor/dtor/operator*/" : m.ReturnType);
                if (m.ReturnTypeIsPointer) returnType.Append(" *");
                if (m.ReturnTypeIsReference) returnType.Append(" &");

                var paramList = (m.Parameters == null || m.Parameters.Count == 0)
                    ? ""
                    : string.Join(", ", m.Parameters.Select(par =>
                    {
                        var pt = !string.IsNullOrWhiteSpace(par.FullType) ? par.FullType : par.Type ?? "(unknown)";
                        var rref = par.IsRValueReference ? "&&" : "";
                        var arr = par.IsArray ? $"[{par.ArraySize}]" : "";
                        return $"{pt}{(par.IsPointer ? " *" : "")}{(par.IsReference ? " &" : "")}{rref} {par.Name}{arr}";
                    }));

                var flags = string.Join(", ", new[]
                {
                    m.IsInline ? "inline" : null,
                    m.IsStatic ? "static" : null,
                    m.IsExplicit ? "explicit" : null,
                    m.IsFriend ? "friend" : null,
                    m.IsConstexpr ? "constexpr" : null,
                    m.IsVirtual ? "virtual" : null,
                    m.IsPureVirtual ? "pure-virtual" : null,
                    m.IsConst ? "const" : null,
                    m.IsDefaultImplementation ? "default" : null,
                    m.IsDeleted ? "delete" : null,
                    m.IsOverride ? "override" : null,
                    m.IsFinal ? "final" : null
                }.Where(s => s != null));

                Console.WriteLine($"{pad}    - [{m.Visibility}] {returnType} {m.Name}({paramList}){(string.IsNullOrEmpty(flags) ? "" : $"  {{{flags}}}")}");
            }
        }

        // 如果后续你扩展了“类内嵌套类集合”，这里可递归打印
        // foreach (var inner in c.Classes) { PrintClass(inner, indent + 1); }
    }
}
