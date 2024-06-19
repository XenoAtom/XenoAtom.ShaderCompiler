// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.ShaderCompiler;

/// <summary>
/// Defines the stage selection for the shader compiler.
/// </summary>
public enum ShaderCompilerMode
{
    /// <summary>
    /// If none of the above options is given, the compiler will run preprocessing, compiling, and linking stages.
    /// </summary>
    Default,

    /// <summary>
    /// Run the preprocessing and compiling stage (-c)
    /// </summary>
    PreprocessorAndCompile,

    /// <summary>
    /// Run only the preprocessing stage (-E)
    /// </summary>
    PreprocessorOnly,

    /// <summary>
    /// Run the preprocessing, compiling, and then disassembling stage (-S)
    /// </summary>
    PreprocessorCompileAndDisassemble,
}