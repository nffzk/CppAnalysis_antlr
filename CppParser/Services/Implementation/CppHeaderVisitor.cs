using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using CppParser.Enums;
using CppParser.Grammars.Generated;
using CppParser.Models;

namespace CppParser.Services.Implementation
{
    public class CppHeaderVisitor : CPP14ParserBaseVisitor<object>
    {
        private readonly string _fileName;
        private readonly CodeHeaderFile _headerFile;
        private readonly Stack<CodeClass> _classStack;
        private readonly Stack<EnumVisibility> _visibilityStack;

        public CppHeaderVisitor(string fileName)
        {
            _fileName = fileName;
            _headerFile = new CodeHeaderFile { FileName = fileName };
            _classStack = new Stack<CodeClass>();
            _visibilityStack = new Stack<EnumVisibility>();
            _visibilityStack.Push(EnumVisibility.Private); // 默认可见性
        }

        public override object VisitTranslationUnit([NotNull] CPP14Parser.TranslationUnitContext context)
        {
            // 访问所有声明
            if (context.declarationseq()?.declaration() != null)
            {
                foreach (var declaration in context.declarationseq().declaration())
                {
                    Visit(declaration);
                }
            }
            return _headerFile;
        }

        public override object VisitClassSpecifier([NotNull] CPP14Parser.ClassSpecifierContext context)
        {
            var classHead = context.classHead();
            var className = classHead.classHeadName()?.GetText() ?? "AnonymousClass";
            var classKey = classHead.classKey().GetText();

            var codeClass = new CodeClass
            {
                Name = className,
                Stereotype = classKey.ToLower() switch
                {
                    "struct" => EnumClassType.Struct,
                    "union" => EnumClassType.Union,
                    _ => EnumClassType.Class
                }
            };

            // 处理基类
            var baseClause = classHead.baseClause();
            if (baseClause != null)
            {
                VisitBaseClause(baseClause, codeClass);
            }

            _headerFile.Classes.Add(codeClass);
            _classStack.Push(codeClass);

            // 设置默认可见性
            _visibilityStack.Push(codeClass.Stereotype == EnumClassType.Struct ?
                EnumVisibility.Public : EnumVisibility.Private);

            // 访问成员声明
            var memberSpecification = context.memberSpecification();
            if (memberSpecification != null)
            {
                Visit(memberSpecification);
            }

            _classStack.Pop();
            _visibilityStack.Pop();

            return codeClass;
        }

        private void VisitBaseClause(CPP14Parser.BaseClauseContext context, CodeClass codeClass)
        {
            var baseSpecifierList = context.baseSpecifierList();
            if (baseSpecifierList != null)
            {
                foreach (var baseSpecifier in baseSpecifierList.baseSpecifier())
                {
                    var baseType = baseSpecifier.baseTypeSpecifier();
                    if (baseType != null)
                    {
                        var baseName = GetTypeText(baseType);
                        if (!string.IsNullOrEmpty(baseName))
                        {
                            var generalization = new CodeGeneralization
                            {
                                TargetName = baseName.Trim()
                            };
                            codeClass.Generalizations.Add(generalization);
                        }
                    }
                }
            }
        }

        public override object VisitAccessSpecifier([NotNull] CPP14Parser.AccessSpecifierContext context)
        {
            var visibilityText = context.GetText().ToLower();
            var visibility = visibilityText switch
            {
                "public" => EnumVisibility.Public,
                "protected" => EnumVisibility.Protected,
                "private" => EnumVisibility.Private,
                _ => EnumVisibility.Private
            };

            if (_visibilityStack.Count > 0)
                _visibilityStack.Pop();
            _visibilityStack.Push(visibility);

            return base.VisitAccessSpecifier(context);
        }

        public override object VisitMemberdeclaration([NotNull] CPP14Parser.MemberdeclarationContext context)
        {
            if (_classStack.Count == 0) return null;

            var currentVisibility = _visibilityStack.Peek();
            var currentClass = _classStack.Peek();

            // 处理字段声明
            if (context.declSpecifierSeq() != null && context.memberDeclaratorList() != null)
            {
                VisitFieldDeclaration(context, currentClass, currentVisibility);
            }
            // 处理方法声明
            else if (IsFunctionDeclaration(context))
            {
                VisitMethodDeclaration(context, currentClass, currentVisibility);
            }

            return base.VisitMemberdeclaration(context);
        }

