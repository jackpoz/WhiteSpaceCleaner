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
        const string WhiteSpaceRegex = @"[^ \n#] ( +)[^ \n\/]";
        const string MultiDimArrayRegex = @"\[.+\]\[.+\][^;].*=$";

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
            Regex regexWhiteSpace = new Regex(WhiteSpaceRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase);            
            Regex regexMultiDimArray = new Regex(MultiDimArrayRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
                    if (options.SkipFiles.Contains(fi.Name))
                        continue;

                    string[] lines = File.ReadAllLines(fi.FullName);
                    string previousLine = "", currentLine = "";
                    bool modified = false;

                    bool isInsideComment = false;
                    bool isInsideEnum = false;
                    bool isInsideMultiDimArray = false;

                    // Process 1 line at a time
                    for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                    {
                        previousLine = currentLine;
                        currentLine = lines[lineIndex];

                        string currentLineTrimStart = currentLine.TrimStart(' ');

                        // Ignore multi-line comments
                        if (isInsideComment)
                        {
                            if (currentLine.TrimEnd(' ').EndsWith("*/"))
                            {
                                isInsideComment = false;
                            }

                            continue;
                        }
                        if (currentLineTrimStart.StartsWith("/*"))
                        {
                            isInsideComment = true;
                            continue;
                        }

                        // Ignore enums
                        if (isInsideEnum)
                        {
                            // Try to find the closing brace of enum declaration but skip comments
                            if (currentLine.IndexOf("};") > currentLine.IndexOf("//"))
                            {
                                isInsideEnum = false;
                            }

                            continue;
                        }
                        if (currentLineTrimStart.StartsWith("enum "))
                        {
                            isInsideEnum = true;
                            continue;
                        }

                        // Ignore multi-dimension arrays
                        if (isInsideMultiDimArray)
                        {
                            // Try to find the closing brace of array declaration but skip comments
                            if (currentLine.IndexOf("};") > currentLine.IndexOf("//"))
                            {
                                isInsideMultiDimArray = false;
                            }

                            continue;
                        }
                        if (regexMultiDimArray.IsMatch(currentLine))
                        {
                            isInsideMultiDimArray = true;
                            continue;
                        }

                        // Ignore defines/includes
                        if (currentLineTrimStart.StartsWith("#"))
                            continue;

                        // Ignore comments
                        if (currentLineTrimStart.StartsWith("/") || currentLineTrimStart.StartsWith("*"))
                            continue;

                        // Ignore typedef
                        if (currentLineTrimStart.StartsWith("typedef"))
                            continue;

                        // Skip multi-line macros
                        if (previousLine.EndsWith("\\"))
                            continue;

                        Match matchWhiteSpace = regexWhiteSpace.Match(currentLine);
                        if (matchWhiteSpace.Success)
                        {
                            int matchIndex = matchWhiteSpace.Groups[1].Index;
                            int matchLength = matchWhiteSpace.Groups[1].Length;
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

                            Console.WriteLine($"File {fi.FullName} line {lineIndex + 1} text '{currentLine}'");

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

                    if (modified && !options.CheckOnly)
                        File.WriteAllLines(fi.FullName, lines);
                }
            }
        }
    }
}
