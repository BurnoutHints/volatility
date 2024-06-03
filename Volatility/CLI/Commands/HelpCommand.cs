using System.Reflection;

using static Volatility.Utilities.ClassUtilities;

namespace Volatility.CLI.Commands;

internal class HelpCommand : ICommand
{
    public static string CommandToken => "help";
    public static string CommandDescription => "Displays all available commands & parameters for individual commands.";
    public static string CommandParameters => "[command]";

    public string WantedCommand {  get; set; }


    public async Task Execute()
    {
        if (!string.IsNullOrEmpty(WantedCommand))
        {
            Frontend.Commands.TryGetValue(WantedCommand, out Type command);
            if (command != null)
            {
                ConstructorInfo constructor = command.GetConstructor(Array.Empty<Type>());
                if (constructor != null)
                {
                    object instance = constructor.Invoke(Array.Empty<object>());
                    ICommand commandInstance = (ICommand)instance;
                    commandInstance.ShowUsage();
                }
                return;
            }
        }

        Console.WriteLine("Available commands:");
        foreach (var command in GetDerivedTypes(typeof(ICommand)))
        {
            string commandName = GetStaticPropertyValue(command, "CommandToken");
            if (!string.IsNullOrEmpty(commandName))
            {
                Console.WriteLine($"    {commandName} - {GetStaticPropertyValue(command, "CommandDescription")}");
            }
        }
        Console.WriteLine("For information on command arguments, run: help <command name>.");
    }

    public void SetArgs(Dictionary<string, object> args)
    {
        WantedCommand = args.TryGetValue("commandName", out object? name) ? name as string : "";
    }
}