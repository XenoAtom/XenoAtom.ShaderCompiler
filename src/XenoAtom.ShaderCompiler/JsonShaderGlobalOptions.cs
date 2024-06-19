// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace XenoAtom.ShaderCompiler
{
    /// <summary>
    /// Represents a shader global options with compiler options used for Json serialization.
    /// </summary>
#if SHADER_COMPILER_RUNTIME
    public
#else
    internal
#endif
    sealed class JsonShaderGlobalOptions : JsonShaderFileOptions
    {
        /// <summary>
        /// Gets or sets the maximum thread count used for compilation.
        /// </summary>
        [JsonPropertyName("max-thread-count")]
        public string? MaxThreadCount { get; set; }

        /// <summary>
        /// Gets or sets the cache directory.
        /// </summary>
        [JsonPropertyName("cache-directory")]
        public string? CacheDirectory { get; set; }

        /// <summary>
        /// Gets or sets the cache C# directory.
        /// </summary>
        [JsonPropertyName("cache-cs-directory")]
        public string? CacheCSharpDirectory { get; set; }

        /// <summary>
        /// Gets or sets the root namespace.
        /// </summary>
        [JsonPropertyName("root-namespace")]
        public string? RootNamespace { get; set; }

        /// <summary>
        /// Gets or sets the class name.
        /// </summary>
        [JsonPropertyName("class-name")]
        public string? ClassName { get; set; }

        /// <summary>
        /// Gets or sets the include directories.
        /// </summary>
        [JsonPropertyName("include-directories")]
        public List<string> IncludeDirectories { get; set; } = new();

        /// <summary>
        /// Gets or sets the input files.
        /// </summary>
        [JsonPropertyName("input-files")]
        public List<JsonShaderFile> InputFiles { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to generate a deps file.
        /// </summary>
        [JsonPropertyName("generate-deps-file")]
        public bool GenerateDepsFile { get; set; }

        /// <summary>
        /// Gets or sets whether the compilation is incremental.
        /// </summary>
        [JsonPropertyName("incremental")]
        public bool Incremental { get; set; }

#if SHADER_COMPILER_RUNTIME
        /// <summary>
        /// Creates a runtime instance of <see cref="ShaderGlobalOptions"/> from this instance.
        /// </summary>
        public override ShaderGlobalOptions ToRuntime()
        {
            var globalOptions = (ShaderGlobalOptions)base.ToRuntime();
            if (MaxThreadCount != null)
            {
                if (int.TryParse(MaxThreadCount, out var maxThreadCountLocal) && maxThreadCountLocal >= 0)
                {
                    globalOptions.MaxThreadCount = maxThreadCountLocal;
                }
            }
            globalOptions.CacheDirectory = CacheDirectory;
            globalOptions.CacheCSharpDirectory = CacheCSharpDirectory;
            globalOptions.RootNamespace = RootNamespace;
            globalOptions.ClassName = ClassName;
            globalOptions.IncludeDirectories.AddRange(IncludeDirectories);
            globalOptions.InputFiles.AddRange(InputFiles.ConvertAll(file => (ShaderFile)file.ToRuntime()));
            globalOptions.GenerateDepsFile = GenerateDepsFile;
            globalOptions.Incremental = Incremental;
            return globalOptions;
        }
#endif
    }
}