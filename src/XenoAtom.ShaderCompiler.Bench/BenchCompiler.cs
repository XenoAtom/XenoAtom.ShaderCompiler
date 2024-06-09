// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using BenchmarkDotNet.Attributes;

namespace XenoAtom.ShaderCompiler.Bench;

public class BenchCompiler
{
    private const int FileCount = 1000;
    private readonly BenchCompilerHelper _singleThread;
    private readonly BenchCompilerHelper _multiThread;

    public BenchCompiler()
    {
        _singleThread = new BenchCompilerHelper("single-thread");
        _multiThread = new BenchCompilerHelper("multi-thread");

        _singleThread.FileCount = FileCount;
        _multiThread.FileCount = FileCount;

        _singleThread.Initialize(1);
        _multiThread.Initialize();
    }

    [Benchmark]
    public void BenchSingleThread()
    {
        _singleThread.ClearCache();
        _singleThread.Run();
    }

    [Benchmark]
    public void BenchMultiThread()
    {
        _multiThread.ClearCache();
        _multiThread.Run();
    }
}