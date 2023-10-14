using System.Runtime.InteropServices;
using System.Text;

namespace Hmb.ProcessRunner
{
    /// <summary>
    /// CommandComposer is a base class for composing the command line for a specific OS.
    /// </summary>
    public abstract record CommandComposer
    {
        /// <summary>
        /// Composes the command line for the current OS.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public abstract (string osCommand, List<string> arguments) GetCommandLine(string command);

        /// <summary>
        /// Gets the command composers which can be used to compose the command line for the current OS.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ProcessServiceException"></exception>
        public static IEnumerable<CommandComposer> GetCommandComposers()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                yield return new PowerShellExeCommandComposer();
                yield return new CmdExeCommandComposer();
                yield return new PwshExeCommandComposer();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                yield return new ShCommandComposer();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                yield return new ZshCommandComposer();
            }
            else
            {
                throw new ProcessServiceException($"Unsupported OS: {RuntimeInformation.OSDescription}");
            }
        }
    }

    /// <summary>
    /// CmdExeCommandComposer is a command composer for cmd.exe
    /// </summary>
    internal sealed record CmdExeCommandComposer : CommandComposer
    {
        /// <summary>
        /// Composes the CLI for cmd.exe to execute <paramref name="command"/> 
        /// <code>
        /// cmd.exe /U /C <paramref name="command"/> 
        /// </code>
        /// </summary>
        /// <remarks>
        /// cmd.exe should be in path, normally on Windows %SystemRoot% is set to C:\WINDOWS, 
        /// and the command will use %SystemRoot%\System32\cmd.exe
        /// </remarks>
        /// <param name="command"></param>
        /// <returns></returns>
        public override (string osCommand, List<string> arguments) GetCommandLine(string command)
            => ("cmd.exe", new List<string> {
                "/U",
                "/C",
                command
            });
    }

    /// <summary>
    /// PowerShellExeCommandComposer is a command composer for powershell.exe
    /// </summary>
    internal sealed record PowerShellExeCommandComposer : CommandComposer
    {
        /// <summary>
        /// Composes the CLI for powershell.exe to execute <paramref name="command"/>
        /// <code>
        /// powershell.exe -NoLogo -Mta -NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand <paramref name="command"/>
        /// </code>
        /// </summary>
        /// <remarks>
        /// powershell.exe should be in path, normally on Windows %SystemRoot% is set to C:\WINDOWS,
        /// and the command will use ${env:SystemRoot}\System32\WindowsPowerShell\v1.0\powershell.exe
        /// </remarks>
        /// <param name="command"></param>
        /// <returns></returns>
        public override (string osCommand, List<string> arguments) GetCommandLine(string command)
            => ("powershell.exe", new List<string> {
                "-NoLogo",
                "-Mta",
                "-NoProfile",
                "-NonInteractive",
                "-WindowStyle", "Hidden",
                "-EncodedCommand",
                // NOTE: Appending exit $LASTEXITCODE to the command to get the exit code of the command
                // https://stackoverflow.com/questions/50200325/returning-an-exit-code-from-a-powershell-script
                Convert.ToBase64String(Encoding.Unicode.GetBytes($"{command}; exit $LASTEXITCODE"))
            });
    }

    /// <summary>
    /// PwshExeCommandComposer is a command composer for pwsh.exe
    /// </summary>
    internal sealed record PwshExeCommandComposer : CommandComposer
    {
        /// <summary>
        /// Composes the CLI for pwsh.exe to execute <paramref name="command"/>
        /// <code>
        /// pwsh.exe -NoLogo -Mta -NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand <paramref name="command"/>
        /// </code>
        /// </summary>
        /// <remarks>
        /// pwsh.exe should be in path, normally on Windows ${env:ProgramFiles} i set to C:\Program files,
        /// and the comand wil use ${env:ProgramFiles}\PowerShell\7\pwsh.exe
        /// </remarks>
        /// <param name="command"></param>
        /// <returns></returns>
        public override (string osCommand, List<string> arguments) GetCommandLine(string command)
            => ("pwsh.exe", new List<string> {
                "-NoLogo",
                "-Mta",
                "-NoProfile",
                "-NonInteractive",
                "-WindowStyle", "Hidden",
                "-EncodedCommand",
                // NOTE: Appending exit $LASTEXITCODE to the command to get the exit code of the command
                // https://stackoverflow.com/questions/50200325/returning-an-exit-code-from-a-powershell-script
                Convert.ToBase64String(Encoding.Unicode.GetBytes($"{command}; exit $LASTEXITCODE"))
            });
    }

    /// <summary>
    /// ZshCommandComposer is a command composer for zsh
    /// </summary>
    internal sealed record ZshCommandComposer : CommandComposer
    {
        /// <summary>
        /// Composes the CLI for zsh to execute <paramref name="command"/>
        /// <code>
        /// zsh -l -c <paramref name="command"/>
        /// </code>
        /// </summary>
        /// <remarks>
        /// zhs should be in path, normally it is in /usr/bin/zsh or /bin/zsh
        /// </remarks>
        /// <param name="command"></param>
        /// <returns></returns>
        public override (string osCommand, List<string> arguments) GetCommandLine(string command)
            => ("zsh", new List<string> {
                "-l",
                "-c",
                command
            });
    }

    /// <summary>
    /// ShCommandComposer is a command composer for sh (Bourne Shell)
    /// </summary>
    internal sealed record ShCommandComposer : CommandComposer
    {
        /// <summary>
        /// Composes the CLI for sh to execute <paramref name="command"/>
        /// <code>
        /// sh -c <paramref name="command"/>
        /// </code>
        /// </summary>
        /// <remarks>
        /// sh should be in path, normally it is in /usr/bin/sh or /bin/sh
        /// </remarks>
        /// <param name="command"></param>
        /// <returns></returns>
        public override (string osCommand, List<string> arguments) GetCommandLine(string command)
            => ("sh", new List<string> {
                "-c",
                command
            });
    }

    // Linux has many shells:
    //
    //  - Bourne Shell (sh) ...
    //  - C Shell (csh) ...
    //  - TENEX C Shell (tcsh) ...
    //  - KornShell (ksh) ...
    //  - Debian Almquist Shell (dash) ...
    //  - Bourne Again Shell (bash) ...
    //  - Z Shell (zsh) ...
    //  - Friendly Interactive Shell (fish)
    //  - Powershell (pwsh)

}