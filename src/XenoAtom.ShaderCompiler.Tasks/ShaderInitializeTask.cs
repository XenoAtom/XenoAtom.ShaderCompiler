// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Text;

namespace XenoAtom.ShaderCompiler.Tasks
{
    public class ShaderInitializeTask : Task
    {
        [Required]
        public string? CacheDirectory { get; set; }

        [Required]
        public string? CacheCSharpDirectory { get; set; }

        [Required]
        public string? BatchFile { get; set; }

        [Required]
        public ITaskItem[]? InputShaderFiles { get; set; }

        public ITaskItem[]? ShaderCompilerGlobalOption_include_directory { get; set; }

        public string? ShaderCompilerGlobalOption_root_namespace { get; set; }

        public string? ShaderCompilerGlobalOption_class_name { get; set; }

        public string? ShaderCompilerOption_output_kind { get; set; }
        
        public string? ShaderCompilerOption_entry_point { get; set;}

        public string? ShaderCompilerOption_source_language { get; set;}

        public string? ShaderCompilerOption_optimization_level { get; set;}

        public string? ShaderCompilerOption_invert_y { get; set;}

        public string? ShaderCompilerOption_target_env { get; set;}

        public string? ShaderCompilerOption_shader_stage { get; set;}

        public string? ShaderCompilerOption_target_spv { get; set;}

        public string? ShaderCompilerOption_generate_debug { get; set;}

        public string? ShaderCompilerOption_hlsl_16bit_types { get; set;}

        public string? ShaderCompilerOption_hlsl_offsets { get; set;}

        public string? ShaderCompilerOption_hlsl_functionality1 { get; set;}

        public string? ShaderCompilerOption_auto_map_locations { get; set;}

        public string? ShaderCompilerOption_auto_bind_uniforms { get; set;}

        public string? ShaderCompilerOption_hlsl_iomap { get; set;}

        public string? ShaderCompilerOption_defines { get; set; }
        
        [Output]
        public ITaskItem[]? OutputCompiledShaders { get; set; }

        [Output]
        public ITaskItem[]? ContentFiles { get; set; }

        public override bool Execute()
        {
            var cacheDirectory = CacheDirectory!;

            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }

            var batchFile = BatchFile;
            if (string.IsNullOrEmpty(batchFile))
            {
                Log.LogError($"BatchFile is not set for task {nameof(ShaderInitializeTask)}");
                return false;
            }
            
            var parentDirectoryBatchFile = Path.GetDirectoryName(batchFile);
            if (!string.IsNullOrEmpty(parentDirectoryBatchFile) && !Directory.Exists(parentDirectoryBatchFile))
            {
                Directory.CreateDirectory(parentDirectoryBatchFile);
            }

            var outputTasks = new ITaskItem[InputShaderFiles!.Length];

            var shaderGlobalOptions = new JsonShaderGlobalOptions
            {
                CacheDirectory = cacheDirectory,
                CacheCSharpDirectory = CacheCSharpDirectory,
                GenerateDepsFile = true,
                Incremental = true,

                RootNamespace = ToStringOpt(ShaderCompilerGlobalOption_root_namespace),
                ClassName = ToStringOpt(ShaderCompilerGlobalOption_class_name),
                OutputKind = ToStringOpt(ShaderCompilerOption_output_kind),
                
                EntryPoint = ToStringOpt(ShaderCompilerOption_entry_point),
                SourceLanguage = ToStringOpt(ShaderCompilerOption_source_language),
                OptimizationLevel = ToStringOpt(ShaderCompilerOption_optimization_level),
                InvertY = ToBool(ShaderCompilerOption_invert_y),
                TargetEnv = ToStringOpt(ShaderCompilerOption_target_env),
                ShaderStage = ToStringOpt(ShaderCompilerOption_shader_stage),
                TargetSpv = ToStringOpt(ShaderCompilerOption_target_spv),
                GeneratedDebug = ToBool(ShaderCompilerOption_generate_debug),
                Hlsl16BitTypes = ToBool(ShaderCompilerOption_hlsl_16bit_types),
                HlslOffsets = ToBool(ShaderCompilerOption_hlsl_offsets),
                HlslFunctionality1 = ToBool(ShaderCompilerOption_hlsl_functionality1),
                AutoMapLocations = ToBool(ShaderCompilerOption_auto_map_locations),
                AutoBindUniforms = ToBool(ShaderCompilerOption_auto_bind_uniforms),
                HlslIomap = ToBool(ShaderCompilerOption_hlsl_iomap),
                Defines = ToStringOpt(ShaderCompilerOption_defines),
            };

