using System.IO.Hashing;
using System.Text.Json;
using System.Text.Json.Serialization;
using XenoAtom.Interop;

namespace XenoAtom.ShaderCompiler.Tests;

[TestClass]
public class DataModelTests : VerifyBase
{
    [TestMethod]
    public async Task TestSimple()
    {
        var options = CreateTestGlobalOptions();

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

        var settings = SharedVerify.CreateVerifySettings();
        await Verify(json, settings);
    }

    [TestMethod]
    public void TestHash()
    {
        var options = CreateTestGlobalOptions();
        var shaderFile = options.InputFiles[0].ToRuntime();

        var xxHash128 = new XxHash128();
        var stream = new MemoryStream();

        var hash = shaderFile.Hash(xxHash128, stream);
        var hash2 = shaderFile.Hash(xxHash128, stream);
        Assert.AreEqual(hash, hash2);

        shaderFile.Defines.Add(new("MY_DEFINE3", "2"));
        var hash3 = shaderFile.Hash(xxHash128, stream);
        Assert.AreNotEqual(hash, hash3);
    }

    [TestMethod]
    public void TestMerge()
    {
        var options = CreateTestGlobalOptions();
        var shaderFile1 = new ShaderFile("input.hlsl")
        {
            EntryPoint = "hello_from_left",
            InvertY = false,
        };
        shaderFile1.Defines.Add(new("MY_DEFINE2", "2"));
        shaderFile1.Defines.Add(new("MY_DEFINE3", "3"));
        
        var shaderFile2 = options.InputFiles[0].ToRuntime();

        var mergedOptions = ShaderFileOptions.Merge(shaderFile1, shaderFile2);

        Assert.AreEqual("main", mergedOptions.EntryPoint);
        Assert.AreEqual(true, mergedOptions.InvertY);
        Assert.AreEqual(libshaderc.shaderc_env_version.shaderc_env_version_vulkan_1_0, mergedOptions.TargetEnv);
        Assert.AreEqual(libshaderc.shaderc_shader_kind.shaderc_vertex_shader, mergedOptions.ShaderStage);
        Assert.AreEqual(libshaderc.shaderc_spirv_version.shaderc_spirv_version_1_0, mergedOptions.TargetSpv);
        Assert.AreEqual(true, mergedOptions.GeneratedDebug);
        Assert.AreEqual(true, mergedOptions.Hlsl16BitTypes);
        Assert.AreEqual(true, mergedOptions.HlslOffsets);
        Assert.AreEqual(true, mergedOptions.HlslFunctionality1);
        Assert.AreEqual(true, mergedOptions.AutoMapLocations);
        Assert.AreEqual(true, mergedOptions.AutoBindUniforms);
        Assert.AreEqual(true, mergedOptions.HlslIomap);
        Assert.AreEqual(3, mergedOptions.Defines.Count);
        var keyValue = mergedOptions.Defines.FirstOrDefault(x => x.Key == "MY_DEFINE2");
        Assert.IsNotNull(keyValue);
        Assert.AreEqual(null, keyValue.Value); // Overridden by the right shader file

        mergedOptions = ShaderFileOptions.Merge(shaderFile2, shaderFile1);
        Assert.AreEqual("hello_from_left", mergedOptions.EntryPoint);
        Assert.AreEqual(false, mergedOptions.InvertY);
    }

    private static JsonShaderGlobalOptions CreateTestGlobalOptions()
    {
        var options = new JsonShaderGlobalOptions
        {
            MaxThreadCount = "4",
            CacheDirectory = "cache",
            CacheCSharpDirectory = "cache_cs",
            RootNamespace = "root",
            ClassName = "class",
            GenerateDepsFile = true
        };

        options.IncludeDirectories.Add("include1");
        options.InputFiles.Add(new JsonShaderFile()
            {
                StageSelection = "default",
                EntryPoint = "main",
                Description = "This is a description",
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
        return options;
    }
}
