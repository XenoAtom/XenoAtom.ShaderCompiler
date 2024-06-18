using System.Text.Json;
using System.Text.Json.Serialization;

namespace XenoAtom.ShaderCompiler.Tests;

[TestClass]
public class JsonTests
{
    [TestMethod]
    public void TestSimple()
    {
        var options = new JsonShaderGlobalOptions();

        options.MaxThreadCount = "4";
        options.CacheDirectory = "cache";
        options.CacheCSharpDirectory = "cache_cs";
        options.RootNamespace = "root";
        options.ClassName = "class";
        options.IncludeDirectories.Add("include1");
        options.GenerateDepsFile = true;
        options.InputFiles.Add(new JsonShaderFile()
            {
                StageSelection = "default",
                EntryPoint = "main",
                SourceLanguage = "hlsl",
                OptimizationLevel = "Os",
                InvertY = true,
                TargetEnv = "vulkan1.0",
                ShaderStage = "vertex",
                TargetSpv = "spv1.0",
                GeneratedDebug = true,
                Hlsl16BitTypes = true,
                HlslOffsets = true,
                HlslFunctionality1 = true,
                AutoMapLocations = true,
                AutoBindUniforms = true,
                HlslIomap = true,
                InputFilePath = "helloworld.hlsl",
                OutputDepsPath = "helloworld.deps",
                OutputSpvPath = "helloworld.spv",
                Defines = "MY_DEFINE=1;MY_DEFINE2=",
            }
        );

        var sourceGenOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = JsonShaderGenerationContext.Default,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // serialize to a json string
        var json = JsonSerializer.Serialize(options, sourceGenOptions);
        var deserialize = JsonSerializer.Deserialize<JsonShaderGlobalOptions>(json, sourceGenOptions);
        var json2 = JsonSerializer.Serialize(deserialize, sourceGenOptions);

        // Compare serialize and deserialize
        Assert.AreEqual(json, json2);
    }
}
