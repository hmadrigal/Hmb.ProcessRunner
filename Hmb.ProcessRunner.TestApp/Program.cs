// See https://aka.ms/new-console-template for more information
using System.CommandLine;
using System.Text;

public class Program
{
    private static async Task Main(string[] args)
    {
        // enabling unicode code characters for input/output
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        // echo command
        var echoCommand = new Command("echo", "Echos the first argument");
        var echoMessageArgument = new Argument<string>(name: "message", description: "Message to echo", getDefaultValue: () => string.Empty);
        echoCommand.AddArgument(echoMessageArgument);
        echoCommand.SetHandler(msg => Console.WriteLine("{0}", msg), echoMessageArgument);

        // exit command
        var exitCommand = new Command("exit", "Exits the application");
        var exitCodeArgument = new Argument<int>(name: "exitCode", description: "Application exit code", getDefaultValue: () => 0);
        exitCommand.AddArgument(exitCodeArgument);
        exitCommand.SetHandler(Environment.Exit, exitCodeArgument);

        // sleep command
        var sleepCommand = new Command("sleep", "Sleeps for the specified number of seconds");
        var sleepSecondsArgument = new Argument<int>(name: "seconds", description: "Number of seconds to sleep", getDefaultValue: () => 1);
        sleepCommand.AddArgument(sleepSecondsArgument);
        sleepCommand.SetHandler(async (seconds) => await Task.Delay(TimeSpan.FromSeconds(seconds)), sleepSecondsArgument);

        // env command
        var envCountCommand = new Command("count", "Command to count environmental variables");
        envCountCommand.SetHandler(() => Console.WriteLine(Environment.GetEnvironmentVariables().Count));
        var envNamesCommand = new Command("names", "Command to list environmental variable names");
        envNamesCommand.SetHandler(() => Console.WriteLine(string.Join(Environment.NewLine, Environment.GetEnvironmentVariables().Keys.OfType<string>())));
        var envCommand = new Command("env", "Command to related to environmental variables") {
            envCountCommand,
            envNamesCommand
        };


        var rootCommand = new RootCommand {
            echoCommand,
            exitCommand,
            sleepCommand,
            envCommand
        };
        await rootCommand.InvokeAsync(args);
    }
}