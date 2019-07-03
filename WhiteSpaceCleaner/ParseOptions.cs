using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace WhiteSpaceCleaner
{
    public class ParseOptions
    {
        [Option(Required = true)]
        public string Path { get; private set; }
        [Option]
        public bool CheckOnly { get; private set; }
        [Option(Separator = ',')]
        public IEnumerable<string> FileExt { get; private set; }

        public ParseOptions() : this(null, false, null)
        { }

        public ParseOptions(string path, bool checkOnly, IEnumerable<string> fileExt)
        {
            this.Path = path;
            this.CheckOnly = checkOnly;
            this.FileExt = fileExt;
            if (this.FileExt == null)
                this.FileExt = new List<string>();
        }
    }
}
