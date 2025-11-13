using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CppParser.Enums;


namespace CppParser.Models
{
    public abstract class CodeRelationship
    {
        /// <summary>
        /// 源名（class、struct、interface类名）
        /// </summary>
        public string SourceName { get; set; }

        /// <summary>
        /// 源类型
        /// </summary>
        public EnumClassType SourceStereotype { get; set; }

        /// <summary>
        /// 目标名（class、struct、interface类名）
        /// </summary>
        public string TargetName { get; set; }

        /// <summary>
        /// 目标类型
        /// </summary>
        public EnumClassType TargetStereotype { get; set; }

        /// <summary>
        /// source多重性
        /// </summary>
        public Tuple<string, string> sourcetMultiplicity { get; set; }

        /// <summary>
        /// target多重性
        /// </summary>
        public Tuple<string, string> TargetMultiplicity { get; set; }

        /// <summary>
        /// 记录原型中source多重性的字符串表示
        /// </summary>
        public string SourceMarkMultiplicity { get; set; }

        /// <summary>
        /// 记录原型中target多重性的字符串表示
        /// </summary>
        public string TargetMarkMultiplicity { get; set; }

        /// <summary>
        /// source成员名
        /// </summary>
        public string SourceRoleName { get; set; }

        /// <summary>
        /// target成员名
        /// </summary>
        public string TargetRoleName { get; set; }
    }

    /// <summary>
    /// 泛化（继承）
    /// </summary>
    public class CodeGeneralization : CodeRelationship { } 

    /// <summary>
    /// 实现
    /// </summary>
    public class CodeRealization : CodeRelationship { }

    /// <summary>
    /// 依赖
    /// </summary>
    public class CodeDependency : CodeRelationship 
    {
        public EnumDenpencyType Type { get; set; } = EnumDenpencyType.Denpency;
    }

    /// <summary>
    /// 双向关联
    /// </summary>
    public class CodeAssociation : CodeRelationship 
    {
        /// <summary>
        /// 可见性
        /// <summary>
        public EnumVisibility Visibility { get; set; } = EnumVisibility.Public;

        /// <summary>
        /// 可见性
        /// <summary>
        public EnumVisibility SourceVisibility { get; set; } = EnumVisibility.Public;

        /// <summary>
        /// 可见性
        /// <summary>
        public EnumVisibility TargetVisibility { get; set; } = EnumVisibility.Public;
    }

    /// <summary>
    /// 单向关联
    /// </summary>
    public class CodeUniDirectionalAssociation : CodeRelationship
    {
        /// <summary>
        /// 可见性
        /// <summary>
        public EnumVisibility Visibility { get; set; } = EnumVisibility.Public;

        /// <summary>
        /// 可见性
        /// <summary>
        public EnumVisibility SourceVisibility { get; set; } = EnumVisibility.Public;

        /// <summary>
        /// 可见性
        /// <summary>
        public EnumVisibility TargetVisibility { get; set; } = EnumVisibility.Public;
    }

    /// <summary>
    /// 组合
    /// </summary>
    public class CodeComposition : CodeRelationship { }

    /// <summary>
    /// 聚合
    /// </summary>
    public class CodeAggregation : CodeRelationship { }   
    
}
