// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;

namespace XenoAtom.ShaderCompiler;

/// <summary>
/// Represents global options for the Shader Compiler.
/// </summary>
public class ShaderGlobalOptions : ShaderFileOptions
{
    /// <summary>
    /// Gets or sets the maximum thread count used for compilation.
    /// </summary>
    public int? MaxThreadCount { get; set; }

    /// <summary>
    /// Gets or sets the cache directory (used only in batch file mode).
    /// </summary>
    public string? CacheDirectory { get; set; }

    /// <summary>
    /// Gets or sets the cache C# directory (used only in batch file mode).
    /// </summary>
    public string? CacheCSharpDirectory { get; set; }

    /// <summary>
    /// Gets or sets the root namespace.
    /// </summary>
    public string? RootNamespace { get; set; }

    /// <summary>
    /// Gets or sets the class name.
    /// </summary>
    public string? ClassName { get; set; }

    /// <summary>
    /// Gets the list of include directories.
    /// </summary>
    public List<string> IncludeDirectories { get; } = new();

    /// <summary>
    /// Gets the list of input files.
    /// </summary>
    public List<ShaderFile> InputFiles { get; } = new();

    /// <summary>
    /// Gets or sets whether to generate a deps file.
    /// </summary>
    public bool GenerateDepsFile { get; set; }

    /// <summary>
    /// Gets or sets whether to perform an incremental compilation (used only in batch file mode).
    /// </summary>
    public bool Incremental { get; set; }
}