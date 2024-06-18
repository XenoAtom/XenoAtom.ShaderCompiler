// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.
using System;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable RS1035

namespace XenoAtom.ShaderCompiler.SourceGen
{
    [Generator(LanguageNames.CSharp)]
    public class ShaderSourceGenerator : IIncrementalGenerator
    {
        public const string SourceGenMetadata = $"build_metadata.AdditionalFiles." + ShaderCompilerConstants.ShaderCompile_SourceGenerator;
        public const string SourceRelativeCSharpFile = $"build_metadata.AdditionalFiles." + ShaderCompilerConstants.ShaderCompile_RelativePathCSharp;

        //public const string LogPath = "C:\\code\\XenoAtom\\XenoAtom.ShaderCompiler\\src\\XenoAtom.ShaderCompiler.Tests\\obj\\Debug\\net8.0\\ShaderCompiler_SourceGenerator.log";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //File.AppendAllText(LogPath, "Called\n");
            var filesProvider = context.AdditionalTextsProvider.Combine(context.AnalyzerConfigOptionsProvider).Select(
                    ((AdditionalText, AnalyzerConfigOptionsProvider) tuple, CancellationToken cancellationToken) =>
                    {
                        var (additionalText, analyzerConfigOptions) = tuple;
                        //File.AppendAllText(LogPath, $"File {additionalText.Path}\n");
                        var options = analyzerConfigOptions.GetOptions(additionalText);

                        if (!analyzerConfigOptions.GlobalOptions.TryGetValue(nameof(ShaderCompilerConstants.ShaderCompilerOption_root_namespace), out var csNamespace) || string.IsNullOrEmpty(csNamespace))
                        {
                            csNamespace = "";
                        }

                        if (!analyzerConfigOptions.GlobalOptions.TryGetValue(nameof(ShaderCompilerConstants.ShaderCompilerOption_class_name), out var csClassName) || string.IsNullOrEmpty(csClassName))
                        {
                            csClassName = "CompiledShaders";
                        }

                        if (options.TryGetValue(SourceGenMetadata, out var sourceGenEnabled) &&
                            string.Equals(sourceGenEnabled, "true", StringComparison.OrdinalIgnoreCase) &&
                            options.TryGetValue(SourceRelativeCSharpFile, out var csRelativeFilePath) && !string.IsNullOrEmpty(csRelativeFilePath))
                        {
                            string text;
                            var csFilePath = additionalText.Path;
                            if (!string.IsNullOrEmpty(csFilePath) && File.Exists(csFilePath))
                            {
                                text = File.ReadAllText(csFilePath);
                            }
                            else
                            {
                                text = ShaderCompilerHelper.GenerateCSharpFile(Array.Empty<byte>(), csRelativeFilePath, csNamespace, csClassName);
                            }
                            
                            return (csRelativeFilePath, text);
                        }

                        return ((string?)null, (string?)null);
                    })
                .WithTrackingName(ShaderCompilerConstants.ShaderCompilerTrackingName);


            context.RegisterSourceOutput(filesProvider, (spc, nameAndContent) =>
            {
                if (nameAndContent.Item1 != null && nameAndContent.Item2 != null)
                {
                    spc.AddSource(nameAndContent.Item1, nameAndContent.Item2);
                }
            });
        }
    }
}
