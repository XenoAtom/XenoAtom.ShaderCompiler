// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using XenoAtom.Interop;

namespace XenoAtom.ShaderCompiler;

/// <summary>
/// Represents a shader file with options.
/// </summary>
public class ShaderFileOptions
{
    /// <summary>
    /// Gets or sets the output kind (C#, tar, tar.gz...)
    /// </summary>
    public ShaderOutputKind? OutputKind { get; set; }

    /// <summary>
    /// Gets or sets the mode of the compiler (preprocessor only, compile & disassemble...)
    /// </summary>
    public ShaderCompilerMode? CompilerMode { get; set; }

    /// <summary>
    /// Gets or sets the entry point (e.g main).
    /// </summary>
    public string? EntryPoint { get; set; }

    /// <summary>
    /// Gets or sets the source language (hlsl or glsl).
    /// </summary>
    public libshaderc.shaderc_source_language? SourceLanguage { get; set; }

    /// <summary>
    /// Gets or sets the optimization level (O, O0, Os).
    /// </summary>
    public libshaderc.shaderc_optimization_level? OptimizationLevel { get; set; }

    /// <summary>
    /// Gets or sets whether to invert Y (HLSL only)
    /// </summary>
    public bool? InvertY { get; set; }

    /// <summary>
    /// Gets or sets the target environment (vulkan, opengl...)
    /// </summary>
    public libshaderc.shaderc_env_version? TargetEnv { get; set; }

    /// <summary>
    /// Gets or sets the shader stage (vertex, fragment, compute...)
    /// </summary>
    public libshaderc.shaderc_shader_kind? ShaderStage { get; set; }

    /// <summary>
    /// Gets or sets the target SPIR-V version.
    /// </summary>
    public libshaderc.shaderc_spirv_version? TargetSpv { get; set; }

    /// <summary>
    /// Gets or sets whether to generate debug information.
    /// </summary>
    public bool? GeneratedDebug { get; set; }

    /// <summary>
    /// Gets or sets whether to enable 16bit types for HLSL compilation.
    /// </summary>
    public bool? Hlsl16BitTypes { get; set; }

    /// <summary>
    /// Gets or sets whether to use HLSL packing rules instead of GLSL rules when determining offsets of members of blocks.
    /// </summary>
    public bool? HlslOffsets { get; set; }

    /// <summary>
    /// Gets or sets whether to enable extension SPV_GOOGLE_hlsl_functionality1.
    /// </summary>
    public bool? HlslFunctionality1 { get; set; }

    /// <summary>
    /// Gets or sets whether to automatically assign locations to all shader inputs and outputs.
    /// </summary>
    public bool? AutoMapLocations { get; set; }

    /// <summary>
    /// Gets or sets whether to automatically assign binding numbers to uniform variables, when an explicit binding is not specified in the shader source.
    /// </summary>
    public bool? AutoBindUniforms { get; set; }

    /// <summary>
    /// Gets or sets whether to use HLSL IO mapping.
    /// </summary>
    public bool? HlslIomap { get; set; }

    /// <summary>
    /// Gets the list of defines.
    /// </summary>
    public List<ShaderMacro> Defines { get; } = new();

    /// <summary>
    /// Calculate a hash for these options.
    /// </summary>
    /// <param name="hash">The XxHash128 instance to reuse.</param>
    /// <param name="stream">A memory stream used to store temporarily the data to hash.</param>
    /// <returns>The hash of the data.</returns>
    public Guid Hash(XxHash128 hash, MemoryStream stream)
    {
        stream.SetLength(0);

        Append(stream, OutputKind);
        Append(stream, CompilerMode);
        Append(stream, EntryPoint);
        Append(stream, SourceLanguage);
        Append(stream, OptimizationLevel);
        Append(stream, InvertY);
        Append(stream, TargetEnv);
        Append(stream, ShaderStage);
        Append(stream, TargetSpv);
        Append(stream, GeneratedDebug);
        Append(stream, Hlsl16BitTypes);
        Append(stream, HlslOffsets);
        Append(stream, HlslFunctionality1);
        Append(stream, AutoMapLocations);
        Append(stream, AutoBindUniforms);
        Append(stream, HlslIomap);
        Append(stream, Defines.Count);
        var defines = Defines.OrderBy(x => x.Key).ToArray();
        foreach (var define in defines)
        {
            Append(stream, define.Key);
            Append(stream, define.Value);
        }

        stream.Position = 0;
        hash.Reset();
        hash.Append(stream);
        return Unsafe.BitCast<UInt128, Guid>(hash.GetCurrentHashAsUInt128());
    }

    /// <summary>
    /// Merge right options over left options.
    /// </summary>
    /// <param name="left">Left options</param>
    /// <param name="right">Right options</param>
    /// <returns>The merged options.</returns>
    public static ShaderFileOptions Merge(ShaderFileOptions left, ShaderFileOptions right)
    {
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(left);

        var result = new ShaderFileOptions
        {
            OutputKind = right.OutputKind ?? left.OutputKind,
            CompilerMode = right.CompilerMode ?? left.CompilerMode,
            EntryPoint = right.EntryPoint ?? left.EntryPoint,
            SourceLanguage = right.SourceLanguage ?? left.SourceLanguage,
            OptimizationLevel = right.OptimizationLevel ?? left.OptimizationLevel,
            InvertY = right.InvertY ?? left.InvertY,
            TargetEnv = right.TargetEnv ?? left.TargetEnv,
            ShaderStage = right.ShaderStage ?? left.ShaderStage,
            TargetSpv = right.TargetSpv ?? left.TargetSpv,
            GeneratedDebug = right.GeneratedDebug ?? left.GeneratedDebug,
            Hlsl16BitTypes = right.Hlsl16BitTypes ?? left.Hlsl16BitTypes,
            HlslOffsets = right.HlslOffsets ?? left.HlslOffsets,
            HlslFunctionality1 = right.HlslFunctionality1 ?? left.HlslFunctionality1,
            AutoMapLocations = right.AutoMapLocations ?? left.AutoMapLocations,
            AutoBindUniforms = right.AutoBindUniforms ?? left.AutoBindUniforms,
            HlslIomap = right.HlslIomap ?? left.HlslIomap,
        };

        foreach (var define in left.Defines)
        {
            result.Defines.Add(define);
        }

        foreach (var define in right.Defines)
        {
            // Remove any existing define with the same key
            var removeList = result.Defines.Where(x => x.Key == define.Key).ToList();
            foreach (var remove in removeList)
            {
                result.Defines.Remove(remove);
            }

            result.Defines.Add(define);
        }
        
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Append(MemoryStream stream, string? value)
    {
        if (value is not null)
        {
            Append(stream, 1);
            Append(stream, value.Length);
            stream.Write(MemoryMarshal.AsBytes(value.AsSpan()));
        }
        else
        {
            Append(stream, 0);
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Append<T>(MemoryStream stream, T? value) where T : unmanaged
    {
        if (value.HasValue)
        {
            Append(stream, 1);
            Append(stream, value.Value);
        }
        else
        {
            Append(stream, 0);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Append<T>(MemoryStream stream, T value) where T : unmanaged
    {
        stream.Write(MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateSpan(ref value, 1)));
    }
}