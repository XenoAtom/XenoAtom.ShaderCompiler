// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.CommandLine;

namespace XenoAtom.ShaderCompiler;

/// <summary>
/// Command-line program for the ShaderCompilerApp.
/// </summary>
public class ShaderCompilerProgram
{
    /// <summary>
    /// Entry point for the ShaderCompilerProgram.
    /// </summary>
    public static async Task<int> Main(string[] args)
    {
        var app = CreateCommandApp();
        return await app.RunAsync(args);
    }

    /// <summary>
    /// Creates a new instance of <see cref="CommandApp"/> for the ShaderCompilerApp.
    /// </summary>
    /// <param name="config">The configuration for the command line app.</param>
    public static CommandApp CreateCommandApp(CommandConfig? config = null)
    {
        const string _ = "";
        var app = new ShaderCompilerApp()
        {
            GetCommandException = message => new CommandException(message),
            GetOptionException = (message, paramName) => new OptionException(message, paramName)
        };
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
            {"max-thread-count=", $"The maximum {{<number>}} of threads to use to compile all the inputs. Default is 0 (Maximum number of cores available)", (int threadCount) =>
                {
                    app.MaxThreadCount = threadCount;
                }
            },
            _,
            "Language and Mode Selection Options:",
            _,
            {"invert-y", "Invert the Y axis of the coordinate system.", v => app.InvertY = v != null},
            {"shader-stage=", $"lets you specify the shader {{<stage>}} for one or more inputs from the command line. Possible values are: {string.Join(", ", ArgumentParser.ShaderKindValues)}.", v => app.ShaderStage = ArgumentParser.ParseShaderStage(v!)},
            {"target-env=", $"The target environment {{<value>}} for the shader. Possible values are: {string.Join(", ", ArgumentParser.TargetEnvValues)}.", v => app.TargetEnv = ArgumentParser.ParseTargetEnv(v!)},
            {"target-spv=", $"Specify the SPIR-V {{<version>}} to be used by the generated module. Possible values are: {string.Join(", ", ArgumentParser.TargetSpvValues)}.", v => app.TargetSpv = ArgumentParser.ParseTargetSpv(v!)},
            {"x=", $"specify the {{<lang>}} of the input shader files. Possible values are: {string.Join(", ", ArgumentParser.SourceLanguageValues)}.", v => app.SourceLanguage = ArgumentParser.ParseSourceLanguage(v!)},
            _,
            "Compilation Stage Selection Options:",
            {"c", "Run the preprocessing and compiling stage.", v =>
            {
                if (v != null) app.CompilerMode = ShaderCompilerMode.PreprocessorAndCompile;
            }},
            {"E", "Run the preprocessing stage.", v =>
            {
                if (v != null) app.CompilerMode = ShaderCompilerMode.PreprocessorOnly;
            }},
            {"S", "Run the preprocessing, compiling, and then disassembling stage.", v =>
            {
                if (v != null) app.CompilerMode = ShaderCompilerMode.PreprocessorCompileAndDisassemble;
            }},
            _,
            "Preprocessor Options:",
            { "D:|define:", "Add a macro {0:name} and an optional {1:value}", (k, v) =>
                {
                    if (k is null) throw new OptionException("Expecting a macro name", "D");
                    app.Defines.Add(new(k, v));
                }
            },
            { "I=|include-dir=", "Adds the specified {<directory>} to the search path for include files.", app.IncludeDirectories },
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
            {"auto-bind-uniforms", "Directs the compiler to automatically assign binding numbers to uniform variables, when an explicit binding is not specified in the shader source.", v => app.AutoBindUniforms = v != null},
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
}