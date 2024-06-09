// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace XenoAtom.ShaderCompiler
{
#if SHADER_COMPILER_RUNTIME
    public
#else
    internal
#endif
    sealed class JsonShaderGlobalOptions : JsonShaderFileOptions
    {
        [JsonPropertyName("max-thread-count")]
        public string? MaxThreadCount { get; set; }

        [JsonPropertyName("cache-directory")]
        public string? CacheDirectory { get; set; }

        [JsonPropertyName("cache-cs-directory")]
        public string? CacheCSharpDirectory { get; set; }

        [JsonPropertyName("root-namespace")]
        public string? RootNamespace { get; set; }

        [JsonPropertyName("class-name")]
        public string? ClassName { get; set; }

        [JsonPropertyName("include-directories")]
        public List<string> IncludeDirectories { get; set; } = new();

        [JsonPropertyName("input-files")]
        public List<JsonShaderFile> InputFiles { get; set; } = new();

        [JsonPropertyName("generate-deps-file")]
        public bool GenerateDepsFile { get; set; }

        [JsonPropertyName("incremental")]
        public bool Incremental { get; set; }

#if SHADER_COMPILER_RUNTIME
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