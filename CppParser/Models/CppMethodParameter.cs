using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CppParser.Models
{
    public class CppMethodParameter : CppProperty
    {
        public bool IsRValueReference { get; set; }
    }
}
