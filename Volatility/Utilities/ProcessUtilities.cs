using System.Diagnostics;
using System.Text;
using Volatility.Abstractions.Messaging;
using Volatility.Messaging;

namespace Volatility.Utilities;

internal static class ProcessUtilities
{
    public static string RunAndCapture(string fileName, string arguments, string? workingDirectory = null)
    {
        ProcessStartInfo startInfo = CreateStartInfo(fileName, arguments, workingDirectory);
        return RunAndCapture(startInfo);
    }

    public static string RunAndCapture(ProcessStartInfo startInfo)
    {
        using Process process = new() { StartInfo = startInfo };
        StringBuilder output = new();

        process.Start();
        output.Append(process.StandardOutput.ReadToEnd());
        output.Append(process.StandardError.ReadToEnd());
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Process '{GetProcessDisplayName(startInfo)}' failed with exit code {process.ExitCode}.{Environment.NewLine}{output}");
        }

        return output.ToString();
    }

    public static void RunAndRelayOutput(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        Action<string>? stdoutHandler = null,
        Action<string>? stderrHandler = null)
    {
        ProcessStartInfo startInfo = CreateStartInfo(fileName, arguments, workingDirectory);
        RunAndRelayOutput(startInfo, stdoutHandler, stderrHandler);
    }

    public static void RunAndRelayOutput(
        ProcessStartInfo startInfo,
        Action<string>? stdoutHandler = null,
        Action<string>? stderrHandler = null)
    {
        using Process process = new() { StartInfo = startInfo };
        StringBuilder output = new();

        process.OutputDataReceived += (_, e) =>
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }

            output.AppendLine(e.Data);
            RelayOutput(e.Data, stdoutHandler);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }

            output.AppendLine(e.Data);
            RelayOutput(e.Data, stderrHandler);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Process '{GetProcessDisplayName(startInfo)}' failed with exit code {process.ExitCode}.{Environment.NewLine}{output}");
        }
    }

    private static ProcessStartInfo CreateStartInfo(string fileName, string arguments, string? workingDirectory)
    {
        return new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? Directory.GetCurrentDirectory() : workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
    }

    private static void RelayOutput(string data, Action<string>? handler)
    {
        if (handler != null)
        {
            handler(data);
            return;
        }

        VolatilityMessageHost.Sink.Verbose(data, MessageCategory.Process, nameof(ProcessUtilities));
    }

    private static string GetProcessDisplayName(ProcessStartInfo startInfo)
    {
        if (!string.IsNullOrWhiteSpace(startInfo.Arguments))
        {
            return $"{startInfo.FileName} {startInfo.Arguments}";
        }

        if (startInfo.ArgumentList.Count > 0)
        {
            return $"{startInfo.FileName} {string.Join(' ', startInfo.ArgumentList)}";
        }

        return startInfo.FileName;
    }
}
