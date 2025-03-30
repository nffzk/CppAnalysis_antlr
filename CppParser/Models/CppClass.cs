
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CppParser.Enums;

namespace CppParser.Models
{
    public class CppClass : CppElement
    {
        /// <summary>
        /// class类型，默认为"class"。包括"class对应类图中的类"、"struct对应类图中的类"、"interface对应类图中的接口"
        /// </summary>
        public EnumClassType Stereotype { get; set; } = EnumClassType.Class;

        /// <summary>
        /// 类的属性、组合属性、聚合属性、关联属性
        /// </summary>
        public List<CppProperty> Properties { get; set; } = new List<CppProperty>();

        /// <summary>
        /// 类的方法
        /// </summary>
        public List<CppMethod> Methods { get; set; } = new List<CppMethod>();

        /// <summary>
        /// 类内的枚举类型
        /// </summary>
        public List<CppEnum> Enums { get; set; } = new List<CppEnum>();

        /// <summary>
        /// 继承的父类列表
        /// </summary>
        public List<CppGeneralization> Generalizations { get; set; } = new List<CppGeneralization>();
        /// <summary>
        /// 实现的接口列表
        /// </summary>
        public List<CppRealization> Realizations { get; set; } = new List<CppRealization>();

        /// <summary>
        /// 类的依赖关系
        /// </summary>
        public List<CppDependency> Dependencies { get; set; } = new List<CppDependency>();

        /// <summary>
        /// 类的关联关系
        /// </summary>
        public List<CppAssociation> Associations { get; set; } = new List<CppAssociation>();

        /// <summary>
        /// 类的组合关系
        /// </summary>
        public List<CppComposition> Compositions { get; set; } = new List<CppComposition>();

        /// <summary>
        /// 类的聚合关系
        /// </summary>
        public List<CppAggregation> Aggregations { get; set; } = new List<CppAggregation>();


    }
}
