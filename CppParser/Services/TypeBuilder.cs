using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Tree;
using CppParser.Grammars.Generated;
using CppParser.Models;

namespace CppParser.Services
{
    /// <summary>
    /// 从 ParserContext 提取声明/类型信息并填充到模型。
    /// 只做 AST 访问和 Token 文本拼接，不使用正则。
    /// </summary>
    public sealed class TypeBuilder
    {
        // ====== 字段：类内（memberdeclaration 分支） ======

        public IEnumerable<CppProperty> BuildFieldsFromMemberDeclaration(
            CPP14Parser.MemberdeclarationContext md,
            string visibility)
        {
            var ds = md.declSpecifierSeq();
            var baseType = ds != null ? JoinTokens(ds) : string.Empty;

            var list = md.memberDeclaratorList();
            if (list == null) yield break;

            foreach (var m in list.memberDeclarator())
            {
                var d = m.declarator();
                if (d == null) continue;

                // —— 精确排除成员函数原型（外层无 pointerDeclarator，且 noptr 链上出现参数）——
                if (IsDeclaratorMethodPrototype(d))
                    continue;

                // 其余情况（普通变量、数组、函数指针/成员指针等）都是数据成员
                var prop = new CppProperty { Visibility = visibility };
                FillNamePointerRefArray(prop, d);

                prop.FullType = (baseType + " " + BuildPtrRefArraySuffix(d)).Trim();
                prop.Type = baseType.Trim();
                MarkTypeFlagsFromDeclSpecs(prop, baseType);

                var ini = m.braceOrEqualInitializer();
                if (ini != null)
                    prop.DefaultValue = JoinTokens(ini);

                yield return prop;
            }
        }

        // 与 HeaderModelBuilder 同逻辑的判定，放在 TypeBuilder 里复用
        private static bool IsDeclaratorMethodPrototype(CPP14Parser.DeclaratorContext d)
        {
            if (d.pointerDeclarator() != null) return false;
            var np = d.noPointerDeclarator();
            while (np != null)
            {
                if (np.parametersAndQualifiers() != null)
                    return true;
                np = np.noPointerDeclarator();
            }
            return false;
        }



        public IEnumerable<CppProperty> BuildFieldsFromSimpleDeclaration(
            CPP14Parser.SimpleDeclarationContext ctx,
            string visibility)
        {
            var declSpecs = ctx.declSpecifierSeq();
            var baseTypeText = declSpecs != null ? JoinTokens(declSpecs) : string.Empty;

            var list = ctx.initDeclaratorList();
            if (list == null) yield break;

            foreach (var initDecl in list.initDeclarator())
            {
                var prop = BuildFieldFromInitDeclarator(baseTypeText, initDecl);
                prop.Visibility = visibility;
                yield return prop;
            }
        }

        // ====== 方法 ======

        public CppMethod BuildMethodFromFunctionDefinition(
            CPP14Parser.FunctionDefinitionContext ctx,
            string visibility)
        {
            var m = new CppMethod { Visibility = visibility };

            var before = ctx.declSpecifierSeq() != null ? JoinTokens(ctx.declSpecifierSeq()) : string.Empty;
            var declarator = ctx.declarator();
            FillMethodSignatureFromDeclarator(m, before, declarator);

            return m;
        }

        public CppMethod BuildMethodFromMemberDeclarationFunction(
            CPP14Parser.MemberdeclarationContext ctx,
            string visibility)
        {
            var m = new CppMethod { Visibility = visibility };

            var before = ctx.declSpecifierSeq() != null ? JoinTokens(ctx.declSpecifierSeq()) : string.Empty;

            var declList = ctx.memberDeclaratorList();
            if (declList == null) return m;

            // 精确选中：noPointerDeclarator() 具有 parametersAndQualifiers() 且最外层无 pointerDeclarator() 的那个
            var md = declList.memberDeclarator()
                             .FirstOrDefault(x =>
                             {
                                 var dec = x.declarator();
                                 if (dec == null) return false;
                                 return IsDeclaratorMethodPrototype(dec);
                             });

            if (md == null || md.declarator() == null) return m;

            FillMethodSignatureFromDeclarator(m, before, md.declarator());

            // 纯虚/默认/删除（若有）
            var tailTxt = (JoinTokens(md.pureSpecifier()) + " " + JoinTokens(md.virtualSpecifierSeq())).Trim();
            if (tailTxt.Contains("=0")) { m.IsPureVirtual = true; m.IsVirtual = true; }
            if (tailTxt.Contains("=default")) m.IsDefaultImplementation = true;
            if (tailTxt.Contains("=delete")) m.IsDeleted = true;

            return m;
        }

