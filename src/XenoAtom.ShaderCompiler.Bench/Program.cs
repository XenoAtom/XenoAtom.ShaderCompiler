using BenchmarkDotNet.Running;

namespace XenoAtom.ShaderCompiler.Bench;

internal class Program
{
    static void Main(string[] args)
    {
        //var benchCompiler = new BenchCompilerHelper("multi-thread");
        //benchCompiler.Initialize();
        //benchCompiler.ClearCache();
        //benchCompiler.Run();
        BenchmarkRunner.Run<BenchCompiler>(null, args);
    }
}