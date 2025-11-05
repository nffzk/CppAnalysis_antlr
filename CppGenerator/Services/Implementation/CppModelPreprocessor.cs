using System;
using System.Linq;
using CppParser.Enums;
using CppParser.Models;

namespace CppGenerator.Services
{
    /// <summary>
    /// 轻量预处理：可见性兜底、排序、FixedSize 兜底、类型小修正等。
    /// 不改变业务语义，只做“生成友好化”。
    /// </summary>
    public sealed class CppModelPreprocessor : ICppModelPreprocessor
    {






        // —— 类 —— //
        public CodeClass ProcessClass(CodeClass model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            model.Name = string.IsNullOrWhiteSpace(model.Name) ? "Unnamed" : model.Name.Trim();

            foreach (var p in model.Properties ?? Enumerable.Empty<CodeProperty>())
                if (p.Visibility == EnumVisibility.None) p.Visibility = EnumVisibility.Private;

            foreach (var m in model.Methods ?? Enumerable.Empty<CodeMethod>())
                if (m.Visibility == EnumVisibility.None) m.Visibility = EnumVisibility.Public;

            var allRels = model.Associations
                .Concat<CodeRelationship>(model.Aggregations)
                .Concat(model.Compositions);
            foreach (var r in allRels)
            {
                //if (r.TargetMultiplicity == EnumCppMultiplicity.ToFixed && r.TargetFixedSize == null)
                //    r.TargetFixedSize = 1;
            }

            if (model.Methods != null)
            {
                model.Methods = model.Methods
                    .OrderBy(m => m.Name == model.Name ? 0 : (m.Name.StartsWith("~") ? 1 : 2))
                    .ThenBy(m => m.IsStatic ? 1 : 0)
                    .ThenBy(m => m.Name)
                    .ToList();
            }

            if (model.Properties != null)
            {
                model.Properties = model.Properties
                    .OrderBy(p => p.Visibility)
                    .ThenBy(p => p.IsStatic ? 1 : 0)
                    .ThenBy(p => p.Name)
                    .ToList();
            }

            // 轻度类型清洗
            foreach (var p in model.Properties ?? Enumerable.Empty<CodeProperty>())
                if (p.Type != null) p.Type = p.Type.Replace("std ::", "std::");
            foreach (var m in model.Methods ?? Enumerable.Empty<CodeMethod>())
                if (m.ReturnType != null) m.ReturnType = m.ReturnType.Replace("std ::", "std::");

            // 若标记为 Interface，确保方法是 virtual/pure & 无数据成员（尽量不强改，仅兜底）
            if (model.Stereotype == EnumClassType.Interface)
            {
                if (model.Properties != null && model.Properties.Count > 0)
                {
                    // 接口不应有数据成员——这里不直接删除，而交由模板忽略/或你自行决定
                    // 你也可以在这里清空：model.Properties.Clear();
                }

                foreach (var m in model.Methods ?? Enumerable.Empty<CodeMethod>())
                {
                    // 让模板做 "=0" 输出，本处不强改 IsPureVirtual
                    if (!m.IsVirtual) m.IsVirtual = true;
                }
            }

            return model;
        }

        // —— 枚举 —— //
        public CodeEnum ProcessEnum(CodeEnum model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            model.Name = string.IsNullOrWhiteSpace(model.Name) ? "UnnamedEnum" : model.Name.Trim();

            // 去重/去空
            //if (model.Values != null)
            //{
            //    model.Values = model.Values
            //        .Where(v => !string.IsNullOrWhiteSpace(v))
            //        .Select(v => v.Trim())
            //        .Distinct()
            //        .ToList();
            //}
            return model;
        }
    }
}
