// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text.Json.Serialization;

namespace XenoAtom.ShaderCompiler
{
#if SHADER_COMPILER_RUNTIME
    public
#else
    internal
#endif
    sealed class JsonShaderFile : JsonShaderFileOptions
    {
        [JsonPropertyName("input-filepath")]
        public string? InputFilePath { get; set; }

        [JsonPropertyName("output-spv-path")]
        public string? OutputSpvPath { get; set; }

        [JsonPropertyName("output-deps-path")]
        public string? OutputDepsPath { get; set; }

        [JsonPropertyName("output-csharp-path")]
        public string? OutputCsPath { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

#if SHADER_COMPILER_RUNTIME
        public override ShaderFile ToRuntime()
        {
            var shaderFile = (ShaderFile)base.ToRuntime();
            shaderFile.Description = Description;
            shaderFile.OutputSpvPath = OutputSpvPath;
            shaderFile.OutputDepsPath = OutputDepsPath;
            shaderFile.OutputCSharpPath = OutputCsPath;
            return shaderFile;
        }
#endif
    }
}