// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace XenoAtom.ShaderCompiler.Bench;

public class BenchCompilerHelper
{
    private readonly string _cacheName;
    private readonly string _cacheDirectory;

    public BenchCompilerHelper(string cacheName)
    {
        _cacheName = cacheName;
        FileCount = 10;
        _cacheDirectory = System.IO.Path.Combine(AppContext.BaseDirectory, "bench_cache", _cacheName);
    }
    
    public int FileCount { get; set; }

    public void ClearCache()
    {
        if (Directory.Exists(_cacheDirectory))
        {
            Directory.Delete(_cacheDirectory, true);
        }
    }

    public void Run()
    {
        using var compilerApp = new ShaderCompilerApp();
        compilerApp.BatchFile = GetJsonBatchFilePath();
        compilerApp.Run(Console.Out);
    }
    
    public void Initialize(int maxThreadCount = 0)
    {
        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }

        var options = new JsonShaderGlobalOptions();
        options.CacheDirectory = _cacheDirectory;
        options.CacheCSharpDirectory = _cacheDirectory;
        options.RootNamespace = "XenoAtom.ShaderCompiler.Bench";
        options.ClassName = "BenchShader";
        options.GenerateDepsFile = true;
        options.Incremental = true;
        options.MaxThreadCount = maxThreadCount.ToString(CultureInfo.InvariantCulture);

        var inputFile = Path.Combine(AppContext.BaseDirectory, "Test.vert.hlsl");
        if (!File.Exists(inputFile))
        {
            throw new FileNotFoundException($"Unable to find file {inputFile}");
        }

        for (int i = 0; i < FileCount; i++)
        {
            var fileOptions = new JsonShaderFile()
            {
                InputFilePath = inputFile,
                OutputSpvPath = Path.Combine(_cacheDirectory, $"Test{i}.spv"),
                OutputCsPath = $"Test{i}.cs",
                OutputDepsPath = Path.Combine(_cacheDirectory, $"Test{i}.deps"),
            };
            options.InputFiles.Add(fileOptions);
        }


        var sourceGenOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = JsonShaderGenerationContext.Default,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(options, sourceGenOptions);
        var jsonFile = GetJsonBatchFilePath();
        File.WriteAllText(jsonFile, json);
    }
    
    private string GetJsonBatchFilePath() => Path.Combine(AppContext.BaseDirectory, $"bench_batch_{_cacheName}.json");
}