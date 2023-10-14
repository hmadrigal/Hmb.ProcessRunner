# Hmb.ProcessRunner

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
  echo <message>   Echos the first argument []
  exit <exitCode>  Exits the application [default: 0]
  sleep <seconds>  Sleeps for the specified number of seconds [default: 1]
  env              Command to related to environmental variables
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
var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "echo $env:PATH" : "echo $PATH";
var customPath = $"{Path.PathSeparator}:.{Path.DirectorySeparatorChar}";

// act
var sb = new StringBuilder();
using var sw = new StringWriter(sb);
var exitCode = await _processService.ExecuteAsync(
    command,
    standardOutputWriter: sw,
    surrogateEnvironmentalVariables: new Dictionary<string, string?> {
        { "PATH", customPath }
    });

// assert
Assert.That(exitCode, Is.EqualTo(0));
Assert.That(sb.ToString().Trim(), Is.EqualTo(customPath));
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