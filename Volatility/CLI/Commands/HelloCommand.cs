namespace Volatility;

// This is a command to test parsing string and boolean arguments.
class HelloCommand : ICommand
{
    public string Name { get; set; }
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

    public void ShowUsage()
    {
        Console.WriteLine
        (
            "Usage: hello [--name <name>] [--loud]" +
            "\nNOTE: This is a test command, and will be removed later."
        );
    }
}