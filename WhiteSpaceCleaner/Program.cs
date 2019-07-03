using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhiteSpaceCleaner
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<ParseOptions>(args)
                   .WithParsed<ParseOptions>(options => Cleaner.Clean(options));
        }
    }
}
