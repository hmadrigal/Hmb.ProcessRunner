// See https://aka.ms/new-console-template for more information
using Bogus;
using System.CommandLine;
using System.CommandLine.Parsing;
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
        var echoMessageArgument = new Argument<string>(name: "message") { Description = "Message to echo", DefaultValueFactory = argumentResult => string.Empty };
        echoCommand.Arguments.Add(echoMessageArgument);
        echoCommand.SetAction(parseResult => Console.WriteLine("{0}", parseResult.GetValue(echoMessageArgument)));

        // exit command
        var exitCommand = new Command("exit", "Exits the application");
        var exitCodeArgument = new Argument<int>(name: "exitCode") { Description = "Application exit code", DefaultValueFactory = argumentResult => 0 };
        exitCommand.Arguments.Add(exitCodeArgument);
        exitCommand.SetAction(parseResult => Environment.Exit(parseResult.GetValue(exitCodeArgument)));

        // sleep command
        var sleepCommand = new Command("sleep", "Sleeps for the specified number of seconds");
        var sleepSecondsArgument = new Argument<int>(name: "seconds") { Description = "Number of seconds to sleep", DefaultValueFactory = argumentResult => 1 };
        sleepCommand.Arguments.Add(sleepSecondsArgument);
        sleepCommand.SetAction(async parseResult => await Task.Delay(TimeSpan.FromSeconds(parseResult.GetValue(sleepSecondsArgument))));

        // env command
        // env count command
        var envCountCommand = new Command("count", "Command to count environmental variables");
        envCountCommand.SetAction(parseResult => Console.WriteLine(Environment.GetEnvironmentVariables().Count));
        // env names command
        var envNamesCommand = new Command("names", "Command to list environmental variable names");
        envNamesCommand.SetAction(parseResult => Console.WriteLine(string.Join(Environment.NewLine, Environment.GetEnvironmentVariables().Keys.OfType<string>())));
        // env print command
        var envPrintCommand = new Command("print", "Command to print environmental variable value");
        var envPrintArgument = new Argument<string>(name: "name") { Description = "Environmental variable name", DefaultValueFactory = argumentResult => string.Empty };
        envPrintCommand.Arguments.Add(envPrintArgument);
        envPrintCommand.SetAction(parseResult => Console.WriteLine(Environment.GetEnvironmentVariable(parseResult.GetValue(envPrintArgument) ?? string.Empty) ?? string.Empty));

        var envCommand = new Command("env", "Command to related to environmental variables") {
            envCountCommand,
            envNamesCommand,
            envPrintCommand
        };

        // counter command
        var counterCommand = new Command("counter", "Command to count to the specified number");
        var counterNumberArgument = new Argument<long>  (name: "number") { Description = "Number to count to", DefaultValueFactory = argumentResult => Convert.ToInt64(int.MaxValue) };
        counterCommand.Arguments.Add(counterNumberArgument);
        counterCommand.SetAction(parseResult => 
        {
            for (var i = 0; i < parseResult.GetValue(counterNumberArgument); i++)
            { Console.WriteLine(i); }
        });

        // csv command
        var csvCommand = new Command("csv", "Command to generate a CSV file");
        var countOption = new Option<long>(name: "--count") { DefaultValueFactory = argumentResult => 3, Description = "Number of lines to generate in CSV" };
        countOption.Aliases.Add("-c");
        var seedOption = new Option<int>(name: "--seed") { DefaultValueFactory = argumentResult => 42, Description = "Seed for pseudorandom generator" };
        seedOption.Aliases.Add("-s");
        //Randomizer.Seed = new Random(8675309);
        csvCommand.Options.Add(countOption);
        csvCommand.Options.Add(seedOption);

        csvCommand.SetAction(parseResult =>
        {
            var count = Math.Abs(parseResult.GetValue(countOption));
            var faker = new Faker();
            faker.Random = new Randomizer(parseResult.GetValue(seedOption));
            for (int i = 0; i < count; i++)
            {
                Console.WriteLine(string.Join(',', faker.Phone.PhoneNumber(), faker.Person.FirstName, faker.Address.City()));
            }
        });


        var rootCommand = new RootCommand {
            echoCommand,
            exitCommand,
            sleepCommand,
            envCommand,
            counterCommand,
            csvCommand
        };
        rootCommand.Parse(args).Invoke();
    }
}