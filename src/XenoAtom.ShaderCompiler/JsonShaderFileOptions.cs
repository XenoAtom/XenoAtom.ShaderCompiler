// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Text.Json.Serialization;
using static XenoAtom.ShaderCompiler.ShaderCompilerConstants;

namespace XenoAtom.ShaderCompiler
{
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

        [JsonPropertyName(ShaderCompilerOption_output_kind)]
        public string? OutputKind { get; set; }

        [JsonPropertyName(ShaderCompilerOption_stage_selection)]
        public string? StageSelection { get; set; }

        [JsonPropertyName(ShaderCompilerOption_entry_point)]
        public string? EntryPoint { get; set; }

        [JsonPropertyName(ShaderCompilerOption_source_language)]
        public string? SourceLanguage { get; set; }

        [JsonPropertyName(ShaderCompilerOption_optimization_level)]
        public string? OptimizationLevel { get; set; }

        [JsonPropertyName(ShaderCompilerOption_invert_y)]
        public bool? InvertY { get; set; }

        [JsonPropertyName(ShaderCompilerOption_target_env)]
        public string? TargetEnv { get; set; }

        [JsonPropertyName(ShaderCompilerOption_shader_stage)]
        public string? ShaderStage { get; set; }

        [JsonPropertyName(ShaderCompilerOption_target_spv)]
        public string? TargetSpv { get; set; }

        [JsonPropertyName(ShaderCompilerOption_generate_debug)]
        public bool? GeneratedDebug { get; set; }

        [JsonPropertyName(ShaderCompilerOption_hlsl_16bit_types)]
        public bool? Hlsl16BitTypes { get; set; }

        [JsonPropertyName(ShaderCompilerOption_hlsl_offsets)]
        public bool? HlslOffsets { get; set; }

        [JsonPropertyName(ShaderCompilerOption_hlsl_functionality1)]
        public bool? HlslFunctionality1 { get; set; }

        [JsonPropertyName(ShaderCompilerOption_auto_map_locations)]
        public bool? AutoMapLocations { get; set; }

        [JsonPropertyName(ShaderCompilerOption_auto_bind_uniforms)]
        public bool? AutoBindUniforms { get; set; }

        [JsonPropertyName(ShaderCompilerOption_hlsl_iomap)]
        public bool? HlslIomap { get; set; }

        [JsonPropertyName(ShaderCompilerOption_defines)]
        public string? Defines { get; set; }

#if SHADER_COMPILER_RUNTIME
        public virtual ShaderFileOptions ToRuntime()
        {
            var runtime = this is JsonShaderGlobalOptions ? (ShaderFileOptions)new ShaderGlobalOptions() :
                this is JsonShaderFile shaderFile ? new ShaderFile(shaderFile.InputFilePath!) : throw new InvalidOperationException($"Unexpected type {this.GetType().FullName}");

            runtime.OutputKind = OutputKind is null ? null : ArgumentParser.ParseOutputKind(OutputKind);
            runtime.StageSelection = StageSelection is null ? null : ArgumentParser.ParseStageSelection(StageSelection);
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