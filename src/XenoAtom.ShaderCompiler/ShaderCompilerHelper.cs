// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System;
using System.Globalization;
using System.Linq;

namespace XenoAtom.ShaderCompiler
{
    internal class ShaderCompilerHelper
    {
        private static readonly Regex RegexMatchNonIdentifierCharacters = new(@"[^\w]+", RegexOptions.Compiled);

        public static string GenerateCSharpFile(ReadOnlySpan<byte> spv, string csRelativeFilePath, string csNamespace, string csClassName, string? description)
        {
            var csNames = csRelativeFilePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            csNames[^1] = Path.GetFileNameWithoutExtension(csNames[^1]); // Remove .cs extension
            for (var i = 0; i < csNames.Length; i++)
            {
                csNames[i] = SanitizeName(csNames[i]);
            }

            var builder = new StringBuilderIndented();
            builder.AppendLine("using System;");
            builder.AppendLine();

            if (!string.IsNullOrEmpty(csNamespace))
            {
                builder.AppendLine($"namespace {csNamespace}");
                builder.OpenBlock();
            }
            {
                builder.AppendLine($"internal static partial class {csClassName}");
                builder.OpenBlock();
                for (int i = 0; i < csNames.Length - 1; i++)
                {
                    // Append the description
                    if (description != null)
                    {
                        var descriptionLines = description.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries);

                        builder.AppendLine("/// <summary>");
                        foreach (var descriptionLine in descriptionLines)
                        {
                            builder.AppendLine($"/// {descriptionLine}");
                        }
                        builder.AppendLine("/// </summary>");
                    }

                    builder.AppendLine($"public static partial class {csNames[i]}");
                    builder.OpenBlock();
                }
                var csFinalName = csNames[^1];
                builder.AppendLine($"public static ReadOnlySpan<byte> {csFinalName} => new byte[]");
                builder.OpenBlock();
                builder.AppendLine($"{string.Join(", ", spv.ToArray().Select(b => b.ToString(CultureInfo.InvariantCulture)))}");
                builder.Unindent();
                builder.AppendLine("};");

                for (int i = 0; i < csNames.Length - 1; i++)
                {
                    builder.CloseBlock();
                }

                builder.CloseBlock();
            }

            if (!string.IsNullOrEmpty(csNamespace))
            {
                builder.CloseBlock();
            }

            var csContent = builder.ToString();
            return csContent;

            static string SanitizeName(string name) => RegexMatchNonIdentifierCharacters.Replace(name, "_");
        }

        private class StringBuilderIndented
        {
            private readonly StringBuilder _builder = new();
            private int _indentLevel;

            public void AppendLine(string line)
            {
                for (var i = 0; i < _indentLevel; i++)
                {
                    _builder.Append("    ");
                }
                _builder.AppendLine(line);
            }

            public void AppendLine()
            {
                for (var i = 0; i < _indentLevel; i++)
                {
                    _builder.Append("    ");
                }
                _builder.AppendLine();
            }

            public void OpenBlock()
            {
                AppendLine("{");
                Indent();
            }

            public void CloseBlock()
            {
                Unindent();
                AppendLine("}");
            }

            public void Indent()
            {
                _indentLevel++;
            }

            public void Unindent()
            {
                _indentLevel--;
            }

            public override string ToString()
            {
                return _builder.ToString();
            }
        }

    }
}