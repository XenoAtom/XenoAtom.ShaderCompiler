// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Locator;
using Microsoft.Build.Logging;

namespace XenoAtom.ShaderCompiler.Tests;

[TestClass]
public class ShaderCompilerIntegrationTests
{
    private const string ShaderHlsl1 = """
                                   #pragma shader_stage(vertex)
                                   float4 main(float2 pos : POSITION) : SV_POSITION
                                   {
                                       return float4(pos, 0, 1);
                                   }
                                   """;

    private const string ShaderHlslWithInclude1 = """
                                       #pragma shader_stage(vertex)
                                       #include "include1.hlsl"
                                       float4 main(float2 pos : POSITION) : SV_POSITION
                                       {
                                           return float4(pos, 0, DEFAULT_VALUE);
                                       }
                                       """;
    
    private const string ShaderHlsl2 = """
                                   #pragma shader_stage(vertex)
                                   float4 main(float2 pos : POSITION) : SV_POSITION
                                   {
                                       return float4(pos, 0, 2);
                                   }
                                   """;

    private const string ShaderFrag1 = """
                                       #version 450
                                       
                                       // Input from the vertex shader
                                       layout(location = 0) in vec3 FragPos;

                                       // Output color
                                       layout(location = 0) out vec4 FragColor;

                                       void main()
                                       {
                                           // Simple gradient based on the position
                                           vec3 color = FragPos * 0.5 + 0.5;
                                           
                                           // Set the fragment color
                                           FragColor = vec4(color, 1.0);
                                       }
                                       """;

    [TestMethod]
    public void Test_Project001_SingleShader()
    {
        var project = _build.Load("Project001_SingleShader");

        const string shaderFile = "Test.vert.hlsl";
        const string shaderProperty = "Test_vert_hlsl";

        // Add a shader file
        project.WriteAllText(shaderFile, ShaderHlsl1);

        // Build the project
        project.BuildAndCheck(TaskExecutedWithShaderAndCSharpCompile);

        // Check the shader is compiled
        {
            using var shaderLoaderContext = project.LoadAssembly();
            var compiledType = shaderLoaderContext.LoadCompiledShaders();
            shaderLoaderContext.AssertShader(compiledType, shaderProperty);
        }

        // Build the project again, it should not recompile the shader
        project.BuildAndCheck(TaskExecutedWithNoCompile);

        // Modify the shader
        project.WriteAllText(shaderFile, ShaderHlsl2);

        // Build the project again, it should recompile the shader
        project.BuildAndCheck(TaskExecutedWithShaderAndCSharpCompile);

        GC.Collect();
    }

    [TestMethod]
    public void Test_Project002_MultipleShaders()
    {
        var project = _build.Load("Project002_MultipleShaders");

        const string shaderFile1 = "Test1.vert.hlsl";
        const string shaderProperty1 = "Test1_vert_hlsl";
        const string shaderFile2 = "Test2.frag";
        const string shaderProperty2 = "Test2_frag";

        // Add a shader file
        project.WriteAllText(shaderFile1, ShaderHlsl1);
        project.WriteAllText(shaderFile2, ShaderFrag1);

        // Build the project
        project.BuildAndCheck(TaskExecutedWithShaderAndCSharpCompile);

        // Check the shader is compiled
        {
            using var shaderLoaderContext = project.LoadAssembly();
            var compiledType = shaderLoaderContext.LoadCompiledShaders();
            shaderLoaderContext.AssertShader(compiledType, shaderProperty1);
            shaderLoaderContext.AssertShader(compiledType, shaderProperty2);
        }

        // Build the project again, it should not recompile the shader
        project.BuildAndCheck(TaskExecutedWithNoCompile);

        GC.Collect();
    }

