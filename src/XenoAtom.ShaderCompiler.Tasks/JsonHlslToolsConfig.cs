// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace XenoAtom.ShaderCompiler.Tasks
{
    public class JsonHlslToolsConfig
    {
        [JsonPropertyName("root")]
        public bool Root { get; set; } = false;

        [JsonPropertyName("hlsl.preprocessorDefinitions")]
        public Dictionary<string, string> HlslPreprocessorDefinitions { get; set; } = new();

        [JsonPropertyName("hlsl.additionalIncludeDirectories")]
        public List<string> HlslAdditionalIncludeDirectories { get; set; } = new();

        [JsonPropertyName("hlsl.virtualDirectoryMappings")]
        public Dictionary<string, string> HlslVirtualDirectoryMappings { get; set; } = new();
    }
    
    [JsonSerializable(typeof(JsonHlslToolsConfig))]
    internal partial class JsonSerializerContextForHlslToolsConfig : JsonSerializerContext
    {
    }
}