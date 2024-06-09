using System.Text.Json;
using System.Text.Json.Serialization;

namespace XenoAtom.ShaderCompiler.Tests;

[TestClass]
public class Class1Test
{

    [TestMethod]
    public void TestSimple()
    {
        var options = new JsonShaderGlobalOptions();

        //var x = CompiledShaders.Test2_vert_hlsl;
        
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

        var obj = JsonSerializer.Deserialize<JsonShaderGlobalOptions>(json, sourceGenOptions);



        Console.WriteLine(json);
    }
}
