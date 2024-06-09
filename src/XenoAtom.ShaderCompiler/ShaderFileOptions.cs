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

public class ShaderFileOptions
{
    public ShaderOutputKind? OutputKind { get; set; }

    public ShaderCompilerStageSelection? StageSelection { get; set; }

    public string? EntryPoint { get; set; }

    public libshaderc.shaderc_source_language? SourceLanguage { get; set; }

    public libshaderc.shaderc_optimization_level? OptimizationLevel { get; set; }

    public bool? InvertY { get; set; }

    public libshaderc.shaderc_env_version? TargetEnv { get; set; }

    public libshaderc.shaderc_shader_kind? ShaderStage { get; set; }

    public libshaderc.shaderc_spirv_version? TargetSpv { get; set; }

    public bool? GeneratedDebug { get; set; }

    public bool? Hlsl16BitTypes { get; set; }

    public bool? HlslOffsets { get; set; }

    public bool? HlslFunctionality1 { get; set; }

    public bool? AutoMapLocations { get; set; }

    public bool? AutoBindUniforms { get; set; }

    public bool? HlslIomap { get; set; }

    public List<ShaderMacro> Defines { get; } = new();

    public Guid Hash(MemoryStream stream)
    {
        stream.SetLength(0);

        Append(stream, OutputKind);
        Append(stream, StageSelection);
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

        XxHash128 hash = new();
        stream.Position = 0;
        hash.Append(stream);
        return Unsafe.BitCast<UInt128, Guid>(hash.GetCurrentHashAsUInt128());
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
    private static void Append<T>(MemoryStream stream, T value) where T: unmanaged
    {
        stream.Write(MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateSpan(ref value, 1)));
    }
    
    public static ShaderFileOptions Merge(ShaderFileOptions left, ShaderFileOptions right)
    {
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(left);

        var result = new ShaderFileOptions
        {
            OutputKind = right.OutputKind ?? left.OutputKind,
            StageSelection = right.StageSelection ?? left.StageSelection,
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
}