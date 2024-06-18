// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

// ReSharper disable InconsistentNaming
namespace XenoAtom.ShaderCompiler
{
    internal static class ShaderCompilerConstants
    {
        public const string ShaderCompilerBatchFileName = "ShaderCompile_input.json";

        public const string ShaderCompilerTrackingName = "ShaderCompileFilesProvider";

        // Item Metadata
        public const string ShaderCompile_PathDeps = nameof(ShaderCompile_PathDeps);
        public const string ShaderCompile_SourceFile = nameof(ShaderCompile_SourceFile);
        public const string ShaderCompile_SourceGenerator = nameof(ShaderCompile_SourceGenerator);
        public const string ShaderCompile_PathCSharp = nameof(ShaderCompile_PathCSharp);
        public const string ShaderCompile_RelativePathCSharp = nameof(ShaderCompile_RelativePathCSharp);
        public const string ShaderCompile_IncludeDependencies = nameof(ShaderCompile_IncludeDependencies);

        // Global options
        public const string ShaderCompilerGlobalOption_root_namespace = "root-namespace";
        public const string ShaderCompilerGlobalOption_class_name = "class-name";

        // Global and Per file options
        public const string ShaderCompilerOption_output_kind = "output-kind";
        public const string ShaderCompilerOption_stage_selection = "stage-selection";
        public const string ShaderCompilerOption_entry_point = "entry-point";
        public const string ShaderCompilerOption_source_language = "source-language";
        public const string ShaderCompilerOption_optimization_level = "optimization-level";
        public const string ShaderCompilerOption_invert_y = "invert-y";
        public const string ShaderCompilerOption_target_env = "target-env";
        public const string ShaderCompilerOption_shader_stage = "shader-stage";
        public const string ShaderCompilerOption_target_spv = "target-spv";
        public const string ShaderCompilerOption_generate_debug = "generate-debug";
        public const string ShaderCompilerOption_hlsl_16bit_types = "hlsl-16bit-types";
        public const string ShaderCompilerOption_hlsl_offsets = "hlsl-offsets";
        public const string ShaderCompilerOption_hlsl_functionality1 = "hlsl-functionality1";
        public const string ShaderCompilerOption_auto_map_locations = "auto-map-locations";
        public const string ShaderCompilerOption_auto_bind_uniforms = "auto-bind-uniforms";
        public const string ShaderCompilerOption_hlsl_iomap = "hlsl-iomap";
        public const string ShaderCompilerOption_output_file = "output-file";
        public const string ShaderCompilerOption_defines = "defines";
    }
}