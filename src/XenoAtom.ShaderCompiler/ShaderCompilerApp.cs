// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using XenoAtom.CommandLine;

namespace XenoAtom.ShaderCompiler;

public unsafe partial class ShaderCompilerApp : ShaderGlobalOptions, IDisposable
{
    private readonly Dictionary<string, DateTime> _includeFilesLastWriteTime = new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    private readonly Stack<ShaderCompilerContext> _contexts = new();

    public string CurrentDirectory { get; set; } = Environment.CurrentDirectory;

    public string? BatchFile { get; set; }

    public string? OutputFile { get; set; }

    public Func<string, string> FileReadAllText { get; set; } = File.ReadAllText;

    public Func<string, bool> FileExists { get; set; } = File.Exists;

    public Action<string, byte[]> FileWriteAllBytes { get; set; } = File.WriteAllBytes;

    public Func<string, DateTime> FileGetLastWriteTimeUtc { get; set; } = File.GetLastWriteTimeUtc;
    
    public static CommandApp CreateCommandApp(CommandConfig? config = null)
    {
        const string _ = "";
        var app = new ShaderCompilerApp();
        var inputFileNames = new List<string>();
        var commandApp = new CommandApp("dotnet-shaderc", config: config)
        {
            _,
            "A command-line GLSL/HLSL to SPIR-V compiler with Clang-compatible arguments.",
            _,
            "Overall Options:",
            _,
            new HelpOption(),
            new VersionOption(),
            {"o=", "The SPIR-V {<output_file>}, expecting a single input file.", v => app.OutputFile = v},
            {"batch=", "A batch {<file>} containing a JSON representation of the items to compile.", v => app.BatchFile = v},
            {"generate-deps", "Generates deps file used by build system.", v => app.GenerateDepsFile = v != null },
            {"max-thread-count=", $"The maximum {{<number>}} of threads to use to compile all the inputs. Default is 0 (number of cores available = {Environment.ProcessorCount})", (int threadCount) =>
                {
                    app.MaxThreadCount = threadCount;
                }
            },
            _,
            "Language and Mode Selection Options:",
            _,
            {"invert-y", "Invert the Y axis of the coordinate system.", v => app.InvertY = v != null},
            {"shader-stage=", "lets you specify the shader {<stage>} for one or more inputs from the command line.", v => app.ShaderStage = ArgumentParser.ParseShaderStage(v!)},
            {"target-env=", "The target environment for the shader.", v => app.TargetEnv = ArgumentParser.ParseTargetEnv(v!)},
            {"target-spv=", "Specify the SPIR-V version to be used by the generated module.", v => app.TargetSpv = ArgumentParser.ParseTargetSpv(v!)},
            {"x=", "specify the {<lang>} of the input shader files. Valid languages are glsl and hlsl. ", v => app.SourceLanguage = ArgumentParser.ParseSourceLanguage(v!)},
            _,
            "Compilation Stage Selection Options:",
            {"c", "Run the preprocessing and compiling stage.", v =>
            {
                if (v != null) app.StageSelection = ShaderCompilerStageSelection.PreprocessorAndCompile;
            }},
            {"E", "Run the preprocessing stage.", v =>
            {
                if (v != null) app.StageSelection = ShaderCompilerStageSelection.PreprocessorOnly;
            }},
            {"S", "Run the preprocessing, compiling, and then disassembling stage.", v =>
            {
                if (v != null) app.StageSelection = ShaderCompilerStageSelection.PreprocessorCompileAndDisassemble;
            }},
            _,
            "Preprocessor Options:",
            { "D=", "Add a marco {0:name} and an optional {1:value}", (k, v) => app.Defines.Add(new(k, v)) },
            { "I=", "Adds the specified {<directory>} to the search path for include files.", app.IncludeDirectories },
            _,
            "Code Generation Options:",
            _,
            {"g", "Generate debug information.", v => app.GeneratedDebug = v != null},
            {"O:", "-O0 No optimization. This level generates the most debuggable code. -Os Enables optimizations to reduce code size. -O The default optimization level for better performance.", v =>
                {
                    if (v == "0") app.OptimizationLevel = XenoAtom.Interop.libshaderc.shaderc_optimization_level.shaderc_optimization_level_zero;
                    if (v == "s") app.OptimizationLevel = XenoAtom.Interop.libshaderc.shaderc_optimization_level.shaderc_optimization_level_size;
                    if (v != null) app.OptimizationLevel = XenoAtom.Interop.libshaderc.shaderc_optimization_level.shaderc_optimization_level_performance;
                }
            },
            {"hlsl-16bit-types", "Enables 16bit types for HLSL compilation.", v => app.Hlsl16BitTypes = v != null},
            {"hlsl-offsets", "Use HLSL packing rules instead of GLSL rules when determining offsets of members of blocks. This option is always on when compiling for HLSL.", v => app.HlslOffsets = v != null},
            {"hlsl-functionality1", "Enable extension SPV_GOOGLE_hlsl_functionality1.", v => app.HlslFunctionality1 = v != null},
            {"entry-point=", "The entry point function {<name>}.", v => app.EntryPoint = v!},
            {"auto-map-locations", "Automatically assign locations to all shader inputs and outputs. For HLSL compilation, this option is on by default.", v => app.AutoMapLocations = v != null},
            { "<>", "input_file+", inputFileNames },
            new ResponseFileSource(),
            // Run the command
            (CommandRunContext context, string[] _) =>
            {
                app.InputFiles.AddRange(inputFileNames.Select(x => new ShaderFile(x)
                {
                    OutputSpvPath = app.OutputFile
                }));

                return ValueTask.FromResult(app.Run(context.Out));
            }
        };

        return commandApp;
    }

