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
    /// <summary>
    /// Helper class to generate a C# file from a SPIR-V binary.
    /// </summary>
    public class ShaderCompilerHelper
    {
        private static readonly Regex RegexMatchNonIdentifierCharacters = new(@"[^\w]+", RegexOptions.Compiled);

        /// <summary>
        /// Generates a C# file from a SPIR-V binary.
        /// </summary>
        /// <param name="spv">The binary data of SPIR-V.</param>
        /// <param name="csRelativeFilePath">The relative C# file path.</param>
        /// <param name="csNamespace">The top level namespace.</param>
        /// <param name="csClassName">The top level class name that will embed the SPIR-V binary.</param>
        /// <param name="description">An optional description for the SPIR-V binary.</param>
        /// <returns>A C# string representation of the SPIR-V binary, embeddable in a C# compilation pipeline.</returns>
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
                    builder.AppendLine($"public static partial class {csNames[i]}");
                    builder.OpenBlock();
                }
                var csFinalName = csNames[^1];

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

                builder.AppendLine("#if NET5_0_OR_GREATER || NETSTANDARD2_1");
                builder.AppendLine($"public static ReadOnlySpan<byte> {csFinalName} => new byte[]");
                builder.AppendLine("#else");
                builder.AppendLine($"public static readonly byte[] {csFinalName} = new byte[]");
                builder.AppendLine("#endif");
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