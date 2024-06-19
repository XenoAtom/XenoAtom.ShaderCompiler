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
using System.Text;
using System.Text.RegularExpressions;
using static XenoAtom.Interop.libshaderc;

namespace XenoAtom.ShaderCompiler;

public unsafe partial class ShaderCompilerContext : IDisposable
{
    private readonly ShaderCompilerApp _app;
    private readonly HashSet<string> _includedFiles = new();
    private readonly List<string> _allIncludeDirectories = new();
    private readonly shaderc_compiler_t _compiler;
    private GCHandle _handle;
    private readonly MemoryStream _hashStream = new();
    private readonly XxHash128 _xxHash128 = new();

    public ShaderCompilerContext(ShaderCompilerApp app)
    {
        _app = app;
        _compiler = shaderc_compiler_initialize();
        _handle = GCHandle.Alloc(this);
    }

    public ShaderCompilerApp App => _app;

    public void Reset()
    {
        _allIncludeDirectories.Clear();
        _includedFiles.Clear();
    }

    public void Dispose()
    {
        if (_handle.IsAllocated)
        {
            _handle.Free();
        }
        
        if (_compiler.Value.Handle != nint.Zero)
        {
            shaderc_compiler_release(_compiler);
        }
    }

    public bool Run(ShaderFile shaderFile, TextWriter output, ShaderFileOptions baseOptions)
    {
        // We have always the search path of the current directory
        _allIncludeDirectories.Clear();
        _allIncludeDirectories.AddRange(_app.IncludeDirectories);
        _allIncludeDirectories.Add(_app.CurrentDirectory);

        // Merge the options from the file on top of the base options
        var mergedOptions = ShaderFileOptions.Merge(baseOptions, shaderFile);

        var outputKind = mergedOptions.OutputKind;

        var inputFileName = shaderFile.InputFilePath;
        var inputFilePath = Path.Combine(_app.CurrentDirectory, inputFileName);

        string? csFileToGenerate = null;
        if (outputKind == ShaderOutputKind.CSharp && shaderFile.OutputCSharpPath != null && _app.CacheCSharpDirectory != null)
        {
            csFileToGenerate = Path.Combine(_app.CacheCSharpDirectory!, shaderFile.OutputCSharpPath!);
        }

        string? outputSpvPath = null;
        if (shaderFile.OutputSpvPath != null)
        {
            outputSpvPath = Path.Combine(_app.CacheDirectory!, shaderFile.OutputSpvPath);
        }
        
        bool compileShader = false;

        var hashOfOptions = mergedOptions.Hash(_xxHash128, _hashStream);

        // Incremental? Check if we need to recompile
        if (_app.Incremental &&
            _app.GenerateDepsFile &&
            shaderFile.OutputDepsPath != null &&
            outputSpvPath != null &&
            _app.FileExists(outputSpvPath) &&
            _app.FileExists(inputFilePath) &&
            (csFileToGenerate == null || _app.FileExists(csFileToGenerate)))
        {
            var inputLastWriteTime = _app.GetCachedLastWriteTimeUtc(inputFilePath);
            var outputLastWriteTime = _app.GetCachedLastWriteTimeUtc(outputSpvPath);

            if (csFileToGenerate != null)
            {
                var csLastWriteTime = _app.GetCachedLastWriteTimeUtc(csFileToGenerate);
                if (inputLastWriteTime > csLastWriteTime)
                {
                    compileShader = true;
                    goto checkCompileShader;
                }
            }

            // If the input file is newer than the output file, we need to recompile
            if (inputLastWriteTime > outputLastWriteTime)
            {
                compileShader = true;
            }
            else
            {

                // If any of the include files is newer than the output file, we need to recompile
                var includeDeps = _app.FileReadAllText(shaderFile.OutputDepsPath);
                ReadOnlySpan<string> includeDepsLines = includeDeps.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                // First entry in the deps file is the hash
                if (includeDepsLines.Length == 0 || !Guid.TryParse(includeDepsLines[0], out var guid) || guid != hashOfOptions)
                {
                    compileShader = true;
                    goto checkCompileShader;
                }

                // Following entries are the include files
                foreach (var includeDep in includeDepsLines[1..])
                {
                    if (_app.FileExists(includeDep))
                    {
                        var lastWriteTime = _app.GetCachedLastWriteTimeUtc(includeDep);
                        if (lastWriteTime > outputLastWriteTime)
                        {
                            compileShader = true;
                            break;
                        }
                    }
                    else
                    {
                        // If the include file doesn't exist, we need to recompile
                        // (it might have been deleted)
                        compileShader = true;
                        break;
                    }
                }
            }
        }
        else
        {
            compileShader = true;
        }

        checkCompileShader:

        // Do we need to compile the shader?
        if (!compileShader)
        {
            return true;
        }

        var shaderText = _app.FileReadAllText(Path.Combine(_app.CurrentDirectory, inputFileName));

        var rcOptions = shaderc_compile_options_initialize();
        try
        {
            // Add macro definitions
            foreach (var define in mergedOptions.Defines)
            {
                shaderc_compile_options_add_macro_definition(rcOptions, define.Key, define.Value ?? string.Empty);
            }

            shaderc_compile_options_set_optimization_level(rcOptions, mergedOptions.OptimizationLevel ?? shaderc_optimization_level.shaderc_optimization_level_performance);

            delegate* unmanaged[Stdcall]<void*, byte*, int, byte*, nuint, shaderc_include_result*> includeCallback = &IncludeCallback;
            delegate* unmanaged[Stdcall]<void*, shaderc_include_result*, void> includeResultReleaser = &IncludeResultReleaser;
            shaderc_compile_options_set_include_callbacks(rcOptions, includeCallback, includeResultReleaser, (void*)GCHandle.ToIntPtr(_handle));

            bool hasHlslFileName = inputFileName.EndsWith(".hlsl");
            var sourceLanguage = shaderc_source_language.shaderc_source_language_glsl;
            if (mergedOptions.SourceLanguage.HasValue)
            {
                sourceLanguage = mergedOptions.SourceLanguage.Value;
            }
            else
            {
                sourceLanguage = hasHlslFileName ? shaderc_source_language.shaderc_source_language_hlsl : shaderc_source_language.shaderc_source_language_glsl;
            }
            shaderc_compile_options_set_source_language(rcOptions, sourceLanguage);
            bool isHlsl = sourceLanguage == shaderc_source_language.shaderc_source_language_hlsl;

            if (mergedOptions.InvertY.HasValue)
            {
                shaderc_compile_options_set_invert_y(rcOptions, mergedOptions.InvertY.Value);
            }

            if (mergedOptions.TargetEnv.HasValue)
            {
                uint version = 0;
                shaderc_target_env targetEnv = shaderc_target_env.shaderc_target_env_default;
                switch (mergedOptions.TargetEnv.Value)
                {
                    case shaderc_env_version.shaderc_env_version_vulkan_1_0:
                        version = (uint)mergedOptions.TargetEnv.Value;
                        targetEnv = shaderc_target_env.shaderc_target_env_vulkan;
                        break;
                    case shaderc_env_version.shaderc_env_version_vulkan_1_1:
                        version = (uint)mergedOptions.TargetEnv.Value;
                        targetEnv = shaderc_target_env.shaderc_target_env_vulkan;
                        break;
                    case shaderc_env_version.shaderc_env_version_vulkan_1_2:
                        version = (uint)mergedOptions.TargetEnv.Value;
                        targetEnv = shaderc_target_env.shaderc_target_env_vulkan;
                        break;
                    case shaderc_env_version.shaderc_env_version_opengl_4_5:
                        version = (uint)mergedOptions.TargetEnv.Value;
                        targetEnv = shaderc_target_env.shaderc_target_env_opengl;
                        break;
                }

                shaderc_compile_options_set_target_env(rcOptions, targetEnv, version);
            }

            if (mergedOptions.TargetSpv.HasValue)
            {
                shaderc_compile_options_set_target_spirv(rcOptions, mergedOptions.TargetSpv.Value);
            }

            if (mergedOptions.GeneratedDebug.HasValue)
            {
                if (mergedOptions.GeneratedDebug.Value)
                {
                    shaderc_compile_options_set_generate_debug_info(rcOptions);
                }
            }

            if (isHlsl)
            {
                if (isHlsl && mergedOptions.HlslOffsets.HasValue)
                {
                    shaderc_compile_options_set_hlsl_offsets(rcOptions, mergedOptions.HlslOffsets.Value);
                }

                if (mergedOptions.HlslFunctionality1.HasValue)
                {
                    shaderc_compile_options_set_hlsl_functionality1(rcOptions, mergedOptions.HlslFunctionality1.Value);
                }

                if (mergedOptions.HlslIomap.HasValue)
                {
                    shaderc_compile_options_set_hlsl_io_mapping(rcOptions, mergedOptions.HlslIomap.Value);
                }

                if (mergedOptions.Hlsl16BitTypes.HasValue)
                {
                    shaderc_compile_options_set_hlsl_16bit_types(rcOptions, mergedOptions.Hlsl16BitTypes.Value);
                }
            }

            if (mergedOptions.AutoMapLocations.HasValue)
            {
                shaderc_compile_options_set_auto_map_locations(rcOptions, mergedOptions.AutoMapLocations.Value);
            }

            if (mergedOptions.AutoBindUniforms.HasValue)
            {
                shaderc_compile_options_set_auto_bind_uniforms(rcOptions, mergedOptions.AutoBindUniforms.Value);
            }

            //if (ImageBindingBase.HasValue)
            //{
            //    shaderc_compile_options_set_binding_base(options, ImageBindingBase.Value);
            //}

            bool hasShaderSelectionFromFileName = false;
            shaderc_shader_kind shaderKind;

            if (mergedOptions.ShaderStage.HasValue)
            {
                shaderKind = mergedOptions.ShaderStage.Value;
            }
            else
            {
                shaderKind = shaderc_shader_kind.shaderc_glsl_infer_from_source;
                if (inputFileName.EndsWith(".vert.hlsl") || inputFileName.EndsWith(".vert"))
                {
                    shaderKind = shaderc_shader_kind.shaderc_vertex_shader;
                    hasShaderSelectionFromFileName = true;
                }
                else if (inputFileName.EndsWith(".frag.hlsl") || inputFileName.EndsWith(".frag"))
                {
                    shaderKind = shaderc_shader_kind.shaderc_fragment_shader;
                    hasShaderSelectionFromFileName = true;
                }
                else if (inputFileName.EndsWith(".comp.hlsl") || inputFileName.EndsWith(".comp"))
                {
                    shaderKind = shaderc_shader_kind.shaderc_compute_shader;
                    hasShaderSelectionFromFileName = true;
                }
                else if (inputFileName.EndsWith(".geom.hlsl") || inputFileName.EndsWith(".geom"))
                {
                    shaderKind = shaderc_shader_kind.shaderc_geometry_shader;
                    hasShaderSelectionFromFileName = true;
                }
                else if (inputFileName.EndsWith(".tesc.hlsl") || inputFileName.EndsWith(".tesc"))
                {
                    shaderKind = shaderc_shader_kind.shaderc_tess_control_shader;
                    hasShaderSelectionFromFileName = true;
                }
                else if (inputFileName.EndsWith(".tese.hlsl") || inputFileName.EndsWith(".tese"))
                {
                    shaderKind = shaderc_shader_kind.shaderc_tess_evaluation_shader;
                    hasShaderSelectionFromFileName = true;
                }
            }

            shaderc_compilation_result_t result;

            string? defaultOutputExtension = ".spv";

            // Clear the list of included files
            _includedFiles.Clear();

            var entryPoint = _app.EntryPoint ?? "main";
            switch (mergedOptions.StageSelection ?? ShaderCompilerStageSelection.Default)
            {
                case ShaderCompilerStageSelection.Default:
                case ShaderCompilerStageSelection.PreprocessorAndCompile:
                    result = shaderc_compile_into_spv(_compiler, shaderText, shaderKind, inputFileName, entryPoint, rcOptions);
                    break;
                case ShaderCompilerStageSelection.PreprocessorOnly:
                    defaultOutputExtension = null; // Only console output
                    result = shaderc_compile_into_preprocessed_text(_compiler, shaderText, shaderKind, inputFileName, entryPoint, rcOptions);
                    break;
                case ShaderCompilerStageSelection.PreprocessorCompileAndDisassemble:
                    defaultOutputExtension = ".spvasm";
                    result = shaderc_compile_into_spv_assembly(_compiler, shaderText, shaderKind, inputFileName, entryPoint, rcOptions);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var status = shaderc_result_get_compilation_status(result);
            if (status != shaderc_compilation_status_success)
            {
                string[] lines;
                if (status == shaderc_compilation_status_invalid_stage)
                {
                    var errorMessage = $"{Path.GetFullPath(inputFileName)}(1,1): error: Stage not specified by #pragma or not inferred from file extension";
                    lines = [errorMessage];
                }
                else
                {
                    var errorMessage = shaderc_result_get_error_message(result);
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        lines = ParseLines(errorMessage, status == shaderc_compilation_status.shaderc_compilation_status_compilation_error);
                    }
                    else
                    {
                        lines = [$"Error: {status}"];
                    }
                }

                lock (output)
                {
                    foreach (var line in lines)
                    {
                        output.WriteLine(line);
                    }
                }
                return false;
            }

            var data = shaderc_result_get_bytes(result);
            var size = shaderc_result_get_length(result);
            var span = new Span<byte>(data, (int)size);
            var buffer = span.ToArray();

            try
            {
                if (defaultOutputExtension == null)
                {
                    var text = Encoding.UTF8.GetString(buffer);
                    lock (output)
                    {
                        output.Write(text);
                    }
                }
                else
                {
                    string outputFile;
                    if (_app.OutputFile == null)
                    {
                        if (shaderFile.OutputSpvPath != null)
                        {
                            outputFile = Path.Combine(_app.CacheDirectory!, shaderFile.OutputSpvPath);
                        }
                        else
                        {
                            if (hasShaderSelectionFromFileName)
                            {
                                outputFile = $"{inputFileName}{defaultOutputExtension}";
                            }
                            else
                            {
                                outputFile = Path.ChangeExtension(inputFileName, defaultOutputExtension);
                            }
                        }
                    }
                    else
                    {
                        outputFile = _app.OutputFile;
                    }

                    if (_app.GenerateDepsFile)
                    {
                        var depsFile = shaderFile.OutputDepsPath;
                        if (string.IsNullOrEmpty(depsFile))
                        {
                            depsFile = Path.ChangeExtension(outputFile, ".deps");
                        }
                        else
                        {
                            depsFile = Path.Combine(_app.CacheDirectory!, depsFile);
                        }

                        var depsContentBuilder = new StringBuilder();
                        depsContentBuilder.AppendLine(hashOfOptions.ToString());
                        foreach (var includedFile in _includedFiles.ToList().Order(StringComparer.Ordinal))
                        {
                            depsContentBuilder.AppendLine(includedFile);
                        }

                        var depsContent = depsContentBuilder.ToString();

                        _app.FileWriteAllBytes(depsFile, Encoding.UTF8.GetBytes(depsContent));
                    }

                    if (csFileToGenerate != null)
                    {
                        var csharpContent = ShaderCompilerHelper.GenerateCSharpFile(buffer, shaderFile.OutputCSharpPath!, _app.RootNamespace ?? "", _app.ClassName ?? "CompiledShaders", shaderFile.Description);

                        var csharpDirectory = Path.GetDirectoryName(csFileToGenerate);
                        if (!string.IsNullOrEmpty(csharpDirectory) && !Directory.Exists(csharpDirectory))
                        {
                            Directory.CreateDirectory(csharpDirectory);
                        }

                        _app.FileWriteAllBytes(csFileToGenerate, Encoding.UTF8.GetBytes(csharpContent));
                    }

                    _app.FileWriteAllBytes(outputFile, buffer);
                }
            }
            finally
            {
                shaderc_result_release(result);
            }
        }
        finally
        {
            shaderc_compile_options_release(rcOptions);
        }

        return true;
    }

