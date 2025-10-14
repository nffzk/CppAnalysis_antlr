using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CppParser.Models
{
    public class CppHeaderFile
    {
        public string FileName { get; set; }
        public List<CppClass> Classes { get; set; } = new List<CppClass>();
        public List<CppEnum> Enums { get; set; } = new List<CppEnum>();
        // public List<string> Includes { get; set; } = new List<string>();
    }
}
