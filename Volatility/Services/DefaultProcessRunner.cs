using System.Diagnostics;
using Volatility.Abstractions.Services;
using Volatility.Utilities;

namespace Volatility.Services;

public sealed class DefaultProcessRunner : IProcessRunner
{
    public string RunAndCapture(string fileName, string arguments, string? workingDirectory = null)
    {
        return ProcessUtilities.RunAndCapture(fileName, arguments, workingDirectory);
    }

    public string RunAndCapture(ProcessStartInfo startInfo)
    {
        return ProcessUtilities.RunAndCapture(startInfo);
    }

    public void RunAndRelayOutput(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        Action<string>? stdoutHandler = null,
        Action<string>? stderrHandler = null)
    {
        ProcessUtilities.RunAndRelayOutput(fileName, arguments, workingDirectory, stdoutHandler, stderrHandler);
    }

    public void RunAndRelayOutput(
        ProcessStartInfo startInfo,
        Action<string>? stdoutHandler = null,
        Action<string>? stderrHandler = null)
    {
        ProcessUtilities.RunAndRelayOutput(startInfo, stdoutHandler, stderrHandler);
    }
}
