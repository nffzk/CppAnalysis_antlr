using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using CppParser.Enums;
using CppParser.Grammars.Generated;
using CppParser.Models;

namespace CppParser.Services.Implementation
{
    /// <summary>
    /// C++头文件访问者，用于构建CodeHeaderFile模型
    /// </summary>
    public class CppHeaderVisitor : CPP14ParserBaseVisitor<object>
    {
        private readonly string _fileName;
        private readonly CodeHeaderFile _headerFile;
        private readonly Stack<CodeClass> _classStack;
        private readonly Stack<EnumVisibility> _visibilityStack;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="fileName"></param>
        public CppHeaderVisitor(string fileName)
        {
            _fileName = fileName;
            _headerFile = new CodeHeaderFile { FileName = fileName };
            _classStack = new Stack<CodeClass>();
            _visibilityStack = new Stack<EnumVisibility>();
            _visibilityStack.Push(EnumVisibility.Private); // 默认可见性
        }

        /// <summary>
        /// 访问翻译单元（Translation Unit）
        /// </summary>
        /// <param name="context">整个C++源文件</param>
        /// <returns></returns>
        public override object VisitTranslationUnit([NotNull] CPP14Parser.TranslationUnitContext context)
        {
            // 访问所有声明
            if (context.declarationseq()?.declaration() != null)
            {
                // 遍历声明序列中的所有声明项
                foreach (var declaration in context.declarationseq().declaration())
                {
                    //对每个声明项调用 Visit方法，触发相应的 VisitDeclaration方法。实现深度优先遍历语法树。
                    Visit(declaration);
                }
            }
            return _headerFile;
        }

        /// <summary>
        /// 访问类说明符（Class Specifier）
        /// </summary>
        /// <param name="context">类定义节点</param>
        /// <returns></returns>
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

        /// <summary>
        /// 访问Access说明符（Access Specifier）
        /// </summary>
        /// <param name="context">访问说明符节点</param>
        /// <returns></returns>
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

        /// <summary>
        /// 访问成员声明（Member Declaration）
        /// </summary>
        /// <param name="context">成员声明节点</param>
        /// <returns></returns>
        public override object VisitMemberdeclaration([NotNull] CPP14Parser.MemberdeclarationContext context)
        {
            if (_classStack.Count == 0) return null;

            try
            {
                var currentVisibility = _visibilityStack.Peek();
                var currentClass = _classStack.Peek();

                // 处理字段声明（成员变量）
                if (context.declSpecifierSeq() != null && context.memberDeclaratorList() != null && !IsFunctionDeclaration(context))
                {
                    var properties = ExtractPropertiesFromDeclaration(context, currentVisibility);
                    if (properties != null && properties.Count > 0)
                    {
                        currentClass.Properties.AddRange(properties);
                    }
                }
                // 处理方法声明（成员函数）
                else if (IsFunctionDeclaration(context))
                {
                    var method = ExtractMethodFromDeclaration(context, currentVisibility);
                    if (method != null)
                    {
                        currentClass.Methods.Add(method);
                    }
                }
                // 处理嵌套类型声明（枚举）。当前元模型只记录了嵌套的枚举类型
                else if (context.declSpecifierSeq()?.GetText().Contains("enum") == true)
                {
                    // 嵌套类型会在相应的访问器方法中处理
                    return base.VisitMemberdeclaration(context);
                }

                return base.VisitMemberdeclaration(context);
            }
            catch (Exception ex)
            {
                // 记录错误但继续处理
                System.Diagnostics.Debug.WriteLine($"处理成员声明时出错: {ex.Message}");
                return base.VisitMemberdeclaration(context);
            }
        }