        private void VisitFieldDeclaration(CPP14Parser.MemberdeclarationContext context, CodeClass currentClass, EnumVisibility visibility)
        {
            var declSpecifierText = GetDeclSpecifierText(context.declSpecifierSeq());
            var baseType = ExtractBaseType(declSpecifierText);

            var memberDeclaratorList = context.memberDeclaratorList();
            foreach (var memberDeclarator in memberDeclaratorList.memberDeclarator())
            {
                var declarator = memberDeclarator.declarator();
                if (declarator != null && !IsFunctionDeclarator(declarator))
                {
                    var propertyName = GetDeclaratorName(declarator);
                    var propertyType = BuildFullType(baseType, declarator);

                    var property = new CodeProperty
                    {
                        Name = propertyName,
                        Type = propertyType,
                        Visibility = visibility,
                        DefaultValue = memberDeclarator.braceOrEqualInitializer()?.GetText() ?? string.Empty,
                        IsStatic = declSpecifierText.Contains("static"),
                    };

                    currentClass.Properties.Add(property);
                }
            }
        }

        private void VisitMethodDeclaration(CPP14Parser.MemberdeclarationContext context, CodeClass currentClass, EnumVisibility visibility)
        {
            var declSpecifierText = context.declSpecifierSeq() != null ?
                GetDeclSpecifierText(context.declSpecifierSeq()) : string.Empty;

            var baseType = ExtractBaseType(declSpecifierText);
            var isConstructorOrDestructor = string.IsNullOrEmpty(baseType);

            var method = new CodeMethod
            {
                Visibility = visibility,
                IsStatic = declSpecifierText.Contains("static"),
                IsVirtual = declSpecifierText.Contains("virtual")
            };

            var memberDeclaratorList = context.memberDeclaratorList();
            if (memberDeclaratorList != null)
            {
                foreach (var memberDeclarator in memberDeclaratorList.memberDeclarator())
                {
                    var declarator = memberDeclarator.declarator();
                    if (declarator != null && IsFunctionDeclarator(declarator))
                    {
                        ExtractFunctionInfo(declarator, method, isConstructorOrDestructor);
                        break;
                    }
                }
            }

            currentClass.Methods.Add(method);
        }

        public override object VisitFunctionDefinition([NotNull] CPP14Parser.FunctionDefinitionContext context)
        {
            if (_classStack.Count == 0) return base.VisitFunctionDefinition(context);

            var currentVisibility = _visibilityStack.Peek();
            var currentClass = _classStack.Peek();

            var declSpecifierText = context.declSpecifierSeq() != null ?
                GetDeclSpecifierText(context.declSpecifierSeq()) : string.Empty;

            var baseType = ExtractBaseType(declSpecifierText);
            var isConstructorOrDestructor = string.IsNullOrEmpty(baseType);

            var method = new CodeMethod
            {
                Visibility = currentVisibility,
                IsStatic = declSpecifierText.Contains("static"),
                IsVirtual = declSpecifierText.Contains("virtual")
            };

            ExtractFunctionInfo(context.declarator(), method, isConstructorOrDestructor);

            currentClass.Methods.Add(method);

            return base.VisitFunctionDefinition(context);
        }

        public override object VisitEnumSpecifier([NotNull] CPP14Parser.EnumSpecifierContext context)
        {
            var enumHead = context.enumHead();
            var enumName = enumHead.Identifier()?.GetText() ?? "AnonymousEnum";

            var codeEnum = new CodeEnum
            {
                Name = enumName,
                IsScoped = enumHead.enumkey().GetText().Contains("class") ||
                          enumHead.enumkey().GetText().Contains("struct"),
                UnderlyingType = enumHead.enumbase()?.typeSpecifierSeq()?.GetText() ?? "int",
                Values = new Dictionary<string, string>()

            };

            // 处理枚举值
            var enumeratorList = context.enumeratorList();
            if (enumeratorList != null)
            {
                foreach (var enumeratorDefinition in enumeratorList.enumeratorDefinition())
                {
                    var enumerator = enumeratorDefinition.enumerator();
                    var valueName = enumerator.Identifier()?.GetText();
                    if (!string.IsNullOrEmpty(valueName))
                    {
                        // 往枚举值字典添加枚举值，初始中文名称为空
                        if (!codeEnum.Values.ContainsKey(valueName))
                        {
                            codeEnum.Values[valueName] = string.Empty;
                        }
                    }
                }
            }

            if (_classStack.Count > 0)
            {
                _classStack.Peek().Enums.Add(codeEnum);
            }
            else
            {
                _headerFile.Enums.Add(codeEnum);
            }

            return base.VisitEnumSpecifier(context);
        }

        #region Helper Methods

