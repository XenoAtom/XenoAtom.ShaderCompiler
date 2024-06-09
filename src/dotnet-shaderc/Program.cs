// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using XenoAtom.Interop;

namespace XenoAtom.ShaderCompiler;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        // Support for dependencies
        // https://stackoverflow.com/questions/76828163/add-dynamic-dependencies-read-from-file-to-msbuild-target-inputs
        var app = ShaderCompilerApp.CreateCommandApp();
        return await app.RunAsync(args);
    }
}