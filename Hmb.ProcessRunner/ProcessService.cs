using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;

namespace Hmb.ProcessRunner;

/// <summary>
/// ProcessService is a service for executing commands in a new process.
/// </summary>
public class ProcessService
{

    private readonly Dictionary<Process, (TextWriter, TextWriter, Channel<string>?, Channel<string>?, CancellationToken)> _processes = new();

    /// <summary>
    /// Looks for a file in the directories specified in the PATH environment variable.
    /// Additional lookup paths can be specified, if not provided the current directory is used.
    /// </summary>
    /// <param name="executableFileName"></param>
    /// <param name="additionalLookupPaths"></param>
    /// <returns></returns>
    public IEnumerable<string> Which(
        string executableFileName,
        params string[] additionalLookupPaths)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableFileName, nameof(executableFileName));

        additionalLookupPaths ??= new string[] { Environment.CurrentDirectory };

        var environmentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        IEnumerable<string> lookupPaths = environmentPath.Split(Path.PathSeparator);
        lookupPaths = lookupPaths.Concat(additionalLookupPaths);

        foreach (var path in lookupPaths)
        {
            var fullPath = Path.Combine(path, executableFileName);
            if (File.Exists(fullPath))
            {
                yield return fullPath;
            }
        }
    }

    /// <summary>
    /// Executes a command in a new process.
    /// </summary>
    /// <param name="command">The command to execute</param>
    /// <param name="workingDirectory">Working directory when executing the command</param>
    /// <param name="standardOutputWriter">TextWriter for the standard output</param>
    /// <param name="standardErrorWriter">TextWriter for the error output</param>
    /// <param name="standardOutputChannel">Channel (publisher/subscriber) for standard output</param>
    /// <param name="standardErrorChannel">Channel (publisher/subscriber) for error output</param>
    /// <param name="standardOutputEncoding">Text encoding to be used in the standard output</param>
    /// <param name="standardErrorEncoding">Text encoding to be used in the error output</param>
    /// <param name="standardInputEncoding">Text encoding to be used in the standard input</param>
    /// <param name="retainedEnvironmentalVariables">When provided only specified variables are passed to child process (By default all variables are passed)</param>
    /// <param name="surrogateEnvironmentalVariables">When provided specified variables are overridden with new values</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ProcessServiceException"></exception>
    public async Task<int> ExecuteAsync(
        string command,
        string? workingDirectory = null,
        TextWriter? standardOutputWriter = null,
        TextWriter? standardErrorWriter = null,
        Channel<string>? standardOutputChannel = null,
        Channel<string>? standardErrorChannel = null,
        Encoding? standardOutputEncoding = null,
        Encoding? standardErrorEncoding = null,
        Encoding? standardInputEncoding = null,
        IEnumerable<string>? retainedEnvironmentalVariables = null,
        IDictionary<string, string?>? surrogateEnvironmentalVariables = null,
        CancellationToken cancellationToken = default
        )
    {
        standardOutputWriter ??= TextWriter.Null;
        standardErrorWriter ??= TextWriter.Null;
        surrogateEnvironmentalVariables ??= new Dictionary<string, string?>();

        using var process = GetNewProcessInstance(
            command,
            workingDirectory,
            standardOutputEncoding,
            standardErrorEncoding,
            standardInputEncoding
        );

        _processes[process] = (
            standardOutputWriter,
            standardErrorWriter,
            standardOutputChannel,
            standardErrorChannel,
            cancellationToken);

        if (retainedEnvironmentalVariables != null)
        {
            var keysToRemove = process.StartInfo.Environment.Keys
                .Where(key => !retainedEnvironmentalVariables.Contains(key));
            foreach (var key in keysToRemove)
            {
                process.StartInfo.Environment.Remove(key);
            }
        }

        foreach (var envVar in surrogateEnvironmentalVariables)
        { process.StartInfo.Environment[envVar.Key] = surrogateEnvironmentalVariables[envVar.Key]; }

        process.OutputDataReceived += OnProcessOutputDataReceived;
        process.ErrorDataReceived += OnProcessErrorDataReceived;

        try
        {
            var hasProcessStarted = process.Start();
            if (!hasProcessStarted)
            {
                throw new ProcessServiceException($"Failed to execute command: {command}");
            }
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        }
        finally
        {
            _processes.Remove(process);
            process.OutputDataReceived -= OnProcessOutputDataReceived;
            process.ErrorDataReceived -= OnProcessErrorDataReceived;
        }

        standardOutputChannel?.Writer.TryComplete();
        standardErrorChannel?.Writer.TryComplete();

        await standardOutputWriter.FlushAsync().ConfigureAwait(false);
        await standardErrorWriter.FlushAsync().ConfigureAwait(false);

        return process.ExitCode;
    }

    private Process GetNewProcessInstance(
        string command,
        string? workingDirectory = null,
        Encoding? standardOutputEncoding = null,
        Encoding? standardErrorEncoding = null,
        Encoding? standardInputEncoding = null
    )
    {
        workingDirectory ??= Environment.CurrentDirectory;

        var process = new Process();
        process.StartInfo.WorkingDirectory = workingDirectory;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.StandardOutputEncoding = standardOutputEncoding;
        process.StartInfo.StandardErrorEncoding = standardErrorEncoding;
        process.StartInfo.StandardInputEncoding = standardInputEncoding;


        var commandComposers = CommandComposer.GetCommandComposers();
        foreach (var commandComposer in commandComposers)
        {
            var (osCommand, arguments) = commandComposer.GetCommandLine(command);

            if (!File.Exists(osCommand))
            { osCommand = Which(osCommand).FirstOrDefault(); }

            if (osCommand == null)
            { continue; }

            process.StartInfo.FileName = osCommand;
            arguments.ForEach(process.StartInfo.ArgumentList.Add);
            break;
        }

        if (string.IsNullOrEmpty(process.StartInfo.FileName))
        {
            throw new ProcessServiceException($"Process creation in: {RuntimeInformation.OSDescription} is not supported.");
        }

        return process;
    }

    private async void OnProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        var process = (Process)sender;
        var (_, stdErrWriter, _, stdErrChannel, cancellationToken) = _processes[process];

        cancellationToken.ThrowIfCancellationRequested();

        await WriteReceivedData(e, stdErrWriter, stdErrChannel, cancellationToken).ConfigureAwait(false);
    }

    private async void OnProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        var process = (Process)sender;
        var (stdOutWriter, _, stdOutChannel, _, cancellationToken) = _processes[process];

        cancellationToken.ThrowIfCancellationRequested();

        await WriteReceivedData(e, stdOutWriter, stdOutChannel, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteReceivedData(DataReceivedEventArgs dataReceivedEventArgs, TextWriter textWriter, Channel<string>? channel, CancellationToken cancellationToken)
    {
        if (dataReceivedEventArgs.Data == null)
        {
            await textWriter.FlushAsync().ConfigureAwait(false);
            return;
        }

        await textWriter.WriteAsync(dataReceivedEventArgs.Data).ConfigureAwait(false);

        if (channel != null)
        {
            await channel.Writer.WriteAsync(dataReceivedEventArgs.Data, cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

}