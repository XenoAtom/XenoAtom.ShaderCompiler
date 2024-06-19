// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.ShaderCompiler
{
    /// <summary>
    /// Defines the kind of output for a shader by the compiler (C#, Tar, TarGz, Content).
    /// </summary>
#if SHADER_COMPILER_RUNTIME
    public
#else
    internal
#endif
    enum ShaderOutputKind
    {
        /// <summary>
        /// Generates a C# file from the SPIR-V binary.
        /// </summary>
        CSharp = 0,

        /// <summary>
        /// Copy the output SPIR-V binary to an entry inside a tar file.
        /// </summary>
        Tar = 1,

        /// <summary>
        /// Copy the output SPIR-V binary to an entry inside a tar.gz file.
        /// </summary>
        TarGz = 2,

        /// <summary>
        /// Copy the output SPIR-V binary to a plain MSBuild content file.
        /// </summary>
        Content = 3
    }
}