        // ====== 内部：字段 ======

        private CppProperty BuildFieldFromInitDeclarator(string baseType, CPP14Parser.InitDeclaratorContext initDecl)
        {
            var p = new CppProperty();

            var decl = initDecl.declarator();
            if (decl != null)
                FillNamePointerRefArray(p, decl);

            p.FullType = (baseType + " " + BuildPtrRefArraySuffix(decl)).Trim();
            p.Type = baseType.Trim();

            MarkTypeFlagsFromDeclSpecs(p, baseType);

            if (initDecl.initializer() != null)
                p.DefaultValue = JoinTokens(initDecl.initializer());

            return p;
        }

        // ====== 内部：方法 ======

        // TypeBuilder.cs 内部
        private void FillMethodSignatureFromDeclarator(
            CppMethod m,
            string before,
            CPP14Parser.DeclaratorContext declarator)
        {
            // 1) 前缀修饰符（inline/static/explicit/friend/constexpr/virtual...）
            MarkMethodPrefixFlags(m, before);

            // 2) 返回类型（把常见的函数前置修饰词从 decl-specifier-seq 中剔除后作为返回类型文本）
            var stripped = StripKeywords(before, new[]
            {
        "inline","static","explicit","friend","constexpr","virtual","extern","mutable","register"
    }).Trim();

            if (!string.IsNullOrEmpty(stripped))
            {
                m.ReturnType = stripped;
                if (stripped.Contains("*")) m.ReturnTypeIsPointer = true;
                if (stripped.Contains("&")) m.ReturnTypeIsReference = true;
                if (stripped.Contains("const")) m.IsReturnConst = true;
            }

            // 3) 函数名（从 declarator 的 idExpression 上取）
            var id = FindIdInDeclarator(declarator);
            m.Name = string.IsNullOrEmpty(id) ? "(anonymous)" : id;

            // 4) 参数与尾部限定：沿 noPointerDeclarator 链任意一层寻找 parametersAndQualifiers()
            CPP14Parser.ParametersAndQualifiersContext? pq = null;

            // 先拿到一条可用的 noptr 链起点
            var np = declarator.noPointerDeclarator()
                     ?? declarator.pointerDeclarator()?.noPointerDeclarator();

            while (np != null && pq == null)
            {
                if (np.parametersAndQualifiers() != null)
                    pq = np.parametersAndQualifiers();
                else
                    np = np.noPointerDeclarator(); // 继续向里一层
            }

            if (pq != null)
            {
                // 参数列表
                var clause = pq.parameterDeclarationClause();
                var list = clause?.parameterDeclarationList();
                if (list != null)
                {
                    foreach (var pd in list.parameterDeclaration())
                        m.Parameters.Add(BuildParameter(pd));
                }

                // 尾部 cv 限定
                if (pq.cvqualifierseq() != null &&
                    JoinTokens(pq.cvqualifierseq()).Contains("const"))
                {
                    m.IsConst = true;
                }

                // ref-qualifier（如 & / &&）如需落模型，可在此读取：JoinTokens(pq.refqualifier())
            }

            // 5) override / final：在 declarator.virtualSpecifierSeq()
            //var vs = declarator.virtualSpecifierSeq();
            //if (vs != null)
            //{
            //    var vtxt = JoinTokens(vs);
            //    if (vtxt.Contains("override")) m.IsOverride = true;
            //    if (vtxt.Contains("final")) m.IsFinal = true;
            //}
        }

        private CppMethodParameter BuildParameter(CPP14Parser.ParameterDeclarationContext pd)
        {
            var par = new CppMethodParameter();

            var ds = pd.declSpecifierSeq();
            var dsText = ds != null ? JoinTokens(ds) : string.Empty;

            var pdecl = pd.declarator();
            if (pdecl != null)
            {
                FillNamePointerRefArray(par, pdecl);
                if (JoinTokens(pdecl).Contains("&&")) par.IsRValueReference = true;
            }
            else
            {
                par.Name = string.Empty;
            }

            par.FullType = (dsText + " " + BuildPtrRefArraySuffix(pdecl)).Trim();
            par.Type = dsText.Trim();

            MarkTypeFlagsFromDeclSpecs(par, dsText);

            return par;
        }

