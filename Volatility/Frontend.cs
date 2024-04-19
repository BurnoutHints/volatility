namespace Volatility;

internal class Frontend
{
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
     * 
     * TODO LIST!
     * --------------------------
     * ? Bit accurate BPR (PC) header (needs testing)
     * ? Proper x64 header export size (need x64 file reference)
     * ? PS3/X360 header formats
     * ? Header parsing logic
     * > Text-based universal header format
     *
     * LOW PRIORITY
     * --------------------------
     * ? Bit accurate BPR headers for other/x64 platforms
     * > Raw DDS texture importing (bundle manager does this)
     * > GUI System
     * 
     */

    static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            string fullCommand = string.Join(" ", args);
            ParseCommand(fullCommand);
        }
        else 
        {
            CommandLine();
        }
    }

    static void CommandLine()
    {
        while (true)
        {
            Console.Write("volatility> ");
            var input = Console.ReadLine();
            
            try
            {
                var command = ParseCommand(input);
                command.Execute();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
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
            parts.Add(currentToken);

        return ParseCommandTokenized(parts.ToArray());
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
                    if (bool.TryParse(value, out bool boolValue))
                    {
                        args[key] = boolValue;
                    }
                    else
                    {
                        args[key] = value;
                    }
                }
                else
                {
                    // Assume the flag is a boolean that defaults to true
                    args[input[i].Substring(2)] = true;
                }
            }
        }

        bool helpMode = commandName == "help" && input.Length > 1 ? true : false;

        if (helpMode) 
        {
            commandName = input[1].ToLower();
        }

        ICommand command = commandName switch
        {
            "hello" => new HelloCommand(),
            "exit" => new ExitCommand(),
            "clear" => new ClearCommand(),
            "importraw" => new ImportRawCommand(),
            "autotest" => new AutotestCommand(),
            "help" => new HelpCommand(),
            _ => throw new InvalidOperationException("Unknown command.")
        };

        if (helpMode)
        {
            command.ShowUsage();     
            command = new NullCommand();
        }

        command.SetArgs(args); // Set arguments before returning the command
        return command;
    }
}