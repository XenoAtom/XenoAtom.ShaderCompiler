// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.ShaderCompiler;

/// <summary>
/// Defines a shader macro.
/// </summary>
public class ShaderMacro
{
    /// <summary>
    /// Creates a new instance of <see cref="ShaderMacro"/>.
    /// </summary>
    /// <param name="key">The key of the macro.</param>
    /// <param name="value">The optional value.</param>
    public ShaderMacro(string key, string? value)
    {
        Key = key;
        Value = value;
    }

    /// <summary>
    /// Gets the key of the macro.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the optional value of the macro.
    /// </summary>
    public string? Value { get; }
}