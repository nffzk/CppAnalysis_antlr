using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using CppParser.Grammars.Generated;
using CppParser.Models;

namespace CppParser.Services
{
    /// <summary>
    /// 仅通过 AST（ParserContext）构建 CppHeaderFile/CppClass/CppEnum/CppProperty/CppMethod。
    /// 不使用正则或源码切片。
    /// </summary>
    public sealed class HeaderModelBuilder : CPP14ParserBaseVisitor<object?>
    {
        private readonly string _fileName;
        private readonly Stack<string> _namespaceStack = new();
        private readonly Stack<CppClass> _classStack = new();
        private readonly AccessControl _access = new();
        private readonly TypeBuilder _typeBuilder = new();

        private readonly CppHeaderFile _header;

        public HeaderModelBuilder(string fileName)
        {
            _fileName = fileName ?? string.Empty;
            _header = new CppHeaderFile { FileName = _fileName };
        }

        public override object? Visit([NotNull] IParseTree tree)
        {
            base.Visit(tree);
            return _header;
        }

        public override object? VisitTranslationUnit([NotNull] CPP14Parser.TranslationUnitContext context)
        {
            return base.VisitTranslationUnit(context);
        }

        // ---------------- 命名空间 ----------------

        public override object? VisitNamespaceDefinition([NotNull] CPP14Parser.NamespaceDefinitionContext context)
        {
            string ns =
                context.Identifier()?.GetText()
                ?? context.originalNamespaceName()?.GetText()
                ?? "(anonymous)";
            _namespaceStack.Push(ns);
            var r = base.VisitNamespaceDefinition(context);
            _namespaceStack.Pop();
            return r;
        }

        // ---------------- 类/结构/联合 ----------------

        public override object? VisitClassSpecifier([NotNull] CPP14Parser.ClassSpecifierContext context)
        {
            var head = context.classHead();
            var stereotype = head.classKey().GetText(); // "class" | "struct" | "union"

            string simpleName = ResolveClassName(head);

            var cls = new CppClass
            {
                Stereotype = stereotype,
                Name = BuildQualifiedName(simpleName)
            };

            // 继承
            var baseClause = head.baseClause();
            if (baseClause != null)
            {
                var list = baseClause.baseSpecifierList();
                if (list != null)
                {
                    foreach (var b in list.baseSpecifier())
                    {
                        var bt = b.baseTypeSpecifier();
                        if (bt != null)
                        {
                            var name = TypeBuilder.JoinTokens(bt);
                            if (!string.IsNullOrWhiteSpace(name))
                                cls.BaseClasses.Add(name);
                        }
                    }
                }
            }

            PlaceClass(cls);

            // 进入类体
            _access.EnterClass(cls.Stereotype);
            _classStack.Push(cls);

            var members = context.memberSpecification();
            if (members != null)
                VisitMemberSpecification(members);

            _classStack.Pop();
            _access.LeaveClass();

            return cls;
        }

        public override object? VisitMemberSpecification([NotNull] CPP14Parser.MemberSpecificationContext context)
        {
            // 逐个子节点判断其真实类型，分别处理
            foreach (var child in context.children ?? Enumerable.Empty<IParseTree>())
            {
                if (child is CPP14Parser.FunctionDefinitionContext fd)
                {
                    HandleFunctionDefinition(fd);
                }
                else if (child is CPP14Parser.MemberdeclarationContext md)
                {
                    HandleMemberDeclaration(md);
                }
                else if (child is CPP14Parser.AccessSpecifierContext acc)
                {
                    _access.Set(acc.GetText()); // public/protected/private
                }
                // 其他节点（如 ';' 号等）忽略
            }
            return null;
        }