        // ====== 共享：Declarator 解析 ======

        private static string FindIdInDeclarator(CPP14Parser.DeclaratorContext declarator)
        {
            var id = declarator?.noPointerDeclarator()?.declaratorid()?.idExpression();
            if (id != null) return JoinTokens(id);

            var inner = declarator?.pointerDeclarator()?.noPointerDeclarator()?.declaratorid()?.idExpression();
            if (inner != null) return JoinTokens(inner);

            return string.Empty;
        }

        private void FillNamePointerRefArray(CppProperty target, CPP14Parser.DeclaratorContext decl)
        {
            var id = FindIdInDeclarator(decl);
            target.Name = string.IsNullOrEmpty(id) ? "(anonymous)" : id;

            var txt = JoinTokens(decl);
            if (txt.Contains("*")) target.IsPointer = true;
            if (txt.Contains("&")) target.IsReference = true;

            var noptr = decl.noPointerDeclarator() ?? decl.pointerDeclarator()?.noPointerDeclarator();
            var (hasArray, size) = FindArraySuffix(noptr);
            if (hasArray)
            {
                target.IsArray = true;
                target.ArraySize = size;
            }
        }

        /// <summary>
        /// 在 noptr-declarator 链上检查 [ constant-expression? ]。
        /// </summary>
        private static (bool hasArray, string? size) FindArraySuffix(CPP14Parser.NoPointerDeclaratorContext? noptr)
        {
            var cur = noptr;
            while (cur != null)
            {
                var leftBrackets = cur.LeftBracket();
                if (leftBrackets != null )
                {
                    var ce = cur.constantExpression();
                    var sizeText = ce != null ? JoinTokens(ce) : null;
                    return (true, sizeText);
                }
                cur = cur.noPointerDeclarator();
            }
            return (false, null);
        }

        private static string BuildPtrRefArraySuffix(CPP14Parser.DeclaratorContext? decl)
        {
            if (decl == null) return string.Empty;
            return JoinTokens(decl);
        }

        // ====== 标志位 ======

        private static void MarkTypeFlagsFromDeclSpecs(CppProperty p, string declSpecText)
        {
            var t = declSpecText;
            if (t.Contains("const")) p.IsConst = true;
            if (t.Contains("volatile")) p.IsVolatile = true;
            if (t.Contains("mutable")) p.IsMutable = true;
            if (t.Contains("static")) p.IsStatic = true;
            if (t.Contains("signed")) p.IsSigned = true;
            if (t.Contains("unsigned")) p.IsUnsigned = true;
            if (t.Contains("short")) p.IsShort = true;
            if (t.Contains("long")) p.IsLong = true;
        }

        private static void MarkMethodPrefixFlags(CppMethod m, string before)
        {
            if (before.Contains("inline")) m.IsInline = true;
            if (before.Contains("static")) m.IsStatic = true;
            if (before.Contains("explicit")) m.IsExplicit = true;
            if (before.Contains("friend")) m.IsFriend = true;
            if (before.Contains("constexpr")) m.IsConstexpr = true;
            if (before.Contains("virtual")) m.IsVirtual = true;
        }

        private static string StripKeywords(string text, IEnumerable<string> keywords)
        {
            var r = " " + (text ?? string.Empty) + " ";
            foreach (var k in keywords)
                r = r.Replace(" " + k + " ", " ");
            return r.Trim();
        }

        // ====== Token 拼接 ======

        public static string JoinTokens(IParseTree? node)
        {
            if (node == null) return string.Empty;
            var parts = new List<string>();
            Collect(node, parts);
            return string.Join(" ", parts.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

            static void Collect(IParseTree n, List<string> acc)
            {
                if (n.ChildCount == 0)
                {
                    var t = n.GetText();
                    if (!string.IsNullOrEmpty(t)) acc.Add(t);
                    return;
                }
                for (int i = 0; i < n.ChildCount; i++)
                    Collect(n.GetChild(i), acc);
            }
        }
    }
}
