using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CppParser.Models
{
    public abstract class CppElement
    {
        public string Name { get; set; }
        public string Visibility { get; set; } = "public";
    }
}