            if (ShaderCompilerGlobalOption_include_directory != null)
            {
                foreach (var includeDirectory in ShaderCompilerGlobalOption_include_directory)
                {
                    shaderGlobalOptions.IncludeDirectories.Add(includeDirectory.ItemSpec);
                }
            }

            // Parse the output kind
            // As we need to generate the proper output files, we need to parse the output kind
            if (!TryParseOutputKind(shaderGlobalOptions.OutputKind, out var baseOutputKind))
            {
                return false;
            }
            
            var contentFiles = new List<ITaskItem>();

            ShaderOutputKind? tarOutput = null;

            var depsBuilder = new StringBuilder();

            for (var i = 0; i < InputShaderFiles!.Length; i++)
            {
                var inputItem = InputShaderFiles[i];
                var sourceShaderFile = inputItem.GetMetadata("FullPath");

                var relativeOutputPath = string.IsNullOrEmpty(inputItem.GetMetadata("Link"))
                    ? $"{inputItem.GetMetadata("RelativeDir")}{inputItem.GetMetadata("Filename")}{inputItem.GetMetadata("Extension")}"
                    : inputItem.GetMetadata("Link");

                var relativeDepsPath = $"{relativeOutputPath}.deps";
                var relativeSpvPath = $"{relativeOutputPath}.spv";
                var depsPath = Path.Combine(cacheDirectory, relativeDepsPath);
                var spvPath = Path.Combine(cacheDirectory, relativeSpvPath);

                var item = new TaskItem(spvPath);
                item.SetMetadata(ShaderCompilerConstants.ShaderCompile_PathDeps, depsPath);
                item.SetMetadata(ShaderCompilerConstants.ShaderCompile_SourceFile, sourceShaderFile);

                var shaderFile = new JsonShaderFile()
                {
                    InputFilePath = sourceShaderFile,
                    OutputSpvPath = relativeSpvPath,
                    OutputDepsPath = relativeDepsPath,

                    OutputKind = ToStringOpt(inputItem.GetMetadata(nameof(ShaderCompilerOption_output_kind))),

                    Description = ToStringOpt(inputItem.GetMetadata(nameof(ShaderCompilerConstants.ShaderCompilerOption_description))),
                    EntryPoint = ToStringOpt(inputItem.GetMetadata(nameof(ShaderCompilerOption_entry_point))),
                    SourceLanguage = ToStringOpt(inputItem.GetMetadata(nameof(ShaderCompilerOption_source_language))),
                    OptimizationLevel = ToStringOpt(inputItem.GetMetadata(nameof(ShaderCompilerOption_optimization_level))),
                    InvertY = ToBool(inputItem.GetMetadata(nameof(ShaderCompilerOption_invert_y))),
                    TargetEnv = ToStringOpt(inputItem.GetMetadata(nameof(ShaderCompilerOption_target_env))),
                    TargetSpv = ToStringOpt(inputItem.GetMetadata(nameof(ShaderCompilerOption_target_spv))),
                    GeneratedDebug = ToBool(inputItem.GetMetadata(nameof(ShaderCompilerOption_generate_debug))),
                    Hlsl16BitTypes = ToBool(inputItem.GetMetadata(nameof(ShaderCompilerOption_hlsl_16bit_types))),
                    HlslOffsets = ToBool(inputItem.GetMetadata(nameof(ShaderCompilerOption_hlsl_offsets))),
                    HlslFunctionality1 = ToBool(inputItem.GetMetadata(nameof(ShaderCompilerOption_hlsl_functionality1))),
                    AutoMapLocations = ToBool(inputItem.GetMetadata(nameof(ShaderCompilerOption_auto_map_locations))),
                    AutoBindUniforms = ToBool(inputItem.GetMetadata(nameof(ShaderCompilerOption_auto_bind_uniforms))),
                    HlslIomap = ToBool(inputItem.GetMetadata(nameof(ShaderCompilerOption_hlsl_iomap))),
                    Defines = ToStringOpt(inputItem.GetMetadata(nameof(ShaderCompilerOption_defines))),
                };

                var outputKind = baseOutputKind;
                if (!string.IsNullOrEmpty(shaderFile.OutputKind) && !TryParseOutputKind(shaderFile.OutputKind, out outputKind))
                {
                    continue;
                }

                if (outputKind == ShaderOutputKind.CSharp)
                {
                    var sourceGenerateText = inputItem.GetMetadata(ShaderCompilerConstants.ShaderCompile_SourceGenerator);
                    var isSourceGenerate = string.Equals(sourceGenerateText, "true", StringComparison.OrdinalIgnoreCase);

                    if (isSourceGenerate)
                    {
                        var relativeCSharpPath = inputItem.GetMetadata("ShaderCompile_RelativePathCSharp");
                        shaderFile.OutputCsPath = relativeCSharpPath;
                    }
                }
                else if (outputKind == ShaderOutputKind.Content)
                {
                    var taskItem = new TaskItem(spvPath);
                    taskItem.SetMetadata("Link", relativeSpvPath);
                    taskItem.SetMetadata("CopyToOutputDirectory", "PreserveNewest");

                    contentFiles.Add(taskItem);
                }
                else
                {
                    if (tarOutput.HasValue && tarOutput.Value != outputKind)
                    {
                        Log.LogError($"Invalid output kind `{outputKind}` for `{shaderFile.InputFilePath}`. Only one output kind is allowed per batch file. Found `{tarOutput.Value}` and `{outputKind}`");
                        return false;
                    }

                    tarOutput = outputKind;
                }
                
                shaderGlobalOptions.InputFiles.Add(shaderFile);
                
                if (File.Exists(depsPath))
                {
                    ReadOnlySpan<string> deps = File.ReadAllLines(depsPath);
                    if (deps.Length > 0)
                    {
                        deps = deps.Slice(1); // The first line in the deps file is the hash of the options
                        depsBuilder.Length = 0;
                        for (var index = 0; index < deps.Length; index++)
                        {
                            var dep = deps[index];
                            if (index > 0)
                            {
                                depsBuilder.Append(';');
                            }
                            depsBuilder.Append(dep);
                        }

                        item.SetMetadata(ShaderCompilerConstants.ShaderCompile_IncludeDependencies, depsBuilder.ToString());
                    }
                    else
                    {
                        item.SetMetadata(ShaderCompilerConstants.ShaderCompile_IncludeDependencies, string.Empty);
                    }
                }
                else
                {
                    item.SetMetadata(ShaderCompilerConstants.ShaderCompile_IncludeDependencies, string.Empty);
                }

                outputTasks[i] = item;
            }

