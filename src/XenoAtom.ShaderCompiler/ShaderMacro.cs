// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text.Json.Serialization;

namespace XenoAtom.ShaderCompiler;

public class ShaderMacro
{
    public ShaderMacro(string key, string? value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; }

    public string? Value { get; }
}