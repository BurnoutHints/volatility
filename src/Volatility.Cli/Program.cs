using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Volatility.Abstractions.Messaging;
using Volatility.CLI;
using Volatility.CLI.Commands;
using Volatility.Hosting;
using Volatility.Messaging;

namespace Volatility;

internal class Frontend
{
    private static IServiceProvider? services;

    /* 
     * TODO LIST:
     *
     * ! = Completed
     * ? = WIP/Needs Testing
     * > = To Do
     * 
     * DONE!
     * --------------------------
     * ! Bit accurate PC header
     * ! User input (CLI)
     * ! ResourceStringTable importer & reference on asset import
     * ! Bit accurate BPR (PC) texture header
     * 
     * TODO LIST!
     * --------------------------
     * ? Proper x64 texture header export size (need x64 file reference)
     * ? PS3/X360 texture header formats
     * ? Texture Header parsing logic
     * ? Text-based universal resource header format
     *
     * LOW PRIORITY
     * --------------------------
     * ? Bit accurate BPR texture headers for other/x64 platforms
     * > Raw DDS texture importing (bundle manager does this)
     * > GUI System
     * 
     */

    static void Main(string[] args)
    {
        ServiceCollection services = new();
        services.AddVolatilityCore();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        Frontend.services = serviceProvider;
        VolatilityMessageHost.Reset(serviceProvider.GetRequiredService<IMessageBus>());
        using IDisposable consoleSubscription = ConfigureMessaging();

        try
        {
            if (args.Length > 0)
            {
                RunCommandTokenized(args);
            }
            else
            {
                CommandLine();
            }
        }
        finally
        {
            Frontend.services = null;
            VolatilityMessageHost.Reset();
        }
    }

    static IDisposable ConfigureMessaging()
    {
        if (services == null)
        {
            throw new InvalidOperationException("Volatility services have not been configured.");
        }

        return services.GetRequiredService<IMessageBus>().Subscribe(new ConsoleMessageSink());
    }

    static void CommandLine()
    {
        var buildTimestamp = "";

        var assembly = Assembly.GetExecutingAssembly();
        foreach (var attribute in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (attribute.Key == "BuildTimestamp")
            {
                buildTimestamp = attribute.Value;
                break;
            }
        }

        VolatilityMessageHost.Sink.Info(
            $"Volatility {assembly.GetName().Version} - Build Date: {buildTimestamp}\n",
            MessageCategory.CLI,
            nameof(Frontend));

        while (true)
        {
            Console.Write("volatility> ");
            string? input = Console.ReadLine();

            RunCommand(input);
        }
    }

    static ICommand ParseCommand(string input)      // Full command
    {
        var parts = new List<string>();
        var quoteContainer = false;
        var currentToken = "";

        foreach (var c in input)
        {
            if (c == '\"')
            {
                quoteContainer = !quoteContainer;
                continue;
            }

            if (char.IsWhiteSpace(c) && !quoteContainer)
            {
                if (!string.IsNullOrEmpty(currentToken))
                {
                    parts.Add(currentToken);
                    currentToken = "";
                }

                continue;
            }

            currentToken += c;
        }

        if (!string.IsNullOrEmpty(currentToken))
        {
            parts.Add(currentToken);
        }

        return ParseCommandTokenized(parts.ToArray());
    }

    static void RunCommand(string? input)
    {
        if (!string.IsNullOrEmpty(input))
        {
            try
            {
                var command = ParseCommand(input);
                command.Execute().GetAwaiter().GetResult();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            catch (Exception ex)
            {
                VolatilityMessageHost.Sink.Error($"Error: {ex.Message}", MessageCategory.CLI, nameof(Frontend));
#if DEBUG
                throw;
#endif
            }
        }
    }

    static void RunCommandTokenized(string[] input)
    {
        if (input.Length == 0)
        {
            return;
        }

        try
        {
            var command = ParseCommandTokenized(input);
            command.Execute().GetAwaiter().GetResult();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        catch (Exception ex)
        {
            VolatilityMessageHost.Sink.Error($"Error: {ex.Message}", MessageCategory.CLI, nameof(Frontend));
            Environment.ExitCode = 1;
#if DEBUG
            throw;
#endif
        }
    }

    static ICommand ParseCommandTokenized(string[] input)    // Split command
    {
        var commandName = input[0].ToLower();
        var args = new Dictionary<string, object>();

        for (int i = 1; i < input.Length; i++)
        {
            if (input[i].StartsWith("--"))
            {
                var equalIndex = input[i].IndexOf('=');
                if (equalIndex != -1)
                {
                    string key = input[i].Substring(2, equalIndex - 2);
                    string value = input[i].Substring(equalIndex + 1);
                    args[key] = bool.TryParse(value, out bool boolValue) ? boolValue : value;
                }
                else
                {
                    // Assume the flag is a boolean that defaults to true
                    args[input[i].Substring(2)] = true;
                }
            }

            if (commandName == "help")
            {
                args["commandName"] = input[i];
            }
        }

        if (Commands.TryGetValue(commandName, out Type? commandType) && commandType != null)
        {
            if (services == null)
            {
                throw new InvalidOperationException("Volatility services have not been configured.");
            }

            ICommand? command = ActivatorUtilities.CreateInstance(services, commandType) as ICommand;
            if (command != null)
            {
                command.SetArgs(args);
                return command;
            }
        }

        throw new InvalidOperationException("Unknown command.");
    }

    public static readonly Dictionary<string, Type> Commands = new Dictionary<string, Type>
    {
        { "exit", typeof(ExitCommand) },
        { "clear", typeof(ClearCommand) },
        { "createresource", typeof(CreateResourceCommand) },
        { "importresource", typeof(ImportResourceCommand) },
        { "exportresource", typeof(ExportResourceCommand) },
        { "autotest", typeof(AutotestCommand) },
        { "help", typeof(HelpCommand) },
        { "porttexture", typeof(PortTextureCommand) },
        { "texturetodds", typeof(TextureToDDSCommand) },
        { "importstringtable", typeof(ImportStringTableCommand) },
    };
}
