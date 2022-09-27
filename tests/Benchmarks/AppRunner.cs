using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Benchmarks;

class AppRunner
{
    private static readonly string _dotnetFileName = "dotnet" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "");

    public static void Run(string appExePath, params (string, string)[] envVars)
    {
        if (!File.Exists(appExePath))
        {
            throw new ArgumentException($"Could not find application exe '{appExePath}'", nameof(appExePath));
        }

        var isAppHost = !Path.GetExtension(appExePath)!.Equals(".dll", StringComparison.OrdinalIgnoreCase);

        var process = new Process
        {
            StartInfo =
            {
                FileName = isAppHost ? appExePath : _dotnetFileName,
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(appExePath),
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        if (!isAppHost)
        {
            process.StartInfo.ArgumentList.Add(appExePath);
        }

        foreach (var (name, value) in envVars)
        {
            process.StartInfo.Environment.Add(name, value);
        }

        if (!process.Start())
        {
            HandleError(process, "Failed to start application process");
        }

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            HandleError(process, $"Application process failed on exit ({process.ExitCode})");
        }

        static void HandleError(Process process, string message)
        {
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            var sb = new StringBuilder();
            sb.AppendLine(message);
            sb.AppendLine("Standard output:");
            sb.AppendLine(output);
            sb.AppendLine("Standard error:");
            sb.AppendLine(error);

            throw new InvalidOperationException(sb.ToString());
        }
    }
}
