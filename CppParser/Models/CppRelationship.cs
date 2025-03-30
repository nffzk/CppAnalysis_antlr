using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CppParser.Enums;


namespace CppParser.Models
{
    public abstract class CppRelationship
    {
        /// <summary>
        /// 目标类名
        /// </summary>
        public string TargetClass { get; set; }

        /// <summary>
        /// 多重性
        /// </summary>
        public EnumCppMultiplicity Multiplicity { get; set; }

        /// <summary>
        /// 多重性固定大小（如有的话）
        /// </summary>
        public int? FixedSize { get; set; } 

        /// <summary>
        /// 成员名
        /// </summary>
        public string RoleName { get; set; }          
    }

    /// <summary>
    /// 泛化（继承）
    /// </summary>
    public class CppGeneralization : CppRelationship { } 

    /// <summary>
    /// 实现
    /// </summary>
    public class CppRealization : CppRelationship { }

    /// <summary>
    /// 依赖
    /// </summary>
    public class CppDependency : CppRelationship { }

    /// <summary>
    /// 关联
    /// </summary>
    public class CppAssociation : CppRelationship 
    {
        /// <summary>
        /// 可见性
        /// <summary>
        public EnumVisibility Visibility { get; set; } = EnumVisibility.Public;
    }

    /// <summary>
    /// 组合
    /// </summary>
    public class CppComposition : CppRelationship { }

    /// <summary>
    /// 聚合
    /// </summary>
    public class CppAggregation : CppRelationship { }   
    
}
