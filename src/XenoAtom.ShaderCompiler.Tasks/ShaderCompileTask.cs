// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace XenoAtom.ShaderCompiler.Tasks
{
    public class ShaderCompileTask : Task
    {
        [Required]
        public string? BatchFile { get; set; }

        [Required]
        public string? ShaderCompilerPath { get; set; }
        
        [Required]
        public string? ShaderCompilerOutputMarkerFile { get; set; }

        public override bool Execute()
        {
            if (!File.Exists(ShaderCompilerPath))
            {
                Log.LogError($"ShaderCompilerPath does not exist: {ShaderCompilerPath}");
                return false;
            }

            var batchFile = BatchFile!;

            if (!File.Exists(batchFile))
            {
                Log.LogError($"BatchFile does not exist: {batchFile}");
                return false;
            }

            var result = RunCompiler(batchFile);
            if (result)
            {
                // Create an empty marker file to indicate that the compilation was successful
                File.WriteAllText(ShaderCompilerOutputMarkerFile!, "");
            }
            else
            {
                // Delete the marker file if the compilation failed
                if (File.Exists(ShaderCompilerOutputMarkerFile))
                {
                    File.Delete(ShaderCompilerOutputMarkerFile!);
                }
            }

            return result;
        }
        
        private bool RunCompiler(string batchFile)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo("dotnet")
                {
                    Arguments = $"exec \"{ShaderCompilerPath}\" --batch \"{batchFile}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.CurrentDirectory // Path of the current project
                };

                var process = new Process()
                {
                    EnableRaisingEvents = true,
                    StartInfo = processStartInfo
                };

                process.OutputDataReceived += (_, e) =>
                {
                    if (string.IsNullOrWhiteSpace(e.Data))
                    {
                        return;
                    }

                    ProcessLog(e.Data);
                };

                process.ErrorDataReceived += (_, e) =>
                {
                    if (string.IsNullOrWhiteSpace(e.Data))
                    {
                        return;
                    }

                    Log.LogError(e.Data);
                };

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.StandardInput.Dispose();

                process.WaitForExit();

                return process.ExitCode == 0;
            }
            catch (System.Exception ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }
        }

        private void ProcessLog(string message)
        {
            var match = MatchWarningOrError.Match(message);
            if (match.Success)
            {
                var path = match.Groups["path"].Value;
                var line = match.Groups["line"].Value;
                var lineNumber = int.Parse(line);
                var kind = match.Groups["kind"].Value;
                var errorMessage = match.Groups["message"].Value;
                if (kind == "error")
                {
                    Log.LogError(null, null, null, path, lineNumber, 1, lineNumber, 1, errorMessage);
                }
                else
                {
                    Log.LogWarning(null, null, null, path, lineNumber, 1, lineNumber, 1, errorMessage);
                }
            }
            else
            {
                Log.LogMessage(MessageImportance.High, message);
            }
        }

        private static readonly Regex MatchWarningOrError = new Regex(@"(?<path>.*?)\((?<line>\d+)\): (?<kind>error|warning): (?<message>.*)");
    }
}
