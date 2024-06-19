// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

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

    public Func<string, Exception>  GetCommandException { get; set; } = message => new InvalidOperationException(message);

    public Func<string, string, Exception> GetOptionException { get; set; } = (message, paramName) => new ArgumentException(message, paramName);

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
                throw GetCommandException($"Error: Unable to parse the batch file `{BatchFile}`");
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
                throw GetCommandException("Error: Expecting a cache directory when using a batch file");
            }

            if (!Directory.Exists(CacheDirectory))
            {
                Directory.CreateDirectory(CacheDirectory);
            }
        }

        if (inputFiles.Count == 0)
        {
            throw GetCommandException("Error: Expecting an input file");
        }

        if (inputFiles.Count > 1 && OutputFile != null)
        {
            throw GetOptionException($"Error: Cannot use an output file with multiple input files. Input files = [{string.Join(", ", inputFiles)}]", "o");
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