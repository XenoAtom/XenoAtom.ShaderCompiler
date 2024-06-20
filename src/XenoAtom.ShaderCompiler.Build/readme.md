# XenoATom.ShaderCompiler.Build

This package allows to automatically compile all your `HLSL`/`GLSL` files in your MSBuild projects to SPIR-V binaries.

If your project is C#, it will be able to embed the generated SPIR-V binaries into your source code directly.

For example, suppose that you have a C# project `HelloWorld` with a `Test.vert.hlsl` file:

```c
#pragma shader_stage(vertex)
float4 main(float2 pos : POSITION) : SV_POSITION
{
    return float4(pos, 0, 1);
}
```

It will generate the following embedded code accessible directly from your C# project:

```c#
using System;

namespace HelloWorld
{
    internal static partial class CompiledShaders
    {
        #if NET5_0_OR_GREATER || NETSTANDARD2_1
        public static ReadOnlySpan<byte> Test_vert_hlsl => new byte[]
        #else
        public static readonly byte[] Test_vert_hlsl = new byte[]
        #endif
        {
            3, 2, 35, 7, 0, 0, 1, 0, 11, 0, 13, 0, 38, 0, 0, 0, 0, 0, 0, 0, 17, 0, 2, 0, 1, 0, 0, 0, 11, 0, 6, 0, 1, 0, 0, 0, 71, 76, 83, 76, 46, 115, 116, 100, 46, 52, 53, 48, 0, 0, 0, 0, 14, 0, 3, 0, 0, 0, 0, 0, 1, 0, 0, 0, 15, 0, 7, 0, 0, 0, 0, 0, 4, 0, 0, 0, 109, 97, 105, 110, 0, 0, 0, 0, 24, 0, 0, 0, 27, 0, 0, 0, 71, 0, 4, 0, 24, 0, 0, 0, 30, 0, 0, 0, 0, 0, 0, 0, 71, 0, 4, 0, 27, 0, 0, 0, 11, 0, 0, 0, 0, 0, 0, 0, 19, 0, 2, 0, 2, 0, 0, 0, 33, 0, 3, 0, 3, 0, 0, 0, 2, 0, 0, 0, 22, 0, 3, 0, 6, 0, 0, 0, 32, 0, 0, 0, 23, 0, 4, 0, 7, 0, 0, 0, 6, 0, 0, 0, 2, 0, 0, 0, 23, 0, 4, 0, 9, 0, 0, 0, 6, 0, 0, 0, 4, 0, 0, 0, 43, 0, 4, 0, 6, 0, 0, 0, 15, 0, 0, 0, 0, 0, 0, 0, 43, 0, 4, 0, 6, 0, 0, 0, 16, 0, 0, 0, 0, 0, 0, 64, 32, 0, 4, 0, 23, 0, 0, 0, 1, 0, 0, 0, 7, 0, 0, 0, 59, 0, 4, 0, 23, 0, 0, 0, 24, 0, 0, 0, 1, 0, 0, 0, 32, 0, 4, 0, 26, 0, 0, 0, 3, 0, 0, 0, 9, 0, 0, 0, 59, 0, 4, 0, 26, 0, 0, 0, 27, 0, 0, 0, 3, 0, 0, 0, 54, 0, 5, 0, 2, 0, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 248, 0, 2, 0, 5, 0, 0, 0, 61, 0, 4, 0, 7, 0, 0, 0, 25, 0, 0, 0, 24, 0, 0, 0, 81, 0, 5, 0, 6, 0, 0, 0, 35, 0, 0, 0, 25, 0, 0, 0, 0, 0, 0, 0, 81, 0, 5, 0, 6, 0, 0, 0, 36, 0, 0, 0, 25, 0, 0, 0, 1, 0, 0, 0, 80, 0, 7, 0, 9, 0, 0, 0, 37, 0, 0, 0, 35, 0, 0, 0, 36, 0, 0, 0, 15, 0, 0, 0, 16, 0, 0, 0, 62, 0, 3, 0, 27, 0, 0, 0, 37, 0, 0, 0, 253, 0, 1, 0, 56, 0, 1, 0
        };
    }
}
```

## 📖 User Guide

For more details on how to use XenoAtom.ShaderCompiler, please visit the [user guide](https://github.com/xoofx/XenoAtom.ShaderCompiler/blob/main/doc/readme.md).

## 🪪 License

This software is released under the [BSD-2-Clause license](https://opensource.org/licenses/BSD-2-Clause). 

## 🤗 Author

Alexandre Mutel aka [xoofx](https://xoofx.github.io).
