using System.Reflection;

using static Volatility.Utilities.TypeUtilities;

namespace Volatility.CLI.Commands;

internal class HelpCommand : ICommand
{
    public static string CommandToken => "help";
    public static string CommandDescription => "Displays all available commands & parameters for individual commands.";
    public static string CommandParameters => "[command]";

    public string? WantedCommand { get; set; }


    public async Task Execute()
    {
        if (!string.IsNullOrEmpty(WantedCommand))
        {
            Frontend.Commands.TryGetValue(WantedCommand.ToLowerInvariant(), out Type? command);
            if (command != null)
            {
                ShowUsage(command);
                return;
            }
        }

        CLIMessageUtilities.Info<HelpCommand>("Available commands:");
        foreach (var command in GetDerivedTypes(typeof(ICommand)))
        {
            string commandName = GetStaticPropertyValue(command, nameof(CommandToken));
            
            if (string.IsNullOrEmpty(commandName))
                continue;
            
            CLIMessageUtilities.Info<HelpCommand>($"    {commandName} - {GetStaticPropertyValue(command, nameof(CommandDescription))}");
            
        }
        CLIMessageUtilities.Info<HelpCommand>("For information on command arguments, run: help <command name>.");
    }

    public void SetArgs(Dictionary<string, object> args)
    {
        WantedCommand = args.TryGetValue("commandName", out object? name) ? name as string : "";
    }

    private static void ShowUsage(Type commandType)
    {
        string token = GetStaticPropertyValue(commandType, nameof(CommandToken));
        string parameters = GetStaticPropertyValue(commandType, nameof(CommandParameters));
        string description = GetStaticPropertyValue(commandType, nameof(CommandDescription));

        CLIMessageUtilities.Info(nameof(HelpCommand), $"Usage:\n   {token} {parameters}\n{description}");
    }

    public HelpCommand() { }
}