        /// <summary>
        /// 访问函数定义（Function Definition）
        /// </summary>
        /// <param name="context">函数定义节点</param>
        /// <returns></returns>
        public override object VisitFunctionDefinition([NotNull] CPP14Parser.FunctionDefinitionContext context)
        {
            if (_classStack.Count == 0) return base.VisitFunctionDefinition(context);

            try
            {
                var currentVisibility = _visibilityStack.Peek();
                var currentClass = _classStack.Peek();

                // 提取基础返回类型
                string baseReturnType = context.declSpecifierSeq() != null ?
                    ExtractPropertyType(context.declSpecifierSeq()) : string.Empty;

                // 判断是否是构造函数或析构函数
                bool isConstructorOrDestructor = string.IsNullOrEmpty(baseReturnType);

                // 构建方法信息
                var methodInfo = ExtractMethodInfo(context.declarator(), baseReturnType);

                var method = new CodeMethod
                {
                    Visibility = currentVisibility,
                    Name = methodInfo.Name,
                    ReturnType = methodInfo.ReturnType,
                    IsStatic = context.declSpecifierSeq()?.GetText().Contains("static") == true,
                    IsVirtual = context.declSpecifierSeq()?.GetText().Contains("virtual") == true,
                    Parameters = methodInfo.Parameters,
                };

                currentClass.Methods.Add(method);

                return base.VisitFunctionDefinition(context);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理函数定义时出错: {ex.Message}");
                return base.VisitFunctionDefinition(context);
            }
        }

        /// <summary>
        /// 访问枚举说明符（Enum Specifier）
        /// </summary>
        /// <param name="context">枚举说明符节点</param>
        /// <returns></returns>
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

        /// <summary>
        /// 访问基类子句（Base Clause）
        /// </summary>
        /// <param name="context"></param>
        /// <param name="codeClass"></param>
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
                        var baseName = baseType.GetText();
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

        /// <summary>
        /// 从成员声明中提取属性信息,支持多个声明如 "int a, b, c;"
        /// </summary>
        /// <param name="context">处理字段声明（成员变量）</param>  
        /// <param name="visibility"></param>
        /// <returns></returns>
        private List<CodeProperty> ExtractPropertiesFromDeclaration(CPP14Parser.MemberdeclarationContext context, EnumVisibility visibility)
        {
            var properties = new List<CodeProperty>();

            try
            {
                var declarators = context.memberDeclaratorList().memberDeclarator();
                string baseType = ExtractPropertyType(context.declSpecifierSeq()); 

                foreach (var declarator in declarators)
                {
                    // 获取完整的类型信息（包括指针、数组等修饰符）
                    string fullType = BuildFullType(baseType, declarator.declarator());
                    string propertyName = ExtractDeclaratorName(declarator.declarator());

                    var property = new CodeProperty
                    {
                        Visibility = visibility,
                        Name = propertyName,
                        // 先记录完整类型，后面会做预处理
                        Type = fullType, 
                        IsStatic = context.declSpecifierSeq()?.GetText().Contains("static") == true,
                        DefaultValue = ExtractDefaultValue(declarator)
                    };

                    properties.Add(property);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"提取属性时出错: {ex.Message}");
            }

            return properties;
        }

        /// <summary>
        /// 构建完整的类型字符串，包括指针和数组等修饰符，例如 "const int*[]"
        /// </summary>
        /// <param name="baseType"></param>
        /// <param name="declarator"></param>
        /// <returns></returns>
        private string BuildFullType(string baseType, CPP14Parser.DeclaratorContext declarator)
        {
            string fullType = baseType;
       
            // 处理指针声明器
            if (declarator.pointerDeclarator() != null)
            {
                var pointerDecl = declarator.pointerDeclarator();

                // 添加指针操作符 (*, &, &&)
                if (pointerDecl.pointerOperator() != null)
                {
                    foreach (var pointerOp in pointerDecl.pointerOperator())
                    {
                        fullType += pointerOp.GetText(); // 添加 "*", "&", "&&"
                    }
                }

                // 处理数组声明
                fullType = ProcessArrayDeclarator(fullType, pointerDecl.noPointerDeclarator());
            }
            else if (declarator.noPointerDeclarator() != null)
            {
                // 直接处理非指针声明器（如数组）
                fullType = ProcessArrayDeclarator(fullType, declarator.noPointerDeclarator());
            }

            return fullType.Trim();
        }

