using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;

namespace Hmb.ProcessRunner.Tests;

internal class ProcessServiceTests
{
    private static readonly string TestAppFilePath = typeof(Program).Assembly.Location;

    private ProcessService _processService;

    [SetUp]
    public void Setup()
    {
        _processService = new ProcessService();
    }

    [Test]
    public async Task ExecuteCommandWithNoException()
    {
        // arrange
        var command = $"dotnet {TestAppFilePath} echo \"Hello World\"";

        // act
        var exitCode = await _processService.ExecuteAsync(command);

        // assert
        Assert.That(exitCode, Is.EqualTo(0));
    }

    [Test]
    public async Task CaptureUnicodeInStandardOutputWriter()
    {
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
    }

    [Test]
    public async Task CaptureUnicodeInStandardOutputChannel()
    {
        // arrange
        var unicodeMessage = "Hello World 🌍 「世界、こんにちは。」";
        var command = $@"dotnet {TestAppFilePath} echo ""{unicodeMessage}""";

        // act
        var commandStdOuputChannel = Channel.CreateUnbounded<string>();
        var exitCode = await _processService.ExecuteAsync(
            command,
            standardOutputChannel: commandStdOuputChannel,
            standardOutputEncoding: Encoding.UTF8
        );

        var sb = new StringBuilder();
        await foreach (var data in commandStdOuputChannel.Reader.ReadAllAsync())
        {
            sb.Append(data);
        }

        // assert
        Assert.That(exitCode, Is.EqualTo(0));
        Assert.That(sb.ToString(), Is.EqualTo(unicodeMessage));

    }

    [Test]
    public async Task ReturnsExitCode()
    {
        // arrange
        var command = $"dotnet {TestAppFilePath} exit 42";

        // act
        var exitCode = await _processService.ExecuteAsync(command);

        // assert
        Assert.That(exitCode, Is.EqualTo(42));
    }

    [Test]
    public void CancellationProcessStopsProcessing()
    {
        // arrange
        var command = $"dotnet {TestAppFilePath} sleep 60";
        var cancellationTokenSource = new CancellationTokenSource();

        // act
        var task = _processService.ExecuteAsync(command, cancellationToken: cancellationTokenSource.Token);
        cancellationTokenSource.CancelAfter(1000);
        var taskCanceledException = Assert.ThrowsAsync<TaskCanceledException>(async () => await task);

        // assert
        Assert.That(taskCanceledException, Is.Not.Null);
        Assert.That(task.IsCanceled, Is.True);
    }

    [Test]
    public void WhichFindDotNetCommand()
    {
        // arrange
        var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";

        // act
        var foundFilePaths = _processService.Which(command);

        // assert
        Assert.That(foundFilePaths, Is.Not.Null);
        Assert.That(foundFilePaths.Any(), Is.True);
    }

    [Test]
    public async Task OverridesEnvironmentVariables()
    {
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
    }

}
