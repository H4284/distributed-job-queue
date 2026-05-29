using System.Diagnostics;

namespace Worker.Services;

public class CodeExecutor
{
    private readonly ILogger<CodeExecutor> _logger;

    public CodeExecutor(ILogger<CodeExecutor> logger)
    {
        _logger = logger;
    }

    public async Task<(bool Success, string Output)> ExecuteAsync(
        string code, string language)
    {
        try
        {
            return language.ToLower() switch
            {
                "python" => await RunPython(code),
                "csharp" => await RunCSharp(code),
                _ => (false, $"Gjuhë e panjohur: {language}")
            };
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

  

    private async Task<(bool, string)> RunPython(string code)
    {
        var tmpFile = Path.Combine(Path.GetTempPath(), $"job_{Guid.NewGuid()}.py");
        await File.WriteAllTextAsync(tmpFile, code);
        var pythonCmd = Environment.GetEnvironmentVariable("PYTHON_CMD") ?? "python";

        try
        {
            return await RunProcess(pythonCmd, tmpFile);
        }
        finally
        {
            if (File.Exists(tmpFile)) File.Delete(tmpFile);
        }
    }



    private async Task<(bool, string)> RunCSharp(string code)
    {
        var fullCode = "using System;\n" +
                       "using System.Linq;\n" +
                       "using System.Collections.Generic;\n\n" +
                       "class Program {\n" +
                       "    static void Main() {\n" +
                       "        " + code + "\n" +
                       "    }\n" +
                       "}";

        var tmpDir = Path.Combine(Path.GetTempPath(), $"job_{Guid.NewGuid()}");
        Directory.CreateDirectory(tmpDir);

        var csFile = Path.Combine(tmpDir, "Program.cs");
        var projFile = Path.Combine(tmpDir, "job.csproj");

        await File.WriteAllTextAsync(csFile, fullCode);
        await File.WriteAllTextAsync(projFile,
            "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
            "  <PropertyGroup>\n" +
            "    <OutputType>Exe</OutputType>\n" +
            "    <TargetFramework>net9.0</TargetFramework>\n" +
            "  </PropertyGroup>\n" +
            "</Project>");

        try
        {
            return await RunProcess("dotnet", $"run --project {tmpDir}", timeoutSeconds: 10);
        }
        finally
        {
            if (Directory.Exists(tmpDir))
                Directory.Delete(tmpDir, recursive: true);
        }
    }



    private async Task<(bool Success, string Output)> RunProcess(
        string fileName, string arguments, int timeoutSeconds = 10)
    {
        using var cts = new CancellationTokenSource(
            TimeSpan.FromSeconds(timeoutSeconds));

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            return (false, "Timeout: ekzekutimi kaloi 10 sekonda");
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        if (process.ExitCode != 0)
            return (false, $"Error: {error}");

        return (true, output);
    }
}