    public int Run(TextWriter output)
    {
        var inputFiles = InputFiles.ToList();

        var mergedOptionsBase = (ShaderFileOptions)this;
        if (BatchFile != null)
        {
            var batchFileContent = FileReadAllText(BatchFile);
            var batchOptionsJson = JsonSerializer.Deserialize(batchFileContent, JsonShaderGenerationContext.Default.JsonShaderGlobalOptions);
            if (batchOptionsJson == null)
            {
                throw new CommandException($"Error: Unable to parse the batch file `{BatchFile}`");
            }
            var batchOptions = batchOptionsJson.ToRuntime();

            inputFiles.AddRange(batchOptions.InputFiles);

            CacheDirectory = batchOptions.CacheDirectory;
            MaxThreadCount = batchOptions.MaxThreadCount;
            CacheCSharpDirectory = batchOptions.CacheCSharpDirectory;
            IncludeDirectories.AddRange(batchOptions.IncludeDirectories);
            GenerateDepsFile = batchOptions.GenerateDepsFile;
            Incremental = batchOptions.Incremental;
            RootNamespace = batchOptions.RootNamespace;
            ClassName = batchOptions.ClassName;
            OutputKind = batchOptions.OutputKind;
            mergedOptionsBase = ShaderFileOptions.Merge(mergedOptionsBase, batchOptions);

            if (CacheDirectory == null)
            {
                throw new CommandException("Error: Expecting a cache directory when using a batch file");
            }

            if (!Directory.Exists(CacheDirectory))
            {
                Directory.CreateDirectory(CacheDirectory);
            }
        }

        if (inputFiles.Count == 0)
        {
            throw new CommandException("Error: Expecting an input file");
        }

        if (inputFiles.Count > 1 && OutputFile != null)
        {
            throw new OptionException("Error: Cannot use an output file with multiple input files", "o");
        }
        
        int runResult = 0;
        try
        {
            if (MaxThreadCount == 1 || inputFiles.Count == 1)
            {
                var context = GetOrCreateCompilerContext();
                try
                {
                    foreach (var shaderFile in inputFiles)
                    {
                        try
                        {
                            // logic
                            if (!context.Run(shaderFile, output, mergedOptionsBase))
                            {
                                runResult = 1;
                            }
                        }
                        finally
                        {
                            context.Reset();
                        }
                    }
                }
                finally
                {
                    ReleaseCompilerContext(context);
                }
            }
            else
            {
                Parallel.ForEach(inputFiles, new ParallelOptions
                    {
                        MaxDegreeOfParallelism = MaxThreadCount is not > 0 ? Environment.ProcessorCount : MaxThreadCount.Value
                    },
                    shaderFile =>
                    {
                        var context = GetOrCreateCompilerContext();
                        try
                        {
                            // logic
                            if (!context.Run(shaderFile, output, mergedOptionsBase))
                            {
                                runResult = 1;
                            }
                        }
                        finally
                        {
                            ReleaseCompilerContext(context);
                        }
                    });
            }
        }
        finally
        {
            DisposeAllContexts();

            lock (_includeFilesLastWriteTime)
            {
                _includeFilesLastWriteTime.Clear();
            }
        }

        return runResult;
    }

    internal ShaderCompilerContext GetOrCreateCompilerContext()
    {
        lock(_contexts)
        {
            if (_contexts.Count == 0)
            {
                _contexts.Push(new ShaderCompilerContext(this));
            }

            return _contexts.Pop();
        }
    }

    internal void ReleaseCompilerContext(ShaderCompilerContext context)
    {
        context.Reset();
        lock(_contexts)
        {
            _contexts.Push(context);
        }
    }

    private void DisposeAllContexts()
    {
        lock(_contexts)
        {
            while (_contexts.Count > 0)
            {
                _contexts.Pop().Dispose();
            }
        }
    }
    
    internal DateTime GetCachedLastWriteTimeUtc(string path)
    {
        lock (_includeFilesLastWriteTime)
        {
            if (_includeFilesLastWriteTime.TryGetValue(path, out var lastWriteTime))
            {
                return lastWriteTime;
            }

            lastWriteTime = FileGetLastWriteTimeUtc(path);
            _includeFilesLastWriteTime[path] = lastWriteTime;
            return lastWriteTime;
        }
    }

    public void Dispose()
    {
        DisposeAllContexts();
    }
}