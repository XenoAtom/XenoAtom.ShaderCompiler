// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text.Json.Serialization;

namespace XenoAtom.ShaderCompiler
{
    /// <summary>
    /// Represents a shader file with options used for Json serialization.
    /// </summary>
#if SHADER_COMPILER_RUNTIME
    public
#else
    internal
#endif
    sealed class JsonShaderFile : JsonShaderFileOptions
    {
        /// <summary>
        /// Gets or sets the input file path.
        /// </summary>
        [JsonPropertyName("input-filepath")]
        public string? InputFilePath { get; set; }

        /// <summary>
        /// Gets or sets the output spv path (relative path to cache directory).
        /// </summary>
        [JsonPropertyName("output-spv-path")]
        public string? OutputSpvPath { get; set; }

        /// <summary>
        /// Gets or sets the output deps path (relative path to cache directory).
        /// </summary>
        [JsonPropertyName("output-deps-path")]
        public string? OutputDepsPath { get; set; }

        /// <summary>
        /// Gets or sets the output csharp path (relative path to cache C# directory).
        /// </summary>
        [JsonPropertyName("output-csharp-path")]
        public string? OutputCsPath { get; set; }

        /// <summary>
        /// Gets or sets the description of the shader file used for the C# summary of the generated shader file.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

#if SHADER_COMPILER_RUNTIME
        /// <summary>
        /// Creates a runtime instance of <see cref="ShaderFile"/> from this instance.
        /// </summary>
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