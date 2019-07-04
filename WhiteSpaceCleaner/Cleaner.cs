using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WhiteSpaceCleaner
{
    class Cleaner
    {
        public static void Clean(ParseOptions options)
        {
            Console.WriteLine("Parsed options: " + CommandLine.Parser.Default.FormatCommandLine(options));

            DirectoryInfo dirInfo = new DirectoryInfo(options.Path);
            if (!dirInfo.Exists)
            {
                Console.WriteLine("The specified directory doesn't exists");
                return;
            }

            // Match all whitespace with more than 1 space at least
            Regex matchWhiteSpace = new Regex("\\b ( +)\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match match;

            var extensions = options.FileExt.Select(e =>
            {
                if (e.StartsWith("*"))
                    return e;
                if (e.StartsWith("."))
                    return "*" + e;
                return "*." + e;
            });
            foreach (var ext in extensions)
            {
                foreach (var fi in dirInfo.EnumerateFiles(ext, SearchOption.AllDirectories))
                {
                    string[] lines = File.ReadAllLines(fi.FullName);
                    string previousLine = "", currentLine = "";
                    bool modified = false;

                    // Process 1 line at a time
                    for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                    {
                        previousLine = currentLine;
                        currentLine = lines[lineIndex];

                        // Ignore defines
                        if (currentLine.Contains("#define"))
                            continue;

                        // Ignore comments
                        if (currentLine.TrimStart(' ').StartsWith("/") || currentLine.TrimStart(' ').StartsWith("*"))
                            continue;

                        match = matchWhiteSpace.Match(currentLine);
                        if (match.Success)
                        {
                            int matchIndex = match.Groups[1].Index;
                            int matchLength = match.Groups[1].Length;
                            int spacesToAddBeforeComment = 0;
                            int currentLineCommentIndex = currentLine.IndexOf("//");
                            if (currentLineCommentIndex != -1)
                            {
                                // Ignore comment-only lines
                                if (currentLineCommentIndex < matchIndex)
                                    continue;

                                // Try to keep the comments // at the end of the same line aligned by checking the line before and after and adding back space as needed
                                if (currentLine.Substring(matchIndex).Contains("//"))
                                {
                                    string nextLine = (lineIndex + 1) < lines.Length ? lines[lineIndex + 1] : "";

                                    int previousLineCommentIndex = previousLine.IndexOf("//");
                                    // Ignore lines with just comments
                                    if (previousLineCommentIndex <= matchIndex + matchLength)
                                        previousLineCommentIndex = -1;

                                    // Ignore lines with just comments
                                    int nextLineCommentIndex = nextLine.IndexOf("//");
                                    if (nextLineCommentIndex <= matchIndex + matchLength)
                                        nextLineCommentIndex = -1;

                                    // Add spaces only if the // are aligned
                                    if ((currentLineCommentIndex == previousLineCommentIndex && currentLineCommentIndex == nextLineCommentIndex)
                                     || (currentLineCommentIndex == previousLineCommentIndex && nextLineCommentIndex == -1)
                                     || (currentLineCommentIndex == nextLineCommentIndex && previousLineCommentIndex == -1))
                                    {
                                        spacesToAddBeforeComment = matchLength;
                                    }
                                }
                            }

                            // Add the spaces before // if needed
                            if (spacesToAddBeforeComment != 0)
                            {
                                currentLine = currentLine.Insert(currentLineCommentIndex, " ".PadLeft(spacesToAddBeforeComment));
                            }

                            // Remove whitespace
                            currentLine = currentLine.Remove(matchIndex, matchLength);

                            lines[lineIndex] = currentLine;
                            modified = true;
                        }
                    }

                    if (modified)
                        File.WriteAllLines(fi.FullName, lines);
                }
            }
        }
    }
}