    [TestMethod]
    public void Test_Project003_ShaderWithIncludes()
    {
        var project = _build.Load("Project003_ShaderWithIncludes");

        const string shaderFile = "Test_with_include.vert.hlsl";
        const string shaderProperty = "Test_with_include_vert_hlsl";
        const string shaderIncludeFile = "include1.hlsl";

        // Add a shader file
        project.WriteAllText(shaderFile, ShaderHlslWithInclude1);
        project.WriteAllText(shaderIncludeFile, "#define DEFAULT_VALUE 3.0\n");

        // Build the project
        project.BuildAndCheck(TaskExecutedWithShaderAndCSharpCompile);

        // Check the shader is compiled
        {
            using var shaderLoaderContext = project.LoadAssembly();
            var compiledType = shaderLoaderContext.LoadCompiledShaders();
            shaderLoaderContext.AssertShader(compiledType, shaderProperty);
        }

        // Build the project again, it should not recompile the shader
        project.BuildAndCheck(TaskExecutedWithNoCompile);

        // Touch the include file
        project.WriteAllText(shaderIncludeFile, "#define DEFAULT_VALUE 2.0\n");

        // Build the project again, it should recompile the shader
        project.BuildAndCheck(TaskExecutedWithShaderAndCSharpCompile);

        GC.Collect();
    }

    [TestMethod]
    public void Test_Project005_WithIncludeDirectories()
    {
        var project = _build.Load("Project005_WithIncludeDirectories");
        project.BuildAndCheck(TaskExecutedWithShaderAndCSharpCompile);

        // Check the shader in the Below folder is compiled
        {
            using var shaderLoaderContext = project.LoadAssembly();
            var compiledType = shaderLoaderContext.LoadCompiledShaders();

            var belowType = compiledType.GetNestedTypes().FirstOrDefault(x => x.Name == "Below");
            Assert.IsNotNull(belowType, "The type `Below` was not found in the compiled shaders");
            shaderLoaderContext.AssertShader(belowType, "Test_frag_hlsl");
        }
    }

    [TestMethod]
    public void Test_Project006_WithDefinePerItem()
    {
        var project = _build.Load("Project006_WithDefinePerItem");
        project.BuildAndCheck(TaskExecutedWithShaderAndCSharpCompile);
    }

    [TestMethod]
    public void Test_Project007_WithDefine()
    {
        var project = _build.Load("Project007_WithDefine");
        project.BuildAndCheck(TaskExecutedWithShaderAndCSharpCompile);
    }
    
    [TestMethod]
    public void Test_Project004_InvalidShader()
    {
        var project = _build.Load("Project004_InvalidShader");

        var result = project.Build();
        Assert.AreNotEqual(BuildResultCode.Success, result.OverallResult, "The build should have failed");

        var log = _build.GetLog().Trim();

        _build.TestContext.WriteLine(log);

        // We should have 3 errors
        var countLines = log.Split('\n').Length;
        Assert.AreEqual(3, countLines);

        StringAssert.Contains(log, "Invalid.vert.hlsl(3,1): error: '@' : unexpected token");
        StringAssert.Contains(log, "Invalid.vert.hlsl(3,1): error: 'expression' : Expected");
        StringAssert.Contains(log, "Invalid.vert.hlsl(2,1): error: '' : function does not return a value: @main");
    }

    [TestMethod]
    public void Test_Project008_ContentOutput()
    {
        var project = _build.Load("Project008_ContentOutput");
        project.BuildAndCheck(TaskExecutedWithShaderAndCSharpCompile);
    }

    [TestMethod]
    public void Test_Project009_SimulateIDE()
    {
        var project = _build.Load("Project009_SimulateIDE");
        project.BuildAndCheck(TaskExecutedWithShaderAndCSharpCompile);

        {
            using var shaderLoaderContext = project.LoadAssembly();
            var compiledType = shaderLoaderContext.LoadCompiledShaders();
            shaderLoaderContext.AssertShader(compiledType, "Test_vert_hlsl", assertZeroLength: true);
        }
    }

