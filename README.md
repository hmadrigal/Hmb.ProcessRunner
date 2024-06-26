# Hmb.ProcessRunner

## Repository

[GitHub - Hmb.ProcessRunner](https://github.com/hmadrigal/Hmb.ProcessRunner)

## Summary

`Hmb.ProcessRunner` is a library for executing shell commands. The class `ProcessService` exposes two methods:

- `ExecuteAsync` to execute a command thru a OS Shell.
- `Which` returns a list of file paths from PATH env var where the given file has been found.

**Why?**

- Do not worry about OS. Default shells will be used in major OS (Windows, OSX, Linux).
- Run inline scripts, command is not limited to executable files. inline commands such as `echo $env:PATH` or `echo $PATH` can be used.
- Long running command with a lot of output can take advantage of `System.Threading.Channel`
- Use `await` to continue execution once command finishes.


## Examples

Following examples uses the sample application `Hmb.ProcessRunner.TestApp.exe`, it supports following subcommands:

```
Description:

Usage:
  Hmb.ProcessRunner.TestApp [command] [options]

Options:
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  echo <message>    Echos the first argument []
  exit <exitCode>   Exits the application [default: 0]
  sleep <seconds>   Sleeps for the specified number of seconds [default: 1]
  env               Command to related to environmental variables
  counter <number>  Command to count to the specified number [default: 2147483647]
  csv               Command to generate a CSV file
```

### Execute command without error

```csharp
// arrange
var command = $"dotnet {TestAppFilePath} echo \"Hello World\"";

// act
var exitCode = await _processService.ExecuteAsync(command);

// assert
Assert.That(exitCode, Is.EqualTo(0));
```

### Capture unicode in standard output writer

```csharp
// arrange
var unicodeMessage = "Hello World 🌍 「世界、こんにちは。」";
var command = $@"dotnet {TestAppFilePath} echo ""{unicodeMessage}""";

// act
var sb = new StringBuilder();
var exitCode = await _processService.ExecuteAsync(
    command,
    standardOutputWriter: new StringWriter(sb),
    standardOutputEncoding: Encoding.UTF8
    );

// assert
Assert.That(exitCode, Is.EqualTo(0));
Assert.That(sb.ToString(), Is.EqualTo(unicodeMessage));
```

### Overrides environment variables
```csharp
// arrange
string EnvVarName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "HOMEPATH" : @"HOME";
string command = $@"dotnet {TestAppFilePath} env print {EnvVarName}";
var envVarValue = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

// act
var sb = new StringBuilder();
using var sw = new StringWriter(sb);
var exitCode = await _processService.ExecuteAsync(
    command,
    standardOutputWriter: sw,
    surrogateEnvironmentalVariables: new Dictionary<string, string?> {
        { EnvVarName, envVarValue }
    });

// assert
string standardOutput = sb.ToString().Trim();
Assert.That(exitCode, Is.EqualTo(0));
// NOTE: Path seems to be modified by test suite, that is why it checks that end of path
Assert.That(standardOutput, Is.EqualTo(envVarValue));
```

### Which finds `dotnet` command
```csharp
// arrange
var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";

// act
var foundFilePaths = _processService.Which(command);

// assert
Assert.That(foundFilePaths, Is.Not.Null);
Assert.That(foundFilePaths.Any(), Is.True);
```

### Using `System.Threading.Channel` to capture large outputs
```csharp
// arrange
const int countLimit = 20_000;
string command = $@"dotnet {TestAppFilePath} csv --count {countLimit}";
Channel<string> stdOutputChannel = Channel.CreateUnbounded<string>();


// act
Task producer = Task.Run(async () =>
{
    await _processService.ExecuteAsync(
        command: command,
        standardOutputChannel: stdOutputChannel
    );
});
int counter = 0;
Task consumer = Task.Run(async () =>
{
    await foreach (var standardOutputLine in stdOutputChannel.Reader.ReadAllAsync())
    {
        counter++;
    }
});
await Task.WhenAll(producer, consumer);

// assert
Assert.That(counter, Is.EqualTo(countLimit));
```