using System.Diagnostics;

namespace Volatility.Abstractions.Services;

public interface IProcessRunner
{
    string RunAndCapture(string fileName, string arguments, string? workingDirectory = null);
    string RunAndCapture(ProcessStartInfo startInfo);
    void RunAndRelayOutput(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        Action<string>? stdoutHandler = null,
        Action<string>? stderrHandler = null);
    void RunAndRelayOutput(
        ProcessStartInfo startInfo,
        Action<string>? stdoutHandler = null,
        Action<string>? stderrHandler = null);
}
