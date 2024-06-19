// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.ShaderCompiler;

/// <summary>
/// Represents a shader file with options.
/// </summary>
public sealed class ShaderFile : ShaderFileOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShaderFile"/> class.
    /// </summary>
    /// <param name="inputFilePath">The input HLSL/GLSL file path.</param>
    public ShaderFile(string inputFilePath)
    {
        InputFilePath = inputFilePath;
    }

    /// <summary>
    /// The input HLSL/GLSL file path.
    /// </summary>
    public string InputFilePath { get; }

    /// <summary>
    /// The output SPIR-V path (relative path to cache directory).
    /// </summary>
    public string? OutputSpvPath { get; set; }

    /// <summary>
    /// The output dependencies file path.
    /// </summary>
    public string? OutputDepsPath { get; set; }

    /// <summary>
    /// The output C# path (relative path to cache C# directory).
    /// </summary>
    public string? OutputCSharpPath { get; set; }

    /// <summary>
    /// Gets or sets the description of the shader file used for the C# summary of the generated shader file.
    /// </summary>
    public string? Description { get; set; }
}