        private string GetDeclSpecifierText(CPP14Parser.DeclSpecifierSeqContext context)
        {
            if (context == null) return string.Empty;
            return string.Join(" ", context.declSpecifier().Select(ds => ds.GetText()));
        }

        private string GetTypeText(IParseTree node)
        {
            if (node == null) return string.Empty;
            return node.GetText();
        }

        private string ExtractBaseType(string declSpecifierText)
        {
            var keywordsToRemove = new[] { "static", "inline", "virtual", "explicit", "friend", "constexpr", "extern", "mutable" };
            var result = declSpecifierText;
            foreach (var keyword in keywordsToRemove)
            {
                result = result.Replace(keyword, "");
            }
            return result.Trim();
        }

        private bool IsFunctionDeclaration(CPP14Parser.MemberdeclarationContext context)
        {
            var memberDeclaratorList = context.memberDeclaratorList();
            if (memberDeclaratorList == null) return false;

            foreach (var memberDeclarator in memberDeclaratorList.memberDeclarator())
            {
                var declarator = memberDeclarator.declarator();
                if (declarator != null && IsFunctionDeclarator(declarator))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsFunctionDeclarator(CPP14Parser.DeclaratorContext declarator)
        {
            var text = declarator.GetText();
            return text.Contains("(") && !text.Contains("(*)") && !text.Contains("(&)");
        }

        private string GetDeclaratorName(CPP14Parser.DeclaratorContext declarator)
        {
            var noPointerDeclarator = declarator.noPointerDeclarator() ??
                                    declarator.pointerDeclarator()?.noPointerDeclarator();

            if (noPointerDeclarator != null)
            {
                var declaratorId = noPointerDeclarator.declaratorid();
                if (declaratorId != null)
                {
                    var idExpression = declaratorId.idExpression();
                    if (idExpression != null)
                    {
                        return idExpression.GetText();
                    }
                }
            }
            return "(anonymous)";
        }

        private string BuildFullType(string baseType, CPP14Parser.DeclaratorContext declarator)
        {
            var declaratorText = GetDeclaratorText(declarator);
            return (baseType + " " + declaratorText).Trim();
        }

        private string GetDeclaratorText(CPP14Parser.DeclaratorContext declarator)
        {
           
            var pointerPart = declarator.pointerDeclarator()?.GetText() ?? "";
            var noPointerPart = declarator.noPointerDeclarator()?.GetText() ?? "";
            return (pointerPart + " " + noPointerPart).Trim();
        }

        private void ExtractFunctionInfo(CPP14Parser.DeclaratorContext declarator, CodeMethod method, bool isConstructorOrDestructor)
        {
            method.Name = GetDeclaratorName(declarator);

            if (!isConstructorOrDestructor)
            {
                // 对于普通方法，返回类型已经在baseType中
            }

            var parametersAndQualifiers = FindNode<CPP14Parser.ParametersAndQualifiersContext>(declarator);
            if (parametersAndQualifiers != null)
            {
                var parameterDeclarationClause = parametersAndQualifiers.parameterDeclarationClause();
                if (parameterDeclarationClause != null)
                {
                    var parameterDeclarationList = parameterDeclarationClause.parameterDeclarationList();
                    if (parameterDeclarationList != null)
                    {
                        foreach (var parameterDeclaration in parameterDeclarationList.parameterDeclaration())
                        {
                            var parameter = ExtractParameterInfo(parameterDeclaration);
                            method.Parameters.Add(parameter);
                        }
                    }
                }
            }
        }

        private CodeMethodParameter ExtractParameterInfo(CPP14Parser.ParameterDeclarationContext parameterDeclaration)
        {
            var parameter = new CodeMethodParameter();

            var declSpecifierText = parameterDeclaration.declSpecifierSeq() != null ?
                GetDeclSpecifierText(parameterDeclaration.declSpecifierSeq()) : string.Empty;

            parameter.Type = ExtractBaseType(declSpecifierText);

            var declarator = parameterDeclaration.declarator();
            if (declarator != null)
            {
                parameter.Name = GetDeclaratorName(declarator);
                var declaratorText = GetDeclaratorText(declarator);
                parameter.Type += " " + declaratorText;
            }

            parameter.Type = parameter.Type.Trim();

            return parameter;
        }

        private T FindNode<T>(IParseTree root) where T : class, IParseTree
        {
            if (root is T result) return result;

            for (int i = 0; i < root.ChildCount; i++)
            {
                var child = root.GetChild(i);
                var found = FindNode<T>(child);
                if (found != null) return found;
            }

            return null;
        }

        #endregion
    }
}