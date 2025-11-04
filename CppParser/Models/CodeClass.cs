
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CppParser.Enums;

namespace CppParser.Models
{
    public class CodeClass : CodeElement
    {
        /// <summary>
        /// class类型，默认为"class"。包括"class对应类图中的类"、"struct对应类图中的类"、"interface对应类图中的接口"
        /// </summary>
        public EnumClassType Stereotype { get; set; } = EnumClassType.Class;

        /// <summary>
        /// 类的值属性
        /// </summary>
        public List<CodeProperty> Properties { get; set; } = new List<CodeProperty>();

        /// <summary>
        /// 类的方法
        /// </summary>
        public List<CodeMethod> Methods { get; set; } = new List<CodeMethod>();

        /// <summary>
        /// 类内的枚举类型（没到用）
        /// </summary>
        public List<CodeEnum> Enums { get; set; } = new List<CodeEnum>();

        /// <summary>
        /// 继承的父类列表
        /// </summary>
        public List<CodeGeneralization> Generalizations { get; set; } = new List<CodeGeneralization>();

        /// <summary>
        /// 实现的接口列表
        /// </summary>
        public List<CodeRealization> Realizations { get; set; } = new List<CodeRealization>();

        /// <summary>
        /// 类的依赖关系
        /// </summary>
        public List<CodeDependency> Dependencies { get; set; } = new List<CodeDependency>();

        /// <summary>
        /// 类的关联关系
        /// </summary>
        public List<CodeAssociation> Associations { get; set; } = new List<CodeAssociation>();

        /// <summary>
        /// 类的关联关系
        /// </summary>
        public List<CodeUnidirectionalAssociation> UnidirectionaAssociations { get; set; } = new List<CodeUnidirectionalAssociation>();

        /// <summary>
        /// 类的组合关系
        /// </summary>
        public List<CodeComposition> Compositions { get; set; } = new List<CodeComposition>();

        /// <summary>
        /// 类的聚合关系
        /// </summary>
        public List<CodeAggregation> Aggregations { get; set; } = new List<CodeAggregation>();


    }
}
