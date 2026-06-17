using System.Reflection;

using static Volatility.Utilities.TypeUtilities;

namespace Volatility.CLI.Commands;

internal class HelpCommand : ICommand
{
    public static string CommandToken => "help";
    public static string CommandDescription => "Displays all available commands & parameters for individual commands.";
    public static string CommandParameters => "[command]";

    public string? WantedCommand { get; set; }


    public Task Execute()
    {
        if (!string.IsNullOrEmpty(WantedCommand))
        {
            Frontend.Commands.TryGetValue(WantedCommand.ToLowerInvariant(), out Type? command);
            if (command != null)
            {
                ShowUsage(command);
                return Task.CompletedTask;
            }
        }

        CLIMessageUtilities.Info<HelpCommand>("Available commands:");
        foreach (var command in Frontend.Commands.Values.Distinct())
        {
            string? commandName = GetStaticPropertyValue(command, "CommandToken");
            
            if (string.IsNullOrEmpty(commandName))
                continue;
            
            CLIMessageUtilities.Info<HelpCommand>($"    {commandName} - {GetStaticPropertyValue(command, "CommandDescription")}");
        }
        CLIMessageUtilities.Info<HelpCommand>("For information on command arguments, run: help <command name>.");
        return Task.CompletedTask;
    }

    public void SetArgs(Dictionary<string, object> args)
    {
        WantedCommand = args.TryGetValue("commandName", out object? name) ? name as string : "";
    }

    private static void ShowUsage(Type commandType)
    {
        string? token = GetStaticPropertyValue(commandType, nameof(CommandToken));
        string? parameters = GetStaticPropertyValue(commandType, nameof(CommandParameters));
        string? description = GetStaticPropertyValue(commandType, nameof(CommandDescription));

        CLIMessageUtilities.Info(nameof(HelpCommand), $"Usage:\n   {token ?? ""} {parameters ?? ""}\n{description ?? ""}");
    }

    public HelpCommand() { }
}
