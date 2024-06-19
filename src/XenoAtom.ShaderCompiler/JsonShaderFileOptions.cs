// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Text.Json.Serialization;
using static XenoAtom.ShaderCompiler.ShaderCompilerConstants;

namespace XenoAtom.ShaderCompiler
{
    /// <summary>
    /// Represents a shader file with compiler options used for Json serialization.
    /// </summary>
#if SHADER_COMPILER_RUNTIME
    public
#else
    internal
#endif
    abstract class JsonShaderFileOptions
    {
        internal JsonShaderFileOptions()
        {
        }

        /// <summary>
        /// Gets or sets the output kind.
        /// </summary>
        [JsonPropertyName(ShaderCompilerOption_output_kind)]
        public string? OutputKind { get; set; }

        /// <summary>
        /// Gets or sets the compiler mode.
        /// </summary>
        [JsonPropertyName("compiler-mode")]
        public string? CompilerMode { get; set; }

        /// <summary>
        /// Gets or sets the entry point.
        /// </summary>
        [JsonPropertyName(ShaderCompilerOption_entry_point)]
        public string? EntryPoint { get; set; }

        /// <summary>
        /// Gets or sets the source language (hlsl or glsl).
        /// </summary>
        [JsonPropertyName(ShaderCompilerOption_source_language)]
        public string? SourceLanguage { get; set; }

        /// <summary>
        /// Gets or sets the optimization level (O, O0, Os).
        /// </summary>
        [JsonPropertyName(ShaderCompilerOption_optimization_level)]
        public string? OptimizationLevel { get; set; }

        /// <summary>
        /// Gets or sets whether to invert Y (HLSL only)
        /// </summary>
        [JsonPropertyName(ShaderCompilerOption_invert_y)]
        public bool? InvertY { get; set; }

        /// <summary>
        /// Gets or sets the target environment (vulkan, opengl...).
        /// </summary>
        [JsonPropertyName(ShaderCompilerOption_target_env)]
        public string? TargetEnv { get; set; }

        /// <summary>
        /// Gets or sets the shader stage (vertex, fragment...).
        /// </summary>
        [JsonPropertyName(ShaderCompilerOption_shader_stage)]
        public string? ShaderStage { get; set; }

        /// <summary>
        /// Gets or sets the target spv (spv1.0, spv1.1...).
        /// </summary>
        [JsonPropertyName(ShaderCompilerOption_target_spv)]
        public string? TargetSpv { get; set; }

        /// <summary>
        /// Gets or sets whether to generate debug information.
        /// </summary>
        [JsonPropertyName(ShaderCompilerOption_generate_debug)]
        public bool? GeneratedDebug { get; set; }

        /// <summary>
        /// Gets or sets whether to use hlsl 16bit types.
        /// </summary>
        [JsonPropertyName(ShaderCompilerOption_hlsl_16bit_types)]
        public bool? Hlsl16BitTypes { get; set; }

        /// <summary>
        /// Gets or sets whether to use hlsl offsets.
        /// </summary>
        [JsonPropertyName(ShaderCompilerOption_hlsl_offsets)]
        public bool? HlslOffsets { get; set; }

        /// <summary>
        /// Gets or sets whether to use hlsl functionality1.
        /// </summary>
        [JsonPropertyName(ShaderCompilerOption_hlsl_functionality1)]
        public bool? HlslFunctionality1 { get; set; }

        /// <summary>
        /// Gets or sets whether to auto map locations.
        /// </summary>
        [JsonPropertyName(ShaderCompilerOption_auto_map_locations)]
        public bool? AutoMapLocations { get; set; }

        /// <summary>
        /// Gets or sets whether to auto bind uniforms.
        /// </summary>
        [JsonPropertyName(ShaderCompilerOption_auto_bind_uniforms)]
        public bool? AutoBindUniforms { get; set; }

        /// <summary>
        /// Gets or sets whether to use hlsl iomap.
        /// </summary>
        [JsonPropertyName(ShaderCompilerOption_hlsl_iomap)]
        public bool? HlslIomap { get; set; }

        /// <summary>
        /// Gets or sets the preprocessor defines for this particular file.
        /// </summary>
        [JsonPropertyName(ShaderCompilerOption_defines)]
        public string? Defines { get; set; }

#if SHADER_COMPILER_RUNTIME
        /// <summary>
        /// Creates a runtime instance of <see cref="ShaderFileOptions"/> from this instance.
        /// </summary>
        public virtual ShaderFileOptions ToRuntime()
        {
            var runtime = this is JsonShaderGlobalOptions ? (ShaderFileOptions)new ShaderGlobalOptions() :
                this is JsonShaderFile shaderFile ? new ShaderFile(shaderFile.InputFilePath!) : throw new InvalidOperationException($"Unexpected type {this.GetType().FullName}");

            runtime.OutputKind = OutputKind is null ? null : ArgumentParser.ParseOutputKind(OutputKind);
            runtime.CompilerMode = CompilerMode is null ? null : ArgumentParser.ParseCompilerMode(CompilerMode);
            runtime.EntryPoint = EntryPoint;
            runtime.SourceLanguage = SourceLanguage is null ? null : ArgumentParser.ParseSourceLanguage(SourceLanguage);
            runtime.OptimizationLevel = OptimizationLevel is null ? null : ArgumentParser.ParseOptimizationLevel(OptimizationLevel);
            runtime.InvertY = InvertY;
            runtime.TargetEnv = TargetEnv is null ? null : ArgumentParser.ParseTargetEnv(TargetEnv);
            runtime.ShaderStage = ShaderStage is null ? null : ArgumentParser.ParseShaderStage(ShaderStage);
            runtime.TargetSpv = TargetSpv is null ? null : ArgumentParser.ParseTargetSpv(TargetSpv);
            runtime.GeneratedDebug = GeneratedDebug;
            runtime.Hlsl16BitTypes = Hlsl16BitTypes;
            runtime.HlslOffsets = HlslOffsets;
            runtime.HlslFunctionality1 = HlslFunctionality1;
            runtime.AutoMapLocations = AutoMapLocations;
            runtime.AutoBindUniforms = AutoBindUniforms;
            runtime.HlslIomap = HlslIomap;

            var defines = Defines;
            if (!string.IsNullOrEmpty(defines))
            {

                var defineList = defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var defineKeyValue in defineList)
                {
                    var define = defineKeyValue.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    runtime.Defines.Add(new(define[0], define.Length > 1 ? define[1] : null));
                }
            }

            return runtime;
        }
#endif
    }
}