        private void HandleMemberDeclaration(CPP14Parser.MemberdeclarationContext md)
        {
            // 成员函数“声明/原型”？仅当：
            //  - declarator 的最外层没有 pointerDeclarator()
            //  - 并且该 declarator 的 noPointerDeclarator() 直接带 parametersAndQualifiers()
            // 这样可避免把函数指针数据成员当成方法。
            if (IsMethodPrototype(md))
            {
                var m = _typeBuilder.BuildMethodFromMemberDeclarationFunction(md, _access.Current);
                _classStack.Peek().Methods.Add(m);
                return;
            }

            // 数据成员（可能一行多声明）
            if (md.declSpecifierSeq() != null && md.memberDeclaratorList() != null)
            {
                foreach (var prop in _typeBuilder.BuildFieldsFromMemberDeclaration(md, _access.Current))
                    _classStack.Peek().Properties.Add(prop);
            }
            // 其余（using/typedef/static_assert/template/空声明）忽略
        }

        private static bool IsMethodPrototype(CPP14Parser.MemberdeclarationContext md)
        {
            var list = md.memberDeclaratorList();
            if (list == null) return false;

            foreach (var item in list.memberDeclarator())
            {
                var d = item.declarator();
                if (d == null) continue;

                if (IsDeclaratorMethodPrototype(d))
                    return true;
            }
            return false;
        }
        private static bool IsDeclaratorMethodPrototype(CPP14Parser.DeclaratorContext d)
        {
            // 排除函数指针/成员函数指针：(*p)(...) / (Class:: *p)(...)
            if (d.pointerDeclarator() != null) return false;

            // 向左沿着 noPointerDeclarator 链找任意一层的参数列表
            var np = d.noPointerDeclarator();
            while (np != null)
            {
                if (np.parametersAndQualifiers() != null)
                    return true;

                // 继续沿链向里查找
                np = np.noPointerDeclarator();
            }
            return false;
        }

        private void HandleFunctionDefinition(CPP14Parser.FunctionDefinitionContext fd)
        {
            var m = _typeBuilder.BuildMethodFromFunctionDefinition(fd, _access.Current);
            _classStack.Peek().Methods.Add(m);
        }

        private void PlaceClass(CppClass c)
        {
            _header.Classes.Add(c);
        }

        private static string ResolveClassName(CPP14Parser.ClassHeadContext head)
        {
            var chn = head.classHeadName();
            if (chn == null) return "(anonymous)";
            return chn.GetText(); // grammar 已将 simpleTemplateId/identifier 汇入
        }

        private string BuildQualifiedName(string simpleName)
        {
            if (_namespaceStack.Count == 0) return simpleName;
            return string.Join("::", _namespaceStack.Reverse()) + "::" + simpleName;
        }

        // ---------------- 枚举 ----------------

        public override object? VisitEnumSpecifier([NotNull] CPP14Parser.EnumSpecifierContext context)
        {
            var e = new CppEnum();

            var head = context.enumHead();
            var keyText = TypeBuilder.JoinTokens(head.enumkey());
            e.IsScoped = keyText.Contains("class") || keyText.Contains("struct");

            string name = string.Empty;
            if (head.nestedNameSpecifier() != null)
                name += head.nestedNameSpecifier().GetText();
            if (head.Identifier() != null)
                name += head.Identifier().GetText();
            e.Name = BuildQualifiedName(string.IsNullOrEmpty(name) ? "(anonymous enum)" : name);

            var enumbase = head.enumbase();
            if (enumbase?.typeSpecifierSeq() != null)
                e.UnderlyingType = TypeBuilder.JoinTokens(enumbase.typeSpecifierSeq());

            var list = context.enumeratorList();
            if (list != null)
            {
                foreach (var def in list.enumeratorDefinition())
                {
                    var id = def.enumerator().Identifier()?.GetText();
                    if (!string.IsNullOrEmpty(id))
                        e.Values.Add(id!);
                }
            }

            if (_classStack.Count == 0) _header.Enums.Add(e);
            else _classStack.Peek().Enums.Add(e);

            return e;
        }
    }
}
