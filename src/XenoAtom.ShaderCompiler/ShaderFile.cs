// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.ShaderCompiler;

public sealed class ShaderFile : ShaderFileOptions
{
    public ShaderFile(string inputFilePath)
    {
        InputFilePath = inputFilePath;
    }

    public string InputFilePath { get; }

    public string? OutputSpvPath { get; set; }

    public string? OutputDepsPath { get; set; }

    public string? OutputCSharpPath { get; set; }

    public string? Description { get; set; }
}