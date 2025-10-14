using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CppParser.Models
{
    public class CppClass : CppElement
    {
        public string Stereotype { get; set; } = "class";
        public List<CppProperty> Properties { get; set; } = new List<CppProperty>();
        public List<CppMethod> Methods { get; set; } = new List<CppMethod>();
        public List<CppEnum> Enums { get; set; } = new List<CppEnum>();
        public List<string> BaseClasses { get; set; } = new List<string>();
    }
}
