namespace Volatility.CLI.Commands;

// This is a command to test parsing string and boolean arguments.
internal class HelloCommand : ICommand
{
    public string CommandToken => "hello";
    public string CommandDescription => "NOTE: This is a test command, and will be removed later.";
    public string CommandParameters => "[--name <name>] [--loud]";

    public string? Name { get; set; }
    public bool Loud { get; set; }

    public void Execute()
    {
        var greeting = $"Hello, {Name}!";
        if (Loud)
            greeting = greeting.ToUpper();

        Console.WriteLine(greeting);
    }

    public void SetArgs(Dictionary<string, object> args)
    {
        Name = args.TryGetValue("name", out object? name) ? name as string : "World";
        Loud = args.TryGetValue("loud", out var loud) && (bool)loud;
    }
}