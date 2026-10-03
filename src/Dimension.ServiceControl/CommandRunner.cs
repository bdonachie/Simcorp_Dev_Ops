using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Dimension.ServiceControl
{
    /// <summary>
    /// What one ScdSvcCtl.exe run produced.
    /// </summary>
    public sealed class CommandResult
    {
        internal CommandResult(int rawExitCode, string standardOutput, string standardError, string commandLine)
        {
            RawExitCode = rawExitCode;
            StandardOutput = standardOutput;
            StandardError = standardError;
            CommandLine = commandLine;
        }

        /// <summary>The exit code exactly as the process returned it.</summary>
        public int RawExitCode { get; }

        /// <summary>
        /// The exit code mapped onto the documented list. An unknown code is still returned
        /// as-is, so <see cref="RawExitCode"/> stays available.
        /// </summary>
        public ServiceControlExitCode ExitCode
        {
            get { return (ServiceControlExitCode)RawExitCode; }
        }

        /// <summary>Whether the exit code is one this library knows about.</summary>
        public bool IsKnownExitCode
        {
            get { return Enum.IsDefined(typeof(ServiceControlExitCode), RawExitCode); }
        }

        /// <summary>Whatever the utility wrote to standard output.</summary>
        public string StandardOutput { get; }

        /// <summary>Whatever the utility wrote to standard error.</summary>
        public string StandardError { get; }

        /// <summary>The full command line that was run.</summary>
        public string CommandLine { get; }
    }

    /// <summary>
    /// Runs ScdSvcCtl.exe and captures its output.
    /// </summary>
    public static class CommandRunner
    {
        private static readonly TimeSpan StreamDrainTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Runs the utility with the given arguments and waits for it to finish.
        /// </summary>
        /// <param name="options">Where the utility lives and how long to wait for it.</param>
        /// <param name="arguments">The rendered argument string.</param>
        /// <returns>The exit code and captured output.</returns>
        public static CommandResult Run(ServiceControlOptions options, string arguments)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            string commandLine = options.ExecutablePath + " " + arguments;

            var startInfo = new ProcessStartInfo
            {
                FileName = options.ExecutablePath,
                Arguments = arguments,
                WorkingDirectory = options.ResolveWorkingDirectory(),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            var outputText = new StringBuilder();
            var errorText = new StringBuilder();

            // The output streams are drained on background threads. Reading them after
            // WaitForExit instead would deadlock as soon as a pipe buffer fills, which a
            // large service listing does easily.
            using (var outputFinished = new ManualResetEventSlim(false))
            using (var errorFinished = new ManualResetEventSlim(false))
            using (var process = new Process { StartInfo = startInfo })
            {
                process.OutputDataReceived += (sender, line) =>
                {
                    if (line.Data == null)
                    {
                        outputFinished.Set();
                    }
                    else
                    {
                        outputText.AppendLine(line.Data);
                    }
                };

                process.ErrorDataReceived += (sender, line) =>
                {
                    if (line.Data == null)
                    {
                        errorFinished.Set();
                    }
                    else
                    {
                        errorText.AppendLine(line.Data);
                    }
                };

                try
                {
                    process.Start();
                }
                catch (Win32Exception startFailure)
                {
                    throw new ServiceControlException(
                        "ScdSvcCtl.exe could not be started from '" + options.ExecutablePath
                        + "'. Check the path is reachable and the account can read the share.",
                        commandLine,
                        startFailure);
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                bool finishedInTime = process.WaitForExit((int)options.CommandTimeout.TotalMilliseconds);

                if (!finishedInTime)
                {
                    StopAbandonedProcess(process);
                    throw new CommandTimeoutException(options.CommandTimeout, commandLine);
                }

                // Exiting does not guarantee the redirected streams are drained, so wait for
                // the end of stream callbacks before reading what was captured.
                outputFinished.Wait(StreamDrainTimeout);
                errorFinished.Wait(StreamDrainTimeout);

                return new CommandResult(
                    process.ExitCode,
                    outputText.ToString(),
                    errorText.ToString(),
                    commandLine);
            }
        }

        /// <summary>
        /// Stops a process that ran past its timeout, ignoring the race where it exits on
        /// its own just as the stop lands.
        /// </summary>
        private static void StopAbandonedProcess(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (InvalidOperationException)
            {
                // Already gone between the check and the kill.
            }
            catch (Win32Exception)
            {
                // Already terminating.
            }
        }
    }
}
