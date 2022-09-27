using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Benchmarks;
class DotNetCli
{
    private static readonly string _fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);

    public static void Clean(IEnumerable<string> args)
    {
        RunCommand("clean", args);
    }

    public static void Publish(IEnumerable<string> args)
    {
        RunCommand("publish", args);
    }

    private static void RunCommand(string commandName, IEnumerable<string> args)
    {
        var process = new Process
        {
            StartInfo =
            {
                FileName = _fileName,
                UseShellExecute = false
            }
        };

        process.StartInfo.ArgumentList.Add(commandName);
        foreach (var arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        var cmdLine = $"{process.StartInfo.FileName} {string.Join(' ', process.StartInfo.ArgumentList)}";
        Console.WriteLine("Running dotnet CLI with cmd line:");
        Console.WriteLine(cmdLine);
        Console.WriteLine();

        if (!process.Start())
        {
            throw new InvalidOperationException($"dotnet {commandName} failed");
        }

        process.WaitForExit();

        if (!process.WaitForExit(_timeout))
        {
            process.Kill();

            throw new InvalidOperationException($"dotnet {commandName} took longer than the allowed time of {_timeout}");
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"dotnet {commandName} failed on exit ({process.ExitCode})");
        }
    }
}