            if (tarOutput.HasValue)
            {
                var tarFileName = $"{(string.IsNullOrEmpty(ShaderCompilerGlobalOption_root_namespace) ? "" : $"{ShaderCompilerGlobalOption_root_namespace}.")}{ShaderCompilerGlobalOption_class_name}.{(tarOutput.Value == ShaderOutputKind.Tar ? "tar" : "tar.gz")}";
                var tarFilePath = Path.Combine(cacheDirectory, tarFileName);
                var taskItem = new TaskItem(tarFilePath);
                taskItem.SetMetadata("Link", tarFileName);
                taskItem.SetMetadata("CopyToOutputDirectory", "PreserveNewest");
                contentFiles.Add(taskItem);
            }

            shaderGlobalOptions.InputFiles.Sort((a, b) => string.Compare(a.InputFilePath, b.InputFilePath, System.StringComparison.Ordinal));
            
            var sourceGenOptions = new JsonSerializerOptions
            {
                TypeInfoResolver = JsonShaderGenerationContext.Default,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(shaderGlobalOptions, sourceGenOptions);

            string? previousJson = null;
            if (File.Exists(batchFile))
            {
                previousJson = File.ReadAllText(batchFile);
            }

            // Update the file only if it has changed (To allow the compiler task to not be triggered if the file has not changed)
            if (json != previousJson)
            {
                File.WriteAllText(batchFile, json);
            }

            OutputCompiledShaders = outputTasks;
            ContentFiles = contentFiles.ToArray();
            return true;
        }

        private static string? ToStringOpt(string? value) => string.IsNullOrEmpty(value) ? null : value;

        private static bool? ToBool(string? value)
        {
            if (value != null)
            {
                value = value.Trim();
                if (string.Equals("true", value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (string.Equals("false", value, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return null;
        }

        private bool TryParseOutputKind(string? outputKind, out ShaderOutputKind shaderOutputKind)
        {
            switch (outputKind)
            {
                case "csharp":
                    shaderOutputKind = ShaderOutputKind.CSharp;
                    return true;
                case "tar":
                    shaderOutputKind = ShaderOutputKind.Tar;
                    return true;
                case "tar.gz":
                    shaderOutputKind = ShaderOutputKind.TarGz;
                    return true;
                case "content":
                    shaderOutputKind = ShaderOutputKind.Content;
                    return true;
            }

            Log.LogError($"Invalid output kind `{outputKind}`. Valid values are: csharp, tar, tar.gz, content.");
            shaderOutputKind = ShaderOutputKind.CSharp;
            return false;
        }
    }
}