    [TestMethod]
    public void Test_Project010_CustomNames()
    {
        var project = _build.Load("Project010_CustomNames");
        project.BuildAndCheck(TaskExecutedWithShaderAndCSharpCompile);

        {
            using var shaderLoaderContext = project.LoadAssembly();
            var compiledType = shaderLoaderContext.LoadCompiledShaders("CustomCompiledShaders");
            shaderLoaderContext.AssertShader(compiledType, "Test_vert_hlsl");
            Assert.AreEqual("CustomNamespace", compiledType.Namespace, "Invalid namespace");
        }
    }

    [TestMethod]
    public void Test_Project011_WithDescription()
    {
        var project = _build.Load("Project011_WithDescription");
        project.BuildAndCheck(TaskExecutedWithShaderAndCSharpCompile);

        var csGeneratedFile = project.GetGeneratedCSharpFile("Test.vert.hlsl");
        Assert.IsTrue(File.Exists(csGeneratedFile), $"The file `{csGeneratedFile}` was not found");

        var csText = File.ReadAllText(csGeneratedFile);
        StringAssert.Contains(csText, "/// <summary>");
        StringAssert.Contains(csText, "/// This is a custom description of this shader.");
        StringAssert.Contains(csText, "/// </summary>");
    }

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        MSBuildLocator.RegisterDefaults();
        _build = new BuildHelper(context);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _build.Dispose();
        MSBuildLocator.Unregister();
    }

    private class BuildHelper : IDisposable
    {
        private readonly BuildManager _buildManager;
        private readonly StringBuilder _logStringBuilder = new StringBuilder();
        private readonly BuildParameters _buildParameters;
        private readonly CustomLogger _logger;

        public BuildHelper(TestContext testContext)
        {
            TestContext = testContext;
            _buildManager = new BuildManager();
            var writer = new StringWriter(_logStringBuilder);
            _logger = new CustomLogger(writer);
            _buildParameters = new BuildParameters
            {
                UseSynchronousLogging = true,
                DisableInProcNode = false,
                MaxNodeCount = 1,
                Loggers = new List<ILogger>
                {
                    _logger
                }
            };
        }

        public TestContext TestContext { get; }

        public List<BuildStatusEventArgs> BuildEvents => _logger.BuildEvents;

        public List<string> GetTasksExecuted() => _logger.BuildEvents.OfType<TaskFinishedEventArgs>().Select(x => x.TaskName).ToList();

        public string GetLog() => _logStringBuilder.ToString();

        public ProjectContext Load(string projectName)
        {
            return new ProjectContext(this, projectName);
        }

        public void AssertSuccess(BuildResult result)
        {
            if (result.OverallResult != BuildResultCode.Success)
            {
                TestContext.WriteLine(GetLog());
            }
            Assert.AreEqual(BuildResultCode.Success, result.OverallResult, "Unexpected result from MSBuild");
        }
        
        public BuildResult BuildProject(string projectFile, string target, string configuration = "Debug")
        {
            _logStringBuilder.Clear();
            _logger.BuildEvents.Clear();

            var globalProperties = new Dictionary<string, string>
            {
                { "Configuration", configuration },
            };

            var buildRequestData = new BuildRequestData(projectFile, globalProperties, null, new[] { target }, null);
            var buildResult = _buildManager.Build(_buildParameters, buildRequestData);
            return buildResult;
        }

        public void Dispose()
        {
            _buildManager.Dispose();
        }
    }

    private class ProjectContext
    {
        private readonly BuildHelper _builder;
        private readonly string _projectName;
        private readonly string _projectPath;
        private readonly string _projectFolder;

        public ProjectContext(BuildHelper builder, string projectName)
        {
            _builder = builder;
            _projectName = projectName;
            _projectFolder = Path.Combine(AppContext.BaseDirectory, ProjectsFolder, projectName);
            _projectPath = PrepareProject(projectName);
        }

        public BuildResult Build(string target = "Build", string configuration = "Debug")
        {
            return _builder.BuildProject(_projectPath, target, configuration);
        }

        public void WriteAllText(string relativePath, string content)
        {
            var path = Path.Combine(_projectFolder, relativePath);
            File.WriteAllText(path, content);
        }

        public void TouchFile(string relativePath)
        {
            var path = Path.Combine(_projectFolder, relativePath);
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
        }

        public void BuildAndCheck(List<string> expectedTasks)
        {
            var result = Build();

            _build.AssertSuccess(result);

            var tasksExecuted = _builder.GetTasksExecuted();
            if (expectedTasks.Count != tasksExecuted.Count)
            {
                _builder.TestContext.WriteLine("Expected tasks:");
                foreach (var expectedTask in expectedTasks)
                {
                    _builder.TestContext.WriteLine($"- {expectedTask}");
                }

                _builder.TestContext.WriteLine("");

                _builder.TestContext.WriteLine("Executed tasks:");
                foreach (var taskExecuted in tasksExecuted)
                {
                    _builder.TestContext.WriteLine($"- {taskExecuted}");
                }
            }

            CollectionAssert.AreEqual(expectedTasks, _build.GetTasksExecuted());
        }

        public string ProjectFolder => _projectFolder;

        public string GetGeneratedCSharpFile(string shaderName, string configuration = "Debug")
        {
            return Path.Combine(_projectFolder, "obj", configuration, TargetFrameworkTestProjects, "ShaderCompiler", "cs", $"{shaderName}.cs");
        }

        public string GetShaderCompilerIntermediateOutputFolder(string configuration = "Debug") => Path.Combine(_projectFolder, "obj", configuration, TargetFrameworkTestProjects, "ShaderCompiler");

        private string PrepareProject(string projectName)
        {
            Assert.IsTrue(Directory.Exists(_projectFolder), $"The project `{_projectFolder}` was not found");

            var binDir = Path.Combine(_projectFolder, "bin");
            var objDir = Path.Combine(_projectFolder, "obj");

            if (Directory.Exists(binDir))
            {
                Directory.Delete(binDir, true);
            }

            if (Directory.Exists(objDir))
            {
                Directory.Delete(objDir, true);
            }

            var projectFile = Path.Combine(_projectFolder, $"{projectName}.csproj");

            var result = _builder.BuildProject(projectFile, "Restore");
            if (result.OverallResult != BuildResultCode.Success)
            {
                _builder.TestContext.WriteLine(_builder.GetLog());
            }
            Assert.AreEqual(BuildResultCode.Success, result.OverallResult, "Assert failed to restore the project");

            return projectFile;
        }

        public ProjectAssemblyLoadContext LoadAssembly(string configuration = "Debug")
        {
            var assemblyPath = Path.Combine(_projectFolder, "bin", configuration, TargetFrameworkTestProjects, $"{_projectName}.dll");
            var projectAssemblyLoadContext = new ProjectAssemblyLoadContext(_projectName, assemblyPath);
            return projectAssemblyLoadContext;
        }
    }

    public class ProjectAssemblyLoadContext : IDisposable
    {
        private readonly string _projectName;
        private readonly string _assemblyPath;
        private AssemblyLoadContext? _assemblyLoadContext;

        public ProjectAssemblyLoadContext(string projectName, string assemblyPath)
        {
            _projectName = projectName;
            _assemblyPath = assemblyPath;
            _assemblyLoadContext = new AssemblyLoadContext(projectName, true);
            Assert.IsTrue(File.Exists(assemblyPath), $"The assembly `{assemblyPath}` was not found");
        }

        public void AssertShader(Type compiledShadersType, string shaderName, bool assertZeroLength = false)
        {
            var property = compiledShadersType.GetProperty(shaderName, BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(property, $"The property `{shaderName}` was not found in the compiled shaders");
            var method = property.GetGetMethod();
            var getShaderDelegate = (GetShaderDelegate)method!.CreateDelegate<GetShaderDelegate>();
            var span = getShaderDelegate();
            if (assertZeroLength)
            {
                Assert.AreEqual(0, span.Length, "Unexpected shader length");
            }
            else
            {
                Assert.AreNotEqual(0, span.Length, "Unexpected shader length");
            }
        }

        public Type LoadCompiledShaders(string compiledTypeName = "CompiledShaders")
        {
            using var stream = File.OpenRead(_assemblyPath);
            var assembly = _assemblyLoadContext!.LoadFromStream(stream);

            var compiledType = assembly.GetTypes().FirstOrDefault(x => x.Name == compiledTypeName);

            Assert.IsNotNull(compiledType, $"The type `{compiledTypeName}` was not found in the assembly `{_assemblyPath}`");

            return compiledType;
        }
        public void Dispose()
        {
            var assemblyLoadContext = _assemblyLoadContext!;
            _assemblyLoadContext = null;
            assemblyLoadContext.Unload();
            assemblyLoadContext = null;
        }
    }
    
    private class CustomLogger : ILogger
    {
        private readonly TextWriter _writer;

        public CustomLogger(TextWriter writer)
        {
            _writer = writer;
            Verbosity = LoggerVerbosity.Minimal;

            TasksToLog = new HashSet<string>
            {
                "ShaderCompileTask",
                "ShaderInitializeTask",
                "Csc"
            };

            TargetsToLog = new HashSet<string>
            {
                "ShaderCompileRetrieveDependencies",
                "ShaderCompile",
            };
        }

        public HashSet<string> TasksToLog { get; } = new();

        public HashSet<string> TargetsToLog { get; } = new();

        public List<BuildStatusEventArgs> BuildEvents { get; } = new();

        public void Initialize(IEventSource eventSource)
        {
            eventSource.TaskStarted += (sender, args) =>
            {
                if (TasksToLog.Contains(args.TaskName))
                {
                    lock (BuildEvents)
                    {
                        BuildEvents.Add(args);
                    }
                }
            };

            eventSource.TaskFinished += (sender, args) =>
            {
                if (TasksToLog.Contains(args.TaskName))
                {
                    lock (BuildEvents)
                    {
                        BuildEvents.Add(args);
                    }
                }
            };


            eventSource.TargetStarted += (sender, args) =>
            {
                if (TargetsToLog.Contains(args.TargetName))
                {
                    lock (BuildEvents)
                    {
                        BuildEvents.Add(args);
                    }
                }
            };

            eventSource.TargetFinished += (sender, args) =>
            {
                if (TargetsToLog.Contains(args.TargetName))
                {
                    lock (BuildEvents)
                    {
                        BuildEvents.Add(args);
                    }
                }
            };

            eventSource.WarningRaised += (sender, args) =>
            {
                _writer.WriteLine($"{args.File}({args.LineNumber},{args.ColumnNumber}): warning: {args.Message}");
            };

            eventSource.ErrorRaised += (sender, args) =>
            {
                _writer.WriteLine($"{args.File}({args.LineNumber},{args.ColumnNumber}): error: {args.Message}");
            };
        }

        public void Shutdown()
        {
        }

        public LoggerVerbosity Verbosity { get; set; }

        public string? Parameters { get; set; }
    }

    private static readonly List<string> TaskExecutedWithShaderAndCSharpCompile = new()
    {
        "ShaderInitializeTask",
        "ShaderCompileTask",
        "Csc",
    };
    
    private static readonly List<string> TaskExecutedWithNoCompile = new()
    {
        "ShaderInitializeTask",
    };

    private const string ProjectsFolder = "Projects";
    private const string TargetFrameworkTestProjects = "net8.0";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private static BuildHelper _build;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public TestContext TestContext { get; set; }

    private delegate ReadOnlySpan<byte> GetShaderDelegate();
}