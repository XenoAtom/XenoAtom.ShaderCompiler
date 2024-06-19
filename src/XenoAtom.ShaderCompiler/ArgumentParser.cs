// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using static XenoAtom.Interop.libshaderc;

namespace XenoAtom.ShaderCompiler;

internal static class ArgumentParser
{
    private static readonly Dictionary<string, shaderc_env_version> EnvVersionMap = new(StringComparer.Ordinal)
    {
        { "vulkan", shaderc_env_version.shaderc_env_version_vulkan_1_0 },
        { "vulkan1.0", shaderc_env_version.shaderc_env_version_vulkan_1_0 },
        { "vulkan1.1", shaderc_env_version.shaderc_env_version_vulkan_1_1 },
        { "vulkan1.2", shaderc_env_version.shaderc_env_version_vulkan_1_2 },
        { "opengl", shaderc_env_version.shaderc_env_version_opengl_4_5 },
        { "opengl4.5", shaderc_env_version.shaderc_env_version_opengl_4_5 },
    };

    private static readonly Dictionary<string, shaderc_spirv_version> SpirvVersionMap = new(StringComparer.Ordinal)
    {
        { "spv1.0", shaderc_spirv_version.shaderc_spirv_version_1_0 },
        { "spv1.1", shaderc_spirv_version.shaderc_spirv_version_1_1 },
        { "spv1.2", shaderc_spirv_version.shaderc_spirv_version_1_2 },
        { "spv1.3", shaderc_spirv_version.shaderc_spirv_version_1_3 },
        { "spv1.4", shaderc_spirv_version.shaderc_spirv_version_1_4 },
        { "spv1.5", shaderc_spirv_version.shaderc_spirv_version_1_5 },
        { "spv1.6", shaderc_spirv_version.shaderc_spirv_version_1_6 },
    };

    private static readonly Dictionary<string, shaderc_shader_kind> ShaderKindMap = new(StringComparer.Ordinal)
    {
        { "vertex", shaderc_shader_kind.shaderc_vertex_shader },
        { "fragment", shaderc_shader_kind.shaderc_fragment_shader },
        { "compute", shaderc_shader_kind.shaderc_compute_shader },
        { "geometry", shaderc_shader_kind.shaderc_geometry_shader },
        { "tesscontrol", shaderc_shader_kind.shaderc_tess_control_shader },
        { "tesseval", shaderc_shader_kind.shaderc_tess_evaluation_shader },
        { "raygen", shaderc_shader_kind.shaderc_raygen_shader },
        { "anyhit", shaderc_shader_kind.shaderc_anyhit_shader },
        { "closesthit", shaderc_shader_kind.shaderc_closesthit_shader },
        { "miss", shaderc_shader_kind.shaderc_miss_shader },
        { "intersection", shaderc_shader_kind.shaderc_intersection_shader },
        { "callable", shaderc_shader_kind.shaderc_callable_shader },
    };

    private static readonly Dictionary<string, shaderc_source_language> SourceLanguageMap = new(StringComparer.Ordinal)
    {
        { "glsl", shaderc_source_language.shaderc_source_language_glsl },
        { "hlsl", shaderc_source_language.shaderc_source_language_hlsl },
    };

    public static string[] TargetEnvValues => EnvVersionMap.Keys.Order(StringComparer.Ordinal).ToArray();

    public static string[] TargetSpvValues => SpirvVersionMap.Keys.Order(StringComparer.Ordinal).ToArray();

    public static string[] ShaderKindValues => ShaderKindMap.Keys.Order(StringComparer.Ordinal).ToArray();

    public static string[] SourceLanguageValues => SourceLanguageMap.Keys.Order(StringComparer.Ordinal).ToArray();

    public static shaderc_env_version ParseTargetEnv(string targetEnv)
    {
        if (EnvVersionMap.TryGetValue(targetEnv, out var envVersion))
        {
            return envVersion;
        }

        throw new ArgumentException($"Invalid target environment: {targetEnv}. Valid values are: [{string.Join(", ", EnvVersionMap.Keys.Order(StringComparer.Ordinal))}]", "target-env");
    }

    public static shaderc_spirv_version ParseTargetSpv(string targetSpv)
    {
        if (SpirvVersionMap.TryGetValue(targetSpv, out var spvVersion))
        {
            return spvVersion;
        }

        throw new ArgumentException($"Invalid target SPIR-V version: {targetSpv}. Valid values are: [{string.Join(", ", SpirvVersionMap.Keys.Order(StringComparer.Ordinal))}]", "target-spv");
    }

    public static shaderc_shader_kind ParseShaderStage(string shaderKind)
    {
        if (ShaderKindMap.TryGetValue(shaderKind, out var kind))
        {
            return kind;
        }

        throw new ArgumentException($"Invalid shader kind: {shaderKind}. Valid values are: [{string.Join(", ", ShaderKindMap.Keys.Order(StringComparer.Ordinal))}]", "fshader-stage");
    }

    public static shaderc_source_language ParseSourceLanguage(string sourceLanguage)
    {
        if (SourceLanguageMap.TryGetValue(sourceLanguage, out var language))
        {
            return language;
        }

        throw new ArgumentException($"Invalid source language: {sourceLanguage}. Valid values are: [{string.Join(", ", SourceLanguageMap.Keys.Order(StringComparer.Ordinal))}]", "source-language");
    }

    public static shaderc_optimization_level? ParseOptimizationLevel(string optimizationLevel)
    {
        return optimizationLevel switch
        {
            "O0" => shaderc_optimization_level.shaderc_optimization_level_zero,
            "Os" => shaderc_optimization_level.shaderc_optimization_level_size,
            "O" => shaderc_optimization_level.shaderc_optimization_level_performance,
            _ => shaderc_optimization_level.shaderc_optimization_level_performance,
        };
    }

    public static ShaderCompilerStageSelection ParseStageSelection(string stageSelection)
    {
        return stageSelection switch
        {
            "default" => ShaderCompilerStageSelection.Default,
            "preprocessor-and-compile" => ShaderCompilerStageSelection.PreprocessorAndCompile,
            "preprocessor-only" => ShaderCompilerStageSelection.PreprocessorOnly,
            "preprocessor-compile-and-disassemble" => ShaderCompilerStageSelection.PreprocessorCompileAndDisassemble,
            _ => throw new ArgumentException($"Invalid stage selection: {stageSelection}. Valid values are: [default, preprocessor-and-compile, preprocessor-only, preprocessor-compile-and-disassemble]", "stage-selection"),
        };
    }

    public static ShaderOutputKind ParseOutputKind(string outputKind)
    {
        return outputKind switch
        {
            "csharp" => ShaderOutputKind.CSharp,
            "tar" => ShaderOutputKind.Tar,
            "tar.gz" => ShaderOutputKind.TarGz,
            "content" => ShaderOutputKind.Content,
            _ => throw new ArgumentException($"Invalid output kind: {outputKind}. Valid values are: [csharp, tar, tar.gz, content]", "output-kind"),
        };
    }
}