    [GeneratedRegex(@"(?<path>.*?)(:(?<line>\d+))?: (?<kind>error|warning): (?<message>.*)")]
    private static partial Regex RegexMatchLine();

    private static string[] ParseLines(string text, bool replaceErrorToVisualStudioFormat)
    {
        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        if (replaceErrorToVisualStudioFormat)
        {
            var regex = RegexMatchLine();
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                var match = regex.Match(line);
                if (match.Success)
                {
                    var path = match.Groups["path"].Value;
                    var lineText = match.Groups["line"];
                    int lineNo = 1;
                    if (!string.IsNullOrEmpty(lineText.Value))
                    {
                        lineNo = int.Parse(lineText.Value);
                    }
                    if (string.IsNullOrEmpty(path))
                    {
                        path = "1";
                    }
                    var kind = match.Groups["kind"].Value;
                    var message = match.Groups["message"].Value;

                    lines[i] = $"{path}({lineNo}): {kind}: {message}";
                }
            }
        }

        return lines;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static shaderc_include_result* IncludeCallback(void* user_data, byte* requested_source, int typeI, byte* requesting_source, nuint include_depth)
    {
        var context = (ShaderCompilerContext)GCHandle.FromIntPtr((nint)user_data).Target!;
        var app = context.App;

        var requestedSource = Marshal.PtrToStringUTF8((nint)requested_source)!;
        var requestingSource = Marshal.PtrToStringUTF8((nint)requesting_source)!;

        var type = (shaderc_include_type)typeI;

        var includeResult = (shaderc_include_result*)NativeMemory.AllocZeroed((nuint)sizeof(shaderc_include_result));
        try
        {
            if (type == shaderc_include_type.shaderc_include_type_relative)
            {
                var includeDirectory = Path.GetDirectoryName(requestingSource);
                if (string.IsNullOrEmpty(includeDirectory))
                {
                    includeDirectory = app.CurrentDirectory;
                }
                {
                    var includeFile = Path.GetFullPath(Path.Combine(includeDirectory, requestedSource));
                    if (app.FileExists(includeFile))
                    {
                        var content = app.FileReadAllText(includeFile);
                        includeResult->content = AllocateString(content, out includeResult->content_length);
                        includeResult->source_name = AllocateString(includeFile, out includeResult->source_name_length);
                        context._includedFiles.Add(includeFile);
                        goto include_found;
                    }
                }
            }

            foreach (var includeDirectory in context._allIncludeDirectories)
            {
                // Make sure that we support relative include directories
                var fullIncludeDirectory = Path.Combine(app.CurrentDirectory, includeDirectory);
                var includeFile = Path.GetFullPath(Path.Combine(fullIncludeDirectory, requestedSource));
                if (app.FileExists(includeFile))
                {
                    var content = app.FileReadAllText(includeFile);
                    includeResult->content = AllocateString(content, out includeResult->content_length);
                    includeResult->source_name = AllocateString(includeFile, out includeResult->source_name_length);
                    context._includedFiles.Add(includeFile);
                    break;
                }
            }

            include_found:
            return includeResult;
        }
        catch
        {
            // ignore
        }

        return includeResult;
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void IncludeResultReleaser(void* userData, shaderc_include_result* includeResult)
    {
        if (includeResult == null) return;

        if (includeResult->content != null)
        {
            NativeMemory.Free((void*)includeResult->content);
        }

        if (includeResult->source_name != null)
        {
            NativeMemory.Free((void*)includeResult->source_name);
        }

        NativeMemory.Free((void*)includeResult);
    }

    private static byte* AllocateString(string content, out nuint length)
    {
        length = (nuint)Encoding.UTF8.GetByteCount(content);
        var byteContent = (byte*)NativeMemory.Alloc((nuint)length + 1);
        fixed (char* pContent = content)
        {
            Encoding.UTF8.GetBytes(pContent, content.Length, byteContent, (int)length);
        }
        byteContent[length] = 0;
        return byteContent;
    }
}