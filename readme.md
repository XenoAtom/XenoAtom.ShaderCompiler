# XenoAtom.ShaderCompiler [![Build Status](https://github.com/XenoAtom/XenoAtom.ShaderCompiler/workflows/ci/badge.svg?branch=main)](https://github.com/XenoAtom/XenoAtom.ShaderCompiler/actions)

<img align="right" width="160px" height="160px" src="https://raw.githubusercontent.com/XenoAtom/XenoAtom.ShaderCompiler/main/img/XenoAtom.ShaderCompiler.png">

This project provides:

- A library `XenoAtom.ShaderCompiler` that exposes higher-level integration of [shaderc](https://github.com/google/shaderc) to compile HLSL/GLSL shaders. [![NuGet](https://img.shields.io/nuget/v/XenoAtom.ShaderCompiler.svg)](https://www.nuget.org/packages/XenoAtom.ShaderCompiler/)
- A tool `dotnet-shaderc` the equivalent of [`glslc`](https://github.com/google/shaderc/tree/main/glslc) that can be installed on any machine that has the .NET 8 SDK  [![NuGet](https://img.shields.io/nuget/v/dotnet-shaderc.svg)](https://www.nuget.org/packages/dotnet-shaderc/)
- A MSBuild integration via `XenoAtom.ShaderCompiler.Build` that allows to compile shaders to SPIR-V binary files, embed them directly in C# (via a built-in Source Generator) or generates `tar`/`tar.gz` files.  [![NuGet](https://img.shields.io/nuget/v/XenoAtom.ShaderCompiler.Build.svg)](https://www.nuget.org/packages/XenoAtom.ShaderCompiler.Build/)

## ✨ Features

- Supports most features of [shaderc](https://github.com/google/shaderc).
  - Support for include directories.
- **Multithreaded shader compiler**.
- `dotnet-shaderc` is a .NET Tool equivalent of [`glslc`](https://github.com/google/shaderc/tree/main/glslc) that can be installed on any machine that has the .NET 8 SDK
- The package `XenoAtom.ShaderCompiler.Build` allows to integrate in your C# or any MSBuild projects the compilation of HLSL/GLSL shaders.
  - **C# source generator** supports embedding SPIR-V binary returned as `ReadOnlySpan<byte>` (Default mode for C# projects).
  - Can generate `tar` / `tar.gz` files to collect all compiled shaders.
  - Can copy SPIR-V files as-is to the output folder shipped with your library/app.
  - **Incremental compiler** that detects includes and dependencies to only compile relevant changes.

## 📖 User Guide

For more details on how to use XenoAtom.ShaderCompiler, please visit the [user guide](https://github.com/XenoAtom/XenoAtom.ShaderCompiler/blob/main/doc/readme.md).

## 🪪 License

This software is released under the [BSD-2-Clause license](https://opensource.org/licenses/BSD-2-Clause). 

## 🤗 Author

Alexandre Mutel aka [xoofx](https://xoofx.github.io).
