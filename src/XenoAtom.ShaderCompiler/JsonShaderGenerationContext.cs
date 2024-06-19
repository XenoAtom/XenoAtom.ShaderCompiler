// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text.Json.Serialization;

namespace XenoAtom.ShaderCompiler;

/// <summary>
/// A Json serializer context for serializing <see cref="JsonShaderGlobalOptions"/>.
/// </summary>
[JsonSerializable(typeof(JsonShaderFile))]
[JsonSerializable(typeof(JsonShaderFileOptions))]
[JsonSerializable(typeof(JsonShaderGlobalOptions))]
#if SHADER_COMPILER_RUNTIME
    public
#else
internal
#endif
partial class JsonShaderGenerationContext : JsonSerializerContext
{
}