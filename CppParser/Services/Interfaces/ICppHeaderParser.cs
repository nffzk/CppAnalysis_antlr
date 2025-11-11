using CppParser.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CppParser.Services.Interfaces
{
    public interface ICppHeaderParser
    {
        CodeHeaderFile ParseHeaderFile(string filePath);
        CodeHeaderFile ParseHeaderContent(string content, string fileName = "unknown.h");
    }
}
