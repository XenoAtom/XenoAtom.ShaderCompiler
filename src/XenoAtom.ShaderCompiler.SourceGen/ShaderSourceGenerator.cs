// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.
using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace XenoAtom.ShaderCompiler.SourceGen
{
    /// <summary>
    /// Source generator to include the SPIR-V binary as a C# file.
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    internal class ShaderSourceGenerator : IIncrementalGenerator
    {
        public const string BuildMetadataSourceGenMetadata = $"build_metadata.AdditionalFiles." + nameof(ShaderCompilerConstants.ShaderCompile_SourceGenerator);
        public const string BuildMetadataSourceRelativeCSharpFile = $"build_metadata.AdditionalFiles." + nameof(ShaderCompilerConstants.ShaderCompile_RelativePathCSharp);
        public const string BuildMetadataSourceOutputKind = $"build_metadata.AdditionalFiles." + nameof(ShaderCompilerConstants.ShaderCompilerOption_output_kind);
        public const string BuildPropertyShaderRootNamespace = "build_property." + nameof(ShaderCompilerConstants.ShaderCompilerGlobalOption_root_namespace);
        public const string BuildPropertyShaderClassName = "build_property." + nameof(ShaderCompilerConstants.ShaderCompilerGlobalOption_class_name);
        public const string BuildPropertyTestEmptySourceGenerator = "build_property." + nameof(ShaderCompilerConstants.ShaderCompilerGlobalOption_test_empty_source_generator);


        //public const string LogPath = "C:\\code\\XenoAtom\\XenoAtom.ShaderCompiler\\src\\XenoAtom.ShaderCompiler.Tests\\obj\\Debug\\net8.0\\ShaderCompiler_SourceGenerator.log";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //File.AppendAllText(LogPath, "Called\n");
            var filesProvider = context.AdditionalTextsProvider.Combine(context.AnalyzerConfigOptionsProvider).Select(
                    ((AdditionalText, AnalyzerConfigOptionsProvider) tuple, CancellationToken cancellationToken) =>
                    {
                        var (additionalText, analyzerConfigOptions) = tuple;
                        var options = analyzerConfigOptions.GetOptions(additionalText);

                        if (!options.TryGetValue(BuildMetadataSourceOutputKind, out var outputKind) || !string.Equals(outputKind, "csharp", StringComparison.OrdinalIgnoreCase))
                        {
                            return ((string?)null, (string?)null);
                        }

                        if (!analyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyShaderRootNamespace, out var csNamespace) || string.IsNullOrEmpty(csNamespace))
                        {
                            csNamespace = "";
                        }

                        if (!analyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyShaderClassName, out var csClassName) || string.IsNullOrEmpty(csClassName))
                        {
                            csClassName = "CompiledShaders";
                        }

                        if (options.TryGetValue(BuildMetadataSourceGenMetadata, out var sourceGenEnabled) &&
                            string.Equals(sourceGenEnabled, "true", StringComparison.OrdinalIgnoreCase) &&
                            options.TryGetValue(BuildMetadataSourceRelativeCSharpFile, out var csRelativeFilePath) && !string.IsNullOrEmpty(csRelativeFilePath))
                        {
                            SourceText? sourceText = null;
                            // Used for internal testing (case of Incremental Source Generator without a build to simulate the behavior in the IDE)
                            if (!analyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyTestEmptySourceGenerator, out var testEmptySourceGen) ||
                                !string.Equals(testEmptySourceGen, "true", StringComparison.OrdinalIgnoreCase))
                            {
                                sourceText = additionalText.GetText();
                            }
                            // We generate an empty file if the sourceText is null
                            // This happens when the IDE hasn't built anything, but we still want to generate a file with accessible properties for the shaders.
                            var text = sourceText != null ? sourceText.ToString() : ShaderCompilerHelper.GenerateCSharpFile(Array.Empty<byte>(), csRelativeFilePath, csNamespace, csClassName, null);
                            
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
