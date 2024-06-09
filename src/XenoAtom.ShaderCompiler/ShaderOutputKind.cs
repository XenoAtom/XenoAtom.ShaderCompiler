// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.ShaderCompiler
{
#if SHADER_COMPILER_RUNTIME
    public
#else
    internal
#endif
    enum ShaderOutputKind
    {
        CSharp = 0,

        Tar = 1,

        TarGz = 2,

        Content = 3
    }
}
