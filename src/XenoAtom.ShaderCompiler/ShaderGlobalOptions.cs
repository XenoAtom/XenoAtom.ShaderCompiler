// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;

namespace XenoAtom.ShaderCompiler;

public class ShaderGlobalOptions : ShaderFileOptions
{
    public int? MaxThreadCount { get; set; }

    public string? CacheDirectory { get; set; }

    public string? CacheCSharpDirectory { get; set; }

    public string? RootNamespace { get; set; }

    public string? ClassName { get; set; }

    public List<string> IncludeDirectories { get; } = new();

    public List<ShaderFile> InputFiles { get; } = new();

    public bool GenerateDepsFile { get; set; }

    public bool Incremental { get; set; }
}