        /// <summary>
        /// 处理数组声明器，递归添加数组维度
        /// </summary>
        /// <param name="currentType"></param>
        /// <param name="noPointerDecl"></param>
        /// <returns></returns>
        private string ProcessArrayDeclarator(string currentType, CPP14Parser.NoPointerDeclaratorContext noPointerDecl)
        {
            if (noPointerDecl == null) return currentType;

            string result = currentType;

            // 递归处理数组维度
            if (noPointerDecl.LeftBracket() != null && noPointerDecl.RightBracket() != null)
            {
                // 添加数组维度
                string arrayPart = noPointerDecl.LeftBracket().GetText();
                if (noPointerDecl.constantExpression() != null)
                {
                    arrayPart += noPointerDecl.constantExpression().GetText();
                }
                arrayPart += noPointerDecl.RightBracket().GetText();

                result += arrayPart;

                // 处理多维数组
                if (noPointerDecl.noPointerDeclarator() != null)
                {
                    result = ProcessArrayDeclarator(result, noPointerDecl.noPointerDeclarator());
                }
            }

            return result;
        }

        /// <summary>
        /// 从声明说明符中提取属性类型 例如 "static const int* value;" 提取 "const int*"
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private string ExtractPropertyType(CPP14Parser.DeclSpecifierSeqContext context)
        {
            if (context == null) return string.Empty;

            var typeParts = new List<string>();

            foreach (var declSpecifier in context.declSpecifier())
            {
                // 这里只会把语法文件中的 trailingTypeSpecifier 加入类型描述中。不会把 static mutable 修饰符加入类型描述
                if (declSpecifier.typeSpecifier() != null)
                {
                    typeParts.Add(declSpecifier.GetText());
                }
            }

            return string.Join(" ", typeParts).Trim();
        }

        /// <summary>
        /// 从声明器中提取默认值
        /// </summary>
        /// <param name="declarator"></param>
        /// <returns></returns>
        private string ExtractDefaultValue(CPP14Parser.MemberDeclaratorContext declarator)
        {
            var initializer = declarator.braceOrEqualInitializer();
            if (initializer != null)
            {
                // 提取 "= value" 或 "{value}" 部分
                var initializerText = initializer.GetText();
                if (initializerText.StartsWith("="))
                {
                    return initializerText.Substring(1).Trim();
                }
                return initializerText;
            }
            return string.Empty;
        }

