// See https://aka.ms/new-console-template for more information
using Bogus;
using System.CommandLine;
using System.Text;

public class Program
{
    public enum CsvColumnsOption
    {
        A,
        B,
        C
    }
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
        // env count command
        var envCountCommand = new Command("count", "Command to count environmental variables");
        envCountCommand.SetHandler(() => Console.WriteLine(Environment.GetEnvironmentVariables().Count));
        // env names command
        var envNamesCommand = new Command("names", "Command to list environmental variable names");
        envNamesCommand.SetHandler(() => Console.WriteLine(string.Join(Environment.NewLine, Environment.GetEnvironmentVariables().Keys.OfType<string>())));
        // env print command
        var envPrintCommand = new Command("print", "Command to print environmental variable value");
        var envPrintArgument = new Argument<string>(name: "name", description: "Environmental variable name", getDefaultValue: () => string.Empty);
        envPrintCommand.AddArgument(envPrintArgument);
        envPrintCommand.SetHandler((name) => Console.WriteLine(Environment.GetEnvironmentVariable(name) ?? string.Empty), envPrintArgument);

        var envCommand = new Command("env", "Command to related to environmental variables") {
            envCountCommand,
            envNamesCommand,
            envPrintCommand
        };

        // counter command
        var counterCommand = new Command("counter", "Command to count to the specified number");
        var counterNumberArgument = new Argument<long>(name: "number", description: "Number to count to", getDefaultValue: () => Convert.ToInt64(int.MaxValue));
        counterCommand.AddArgument(counterNumberArgument);
        counterCommand.SetHandler((number) =>
        {
            for (var i = 0; i < number; i++)
            { Console.WriteLine(i); }
        }, counterNumberArgument);

        // csv command
        var csvCommand = new Command("csv", "Command to generate a CSV file");
        var countOption = new Option<long>(name: "--count", getDefaultValue: () => 3, description: "Number of lines to generate in CSV");
        countOption.AddAlias("-c");
        var seedOption = new Option<int>(name: "--seed", getDefaultValue: () => 42, description: "Seed for pseudorandom generator");
        seedOption.AddAlias("-s");
        //Randomizer.Seed = new Random(8675309);
        csvCommand.AddOption(countOption);
        csvCommand.AddOption(seedOption);

        csvCommand.SetHandler((seed, count) =>
        {
            count = Math.Abs(count);
            var faker = new Faker();
            faker.Random = new Randomizer(seed);
            for (int i = 0; i < count; i++)
            {
                Console.WriteLine(string.Join(',', faker.Phone.PhoneNumber(), faker.Person.FirstName, faker.Address.City()));
            }
        }, seedOption, countOption);


        var rootCommand = new RootCommand {
            echoCommand,
            exitCommand,
            sleepCommand,
            envCommand,
            counterCommand,
            csvCommand
        };
        await rootCommand.InvokeAsync(args);
    }
}