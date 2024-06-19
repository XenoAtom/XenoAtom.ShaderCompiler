// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Threading.Tasks;

namespace XenoAtom.ShaderCompiler;

public unsafe partial class ShaderCompilerApp : ShaderGlobalOptions, IDisposable
{
    private readonly Dictionary<string, DateTime> _includeFilesLastWriteTime = new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    private readonly Stack<ShaderCompilerContext> _contexts = new();
    private readonly List<(string RelativeSpvPath, string FullSpvPath, ShaderOutputKind OutputKind, bool Compiled)> _processedFiles = new();
    
    public string CurrentDirectory { get; set; } = Environment.CurrentDirectory;

    public string? BatchFile { get; set; }

    public string? OutputFile { get; set; }

    public Func<string, string> FileReadAllText { get; set; } = File.ReadAllText;

    public Func<string, bool> FileExists { get; set; } = File.Exists;

    public Func<string, Stream> FileCreate { get; set; } = File.Create;

    public Action<string> FileDelete { get; set; } = File.Delete;

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
            ClassName = ClassName ?? "CompiledShaders";
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

            // Process remaining tar files
            ProcessTar();
        }
        finally
        {
            DisposeAllContexts();

            lock (_includeFilesLastWriteTime)
            {
                _includeFilesLastWriteTime.Clear();
            }

            lock (_processedFiles)
            {
                _processedFiles.Clear();
            }
        }

        return runResult;
    }

    private void ProcessTar()
    {
        // Only supported in batch mode
        if (CacheDirectory is null) return;

        // Collect all files to be included in the tar file
        bool hasNewCompiled = false;
        var tarFiles = new List<(string RelativePath, string SourcePath)>();
        ShaderOutputKind? outputKind = null;
        lock (_processedFiles)
        {
            foreach (var processedFile in _processedFiles)
            {
                if (processedFile.OutputKind != ShaderOutputKind.Tar && processedFile.OutputKind != ShaderOutputKind.TarGz)
                {
                    continue;
                }

                if (processedFile.Compiled)
                {
                    hasNewCompiled = true;
                }

                var normalizedRelativeSpvPath = processedFile.RelativeSpvPath.Replace("\\", "/");
                tarFiles.Add((normalizedRelativeSpvPath, processedFile.FullSpvPath));

                if (!outputKind.HasValue)
                {
                    outputKind = processedFile.OutputKind;
                }
                else if (outputKind != processedFile.OutputKind)
                {
                    throw GetCommandException("Error: Cannot mix tar and tar.gz files");
                }
            }
        }

        // Tar file name
        var tarFile = Path.Combine(CacheDirectory!, $"{(string.IsNullOrEmpty(RootNamespace) ? "" : $"{RootNamespace}.")}{ClassName}{(outputKind == ShaderOutputKind.Tar ? ".tar" : ".tar.gz")}");
        if (tarFiles.Count == 0)
        {
            if (FileExists(tarFile))
            {
                FileDelete(tarFile);
            }
            return;
        }


        if (!hasNewCompiled && FileExists(tarFile))
        {
            return;
        }

        using var tarStream = FileCreate(tarFile);
        var outputTarStream = (Stream)tarStream;
        try
        {

            if (outputKind == ShaderOutputKind.TarGz)
            {
                outputTarStream = new GZipStream(tarStream, CompressionLevel.Optimal, true);
            }

            using var tarWriter = new TarWriter(outputTarStream, TarEntryFormat.Pax, true);
            var directories = new HashSet<string>(StringComparer.Ordinal);

            tarFiles.Sort((a, b) => string.Compare(a.RelativePath, b.RelativePath, StringComparison.Ordinal));
            foreach (var (relativePath, sourcePath) in tarFiles)
            {   
                var directory = Path.GetDirectoryName(relativePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    var splitDirectory = directory.Split("/", StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < splitDirectory.Length; i++)
                    {
                        var subDirectory = $"{string.Join("/", splitDirectory.Take(i + 1))}/";
                        if (directories.Contains(subDirectory))
                        {
                            continue;
                        }

                        var dirEntry = new PaxTarEntry(TarEntryType.Directory, subDirectory);
                        tarWriter.WriteEntry(dirEntry);
                        directories.Add(subDirectory);
                    }
                }

                var fileEntry = new PaxTarEntry(TarEntryType.RegularFile, relativePath);
                using var fileStream = File.OpenRead(sourcePath);
                fileEntry.DataStream = fileStream;

                tarWriter.WriteEntry(fileEntry);
            }

        }
        finally
        {
            if (outputTarStream != tarStream)
            {
                outputTarStream.Dispose();
            }
        }
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

    internal void AddProcessedFile(string relativeOutputSpv, string outputSpv, ShaderOutputKind outputKind, bool compiled)
    {
        lock (_processedFiles)
        {
            _processedFiles.Add((relativeOutputSpv, outputSpv, outputKind, compiled));
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