        /// <summary>
        /// 从成员方法声明中提取方法信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="visibility"></param>
        /// <returns></returns>
        private CodeMethod ExtractMethodFromDeclaration(CPP14Parser.MemberdeclarationContext context, EnumVisibility visibility)
        {
            try
            {
                // 获取第一个声明器（方法声明）
                var declarator = context.memberDeclaratorList()?.memberDeclarator()?.FirstOrDefault();
                if (declarator == null) return null;

                // 提取基础返回类型
                string baseReturnType = context.declSpecifierSeq() != null ?
                    ExtractPropertyType(context.declSpecifierSeq()) : string.Empty;

                // 构建完整的方法信息
                var methodInfo = ExtractMethodInfo(declarator.declarator(), baseReturnType);

                var method = new CodeMethod
                {
                    Visibility = visibility,
                    Name = methodInfo.Name,
                    ReturnType = methodInfo.ReturnType,
                    IsStatic = context.declSpecifierSeq()?.GetText().Contains("static") == true,
                    IsVirtual = context.declSpecifierSeq()?.GetText().Contains("virtual") == true,
                    IsPureVirtual = IsPureVirtualMethod(context),
                    Parameters = methodInfo.Parameters
                };

                return method;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"提取方法时出错: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 从声明器中提取方法信息，包括名称、返回类型和参数列表
        /// </summary>
        /// <param name="declarator"></param>
        /// <param name="baseReturnType"></param>
        /// <returns></returns>
        private (string Name, string ReturnType, List<CodeMethodParameter> Parameters) ExtractMethodInfo(CPP14Parser.DeclaratorContext declarator, string baseReturnType)
        {
            string methodName = string.Empty;
            string fullReturnType = baseReturnType;
            var parameters = new List<CodeMethodParameter>();

            if (declarator.pointerDeclarator() != null)
            {
                var pointerDecl = declarator.pointerDeclarator();

                // 添加指针操作符到返回类型
                if (pointerDecl.pointerOperator() != null)
                {
                    // 添加指针操作符 (*, &, &&)
                    foreach (var pointerOp in pointerDecl.pointerOperator())
                    {
                        fullReturnType += pointerOp.GetText();
                    }
                }

                // 从指针声明器中提取方法名和参数
                var noPointerDecl = pointerDecl.noPointerDeclarator();
                if (noPointerDecl != null)
                {
                    methodName = ExtractMethodNameFromNoPointerDeclarator(noPointerDecl);
                    parameters = ExtractParametersFromNoPointerDeclarator(noPointerDecl);
                }
            }

            return (methodName, fullReturnType.Trim(), parameters);
        }

        /// <summary>
        /// 从非指针声明器中提取方法名称
        /// </summary>
        /// <param name="noPointerDecl"></param>
        /// <returns></returns>
        private string ExtractMethodNameFromNoPointerDeclarator(CPP14Parser.NoPointerDeclaratorContext noPointerDecl)
        {
            if (noPointerDecl.declaratorid() != null)
            {
                var declaratorId = noPointerDecl.declaratorid();
                if (declaratorId.idExpression() != null)
                {
                    return declaratorId.idExpression().GetText();
                }
                return declaratorId.GetText();
            }

            // 处理函数指针等复杂情况
            if (noPointerDecl.noPointerDeclarator() != null)
            {
                return ExtractMethodNameFromNoPointerDeclarator(noPointerDecl.noPointerDeclarator());
            }

            return noPointerDecl.GetText();
        }

        /// <summary>
        /// 从非指针声明器中提取参数列表
        /// </summary>
        /// <param name="noPointerDecl"></param>
        /// <returns></returns>
        private List<CodeMethodParameter> ExtractParametersFromNoPointerDeclarator(CPP14Parser.NoPointerDeclaratorContext noPointerDecl)
        {
            var parameters = new List<CodeMethodParameter>();

            // 查找参数和限定符节点
            var paramsAndQualifiers = noPointerDecl.parametersAndQualifiers();
            if (paramsAndQualifiers != null)
            {
                var paramDeclarationClause = paramsAndQualifiers.parameterDeclarationClause();
                if (paramDeclarationClause != null)
                {
                    var paramList = paramDeclarationClause.parameterDeclarationList();
                    if (paramList != null)
                    {
                        foreach (var paramDeclaration in paramList.parameterDeclaration())
                        {
                            var parameter = ExtractParameterInfo(paramDeclaration);
                            parameters.Add(parameter);
                        }
                    }
                }
            }

            return parameters;
        }

        /// <summary>
        /// 从参数声明中提取参数信息
        /// </summary>
        /// <param name="paramDeclaration"></param>
        /// <returns></returns>
        private CodeMethodParameter ExtractParameterInfo(CPP14Parser.ParameterDeclarationContext paramDeclaration)
        {
            // 复用现有的参数提取逻辑
            var parameter = new CodeMethodParameter();

            // 提取参数类型
            string baseType = paramDeclaration.declSpecifierSeq() != null ?
                ExtractPropertyType(paramDeclaration.declSpecifierSeq()) : string.Empty;
            string fullType = BuildFullType(baseType, paramDeclaration.declarator());

            // 提取参数名和完整类型
            if (paramDeclaration.declarator() != null)
            {
                parameter.Name = ExtractDeclaratorName(paramDeclaration.declarator());
                parameter.Type = fullType;
            }
            else
            {
                // 没有声明器的情况（如匿名参数）
                parameter.Name = string.Empty;
                parameter.Type = baseType;
            }

            // 处理默认值
            if (paramDeclaration.Assign() != null && paramDeclaration.initializerClause() != null)
            {
                parameter.DefaultValue = paramDeclaration.initializerClause().GetText();
            }

            return parameter;
        }

        /// <summary>
        /// 从声明器中提取标识符名称，用于提取属性和方法参数的名称
        /// </summary>
        /// <param name="declarator"></param>
        /// <returns></returns>
        private string ExtractDeclaratorName(CPP14Parser.DeclaratorContext declarator)
        {
            if (declarator == null) return string.Empty;

            // 处理指针声明器
            if (declarator.pointerDeclarator() != null)
            {
                var noPointerDecl = declarator.pointerDeclarator().noPointerDeclarator();
                if (noPointerDecl != null)
                {
                    return ExtractNameFromNoPointerDeclarator(noPointerDecl);
                }
            }

            // 处理非指针声明器
            if (declarator.noPointerDeclarator() != null)
            {
                return ExtractNameFromNoPointerDeclarator(declarator.noPointerDeclarator());
            }

            return declarator.GetText();
        }

        /// <summary>
        /// 从非指针声明器中提取标识符名称
        /// </summary>
        /// <param name="noPointerDecl"></param>
        /// <returns></returns>
        private string ExtractNameFromNoPointerDeclarator(CPP14Parser.NoPointerDeclaratorContext noPointerDecl)
        {
            if (noPointerDecl == null) return string.Empty;

            // 递归查找最内层的标识符
            if (noPointerDecl.declaratorid() != null)
            {
                var declaratorId = noPointerDecl.declaratorid();
                if (declaratorId.idExpression() != null)
                {
                    var idExpr = declaratorId.idExpression();
                    if (idExpr.unqualifiedId() != null)
                    {
                        var unqualifiedId = idExpr.unqualifiedId();
                        if (unqualifiedId.Identifier != null)
                        {
                            // 找到标识符
                            return unqualifiedId.Identifier().GetText();
                        }
                    }
                }
                return declaratorId.GetText();
            }

            // 递归处理嵌套的声明器（如数组声明）
            if (noPointerDecl.noPointerDeclarator() != null)
            {
                return ExtractNameFromNoPointerDeclarator(noPointerDecl.noPointerDeclarator());
            }

            // 处理数组声明：提取数组名（去掉[10]部分）
            string text = noPointerDecl.GetText();
            if (text.Contains("["))
            {
                return text.Substring(0, text.IndexOf("[")).Trim();
            }

            return text;
        }

        /// <summary>
        /// 判断成员方法是否为纯虚函数
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private bool IsPureVirtualMethod(CPP14Parser.MemberdeclarationContext context)
        {
            return context.memberDeclaratorList()?.GetText().Contains("= 0") == true ||
                   context.memberDeclaratorList()?.GetText().Contains("=0") == true;
        }

        /// <summary>
        /// 判断成员声明是否为函数声明
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private bool IsFunctionDeclaration(CPP14Parser.MemberdeclarationContext context)
        {
            // 检查是否是函数声明
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

        /// <summary>
        /// 判断声明器是否为函数声明器
        /// </summary>
        /// <param name="declarator"></param>
        /// <returns></returns>
        private bool IsFunctionDeclarator(CPP14Parser.DeclaratorContext declarator)
        {
            // 查找参数列表节点
            var parametersAndQualifiers = FindNode<CPP14Parser.ParametersAndQualifiersContext>(declarator);
            if (parametersAndQualifiers != null)
            {
                return true;
            }

            // 文本检查
            var text = declarator.GetText();
            return text.Contains("(") && !text.Contains("(*)") && !text.Contains("(&)");
        }

        /// <summary>
        /// 在语法树中递归查找特定类型的节点，例如查找参数列表节点
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="root"></param>
        /// <returns></returns>
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