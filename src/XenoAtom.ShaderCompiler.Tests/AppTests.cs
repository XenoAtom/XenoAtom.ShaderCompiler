// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.CommandLine;

namespace XenoAtom.ShaderCompiler.Tests;

[TestClass]
public class AppTests : VerifyBase
{
    [TestMethod]
    public async Task TestHelp()
    {
        var (result, stdOut, stdErr) = await RunApp(new[] { "--help" });
        Assert.AreEqual(0, result);
        await Verify(stdOut, SharedVerify.CreateVerifySettings());
    }

    [TestMethod]
    public async Task TestMissingInput()
    {
        var (result, stdOut, stdErr) = await RunApp([]);
        Assert.AreNotEqual(0, result);
        Assert.AreEqual(string.Empty, stdOut);
        await Verify(stdErr, SharedVerify.CreateVerifySettings());
    }

    [TestMethod]
    public async Task TestSimple()
    {
        var outputFile = Path.Combine(GetOutputFolderForTest(), "Test.vert.spv");
        var (result, stdOut, stdErr) = await RunApp(
        [
            Path.Combine(GetInputFolderForTest(), "Test.vert.hlsl"),
            "-o", outputFile
        ]);
        Assert.AreEqual(0, result);
        Assert.AreEqual(string.Empty, stdOut);
        Assert.AreEqual(string.Empty, stdErr);

        var fileInfo = new FileInfo(outputFile);
        Assert.IsTrue(fileInfo.Exists, $"The file `{outputFile}` should exist");
        Assert.AreNotEqual(0, fileInfo.Length, $"Expecting a non zero file output `{outputFile}`");
    }

    [TestMethod]
    public async Task TestWithOptions()
    {
        var outputFile = Path.Combine(GetOutputFolderForTest(), "Test.spv");
        var (result, stdOut, stdErr) = await RunApp(
        [
            Path.Combine(GetInputFolderForTest(), "Test.hlsl"),
            "-o", outputFile,
            "--shader-stage", "vertex",
            "--target-env", "vulkan1.2",
            "--target-spv", "spv1.3",
            "-x", "hlsl",
            "--invert-y",
            "--entry-point", "VSMain",
            "--max-thread-count", "1",
            "-O0",
            "--hlsl-16bit-types",
            "--hlsl-offsets",
            "--auto-map-locations",
            "--auto-bind-uniforms",
            "-DDEFAULT_VALUE=1.0",
            "-DOTHER_VALUE",
        ]);
        Assert.AreEqual(string.Empty, stdOut);
        Assert.AreEqual(string.Empty, stdErr);
        Assert.AreEqual(0, result);

        var fileInfo = new FileInfo(outputFile);
        Assert.IsTrue(fileInfo.Exists, $"The file `{outputFile}` should exist");
        Assert.AreNotEqual(0, fileInfo.Length, $"Expecting a non zero file output `{outputFile}`");
    }

    [TestMethod]
    public async Task TestPostProcessorOnly()
    {
        var (result, stdOut, stdErr) = await RunApp(
        [
            Path.Combine(GetInputFolderForTest(), "Test.hlsl"),
            "--entry-point", "VSMain",
            "--shader-stage", "vertex",
            "-DDEFAULT_VALUE=256.0",
            "-E",
        ]);
        Assert.AreEqual(string.Empty, stdErr);
        Assert.AreEqual(0, result);

        await Verify(stdOut, SharedVerify.CreateVerifySettings());
    }

    [TestMethod]
    public async Task TestDisassembly()
    {
        var outputFile = Path.Combine(GetOutputFolderForTest(), "Test.spvasm");
        var (result, stdOut, stdErr) = await RunApp(
        [
            Path.Combine(GetInputFolderForTest(), "Test.hlsl"),
            "--entry-point", "VSMain",
            "--shader-stage", "vertex",
            "-DDEFAULT_VALUE=256.0",
            "-o", outputFile,
            "-S",
        ]);
        Assert.AreEqual(string.Empty, stdErr);
        Assert.AreEqual(0, result);

        var fileInfo = new FileInfo(outputFile);
        Assert.IsTrue(fileInfo.Exists, $"The file `{outputFile}` should exist");
        var fileContent = File.ReadAllText(outputFile);
        await Verify(fileContent, SharedVerify.CreateVerifySettings());
    }

    [TestMethod]
    public async Task TestDisassembly2()
    {
        var (result, stdOut, stdErr) = await RunApp(
        [
            Path.Combine(GetInputFolderForTest(), "Test.hlsl"),
            "--entry-point", "VSMain",
            "--shader-stage", "vertex",
            "-DDEFAULT_VALUE=256.0",
            "-S",
        ]);
        Assert.AreEqual(string.Empty, stdErr);
        Assert.AreEqual(0, result);

        var outputFile = Path.Combine(GetInputFolderForTest(), "Test.spvasm");
        var fileInfo = new FileInfo(outputFile);
        Assert.IsTrue(fileInfo.Exists, $"The file `{outputFile}` should exist");
        var fileContent = File.ReadAllText(outputFile);
        await Verify(fileContent, SharedVerify.CreateVerifySettings());
    }
    
    [TestMethod]
    public async Task TestIncludeDirectories()
    {
        var outputFile = Path.Combine(GetOutputFolderForTest(), "Test.spv");
        var (result, stdOut, stdErr) = await RunApp(
        [
            Path.Combine(GetInputFolderForTest(), "TestWithIncludes.vert.hlsl"),
            "-I", Path.Combine(AppContext.BaseDirectory, "Projects", "Shared"),
            "--shader-stage", "vertex",
            "-o", outputFile
        ]);
        Assert.AreEqual(string.Empty, stdOut);
        Assert.AreEqual(string.Empty, stdErr);
        Assert.AreEqual(0, result);

        var fileInfo = new FileInfo(outputFile);
        Assert.IsTrue(fileInfo.Exists, $"The file `{outputFile}` should exist");
        Assert.AreNotEqual(0, fileInfo.Length, $"Expecting a non zero file output `{outputFile}`");
    }

    private string GetInputFolderForTest()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "AppTests");
        if (!Directory.Exists(folder)) throw new DirectoryNotFoundException(folder);
        return folder;
    }

    private string GetOutputFolderForTest()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "AppTests", $"output_{TestContext.TestName}");
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, true);
        }
        Directory.CreateDirectory(folder);
        return folder;
    }
    
    private async ValueTask<(int Result, string StandardOutput, string StandardError)> RunApp(string[] args)
    {
        var app = ShaderCompilerProgram.CreateCommandApp();

        var writerStdOut = new StringWriter();
        var writerStdErr = new StringWriter();

        var result = await app.RunAsync(args, new CommandRunConfig()
        {
            Out = writerStdOut,
            Error = writerStdErr
        });

        return (result, writerStdOut.ToString(), writerStdErr.ToString());
    }
}