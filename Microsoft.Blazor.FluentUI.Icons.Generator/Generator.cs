using BrotliSharpLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Typography.OpenFont;
using Typography.OpenFont.WebFont;

#nullable enable

namespace Microsoft.Blazor.FluentUI.Icons.Generator
{
    [Generator]
    public class Generator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            Woff2DefaultBrotliDecompressFunc.DecompressHandler = (byte[] compressedBytes, Stream output) =>
            {
                bool result = false;
                try
                {
                    using (MemoryStream ms = new(compressedBytes))
                    using (var decompressStream = new BrotliStream(ms, CompressionMode.Decompress))
                    {
                        decompressStream.CopyTo(output);
                    }

                    result = true;
                }
                catch (Exception)
                {

                }
                return result;
            };

            Dictionary<string, string[]>? fontSources = new();

            IEnumerable<AdditionalText> FontFiles = context.AdditionalFiles.Where(at => at.Path.EndsWith(".woff2"));


            // Get the Class name to use for generating the source from the itemgroup item's ClassName attribute
            foreach (AdditionalText? item in FontFiles)
            {
                context.AnalyzerConfigOptions.GetOptions(item).TryGetValue("build_metadata.additionalfiles.FontNamespace", out string? fontNamespace);
                context.AnalyzerConfigOptions.GetOptions(item).TryGetValue("build_metadata.additionalfiles.FontClass", out string? fontClass);
                fontSources[item.Path] = new string[] {
                    fontNamespace!,
                    fontClass!,
                };
            }

            StringBuilder? sb = new();
            Regex? regex = new Regex(@"(\w*)_(\d*)_(\w*)");
            HashSet<(string friendlyname, string shortname)> constants = new();
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

            sb.AppendLine("using System.Collections.Generic;");

            foreach (KeyValuePair<string, string[]> fontSource in fontSources)
            {
                constants.Clear();
                string? fontNamespace = fontSource.Value[0];
                string? fontClass = fontSource.Value[1];

                string? fontFileName = Path.GetFileName(fontSource.Key);
                OpenFontReader? reader = new();
                Typeface? t = reader.Read(File.OpenRead(fontSource.Key));

                List<uint>? unicodes = new();
                t.CollectUnicode(unicodes);

                Dictionary<int, string[]>? glyphs = new();

                foreach (uint unicode in unicodes)
                {
                    string? str = String.Format("{0:X}", unicode);

                    ushort index = t.GetGlyphIndex((int)unicode);

                    glyphs[index] = new[] { "", str };
                }

                foreach (GlyphNameMap gname in t.GetGlyphNameIter())
                {
                    glyphs[gname.glyphIndex] = new string[]
                    {
                        gname.glyphName,
                        glyphs[gname.glyphIndex][1]
                    };
                }
                sb.AppendLine($"namespace {fontNamespace}");
                sb.AppendLine("{");
                sb.AppendLine($"\tpublic static partial class {fontClass}");
                sb.AppendLine("\t{");
                sb.AppendLine($"\t\tpublic const string Filename = \"{fontFileName}\";");
                sb.AppendLine("\t\tpublic static Dictionary<string, string> IconMap = new()");
                sb.AppendLine("\t\t{");

                foreach (KeyValuePair<int, string[]> gvp in glyphs)
                {
                    string? name = gvp.Value[0];
                    string? unicode = gvp.Value[1].PadLeft(4, '0');

                    if (string.IsNullOrEmpty(name))
                        continue;

                    name = name.Replace("ic_fluent_", "");
                    sb.AppendLine($"\t\t\t{{ \"{name}\", \"\\u{unicode.ToLowerInvariant()}\"}},");

                    MatchCollection? matches = regex.Matches(name);
                    if (matches.Count == 0)
                        continue;

                    string shortname = matches[0].Groups[1].Value;
                    //int size = int.Parse(matches[0].Groups[2].Value);
                    //bool isFilled = matches[0].Groups[3].Value == "filled" ? true : false;

                    string friendlyname = textInfo.ToTitleCase(shortname).Replace("_", "");
                    constants.Add((friendlyname, shortname));
                }
                sb.AppendLine("\t\t};");

                foreach ((string friendlyname, string shortname) item in constants.OrderBy(x => x.friendlyname))
                {
                    sb.AppendLine($"\t\tpublic const string {item.friendlyname} = \"{item.shortname}\";");
                }

                sb.AppendLine("\t}");
                sb.AppendLine("}");
            }

            context.AddSource($"FluentSystemIcons.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required for this one
#if DEBUG
            //if (!Debugger.IsAttached)
            //{
            //    Debugger.Launch();
            //}
#endif
        }
    }
}
