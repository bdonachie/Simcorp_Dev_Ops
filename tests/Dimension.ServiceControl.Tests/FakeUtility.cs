using System;
using System.IO;
using System.Text;

namespace Dimension.ServiceControl.Tests
{
    /// <summary>
    /// A stand-in for ScdSvcCtl.exe: a batch file that prints a canned response and exits
    /// with a chosen code. Windows runs .cmd files through cmd.exe when they are started
    /// directly, so the client drives it exactly as it would drive the real utility.
    /// </summary>
    internal sealed class FakeUtility : IDisposable
    {
        private readonly string directory;

        private FakeUtility(string directory, string executablePath)
        {
            this.directory = directory;
            ExecutablePath = executablePath;
        }

        public string ExecutablePath { get; }

        /// <summary>
        /// A utility that prints <paramref name="standardOutput"/>, optionally writes
        /// <paramref name="standardError"/> to stderr, and exits with
        /// <paramref name="exitCode"/>.
        /// </summary>
        public static FakeUtility Returning(string standardOutput, int exitCode = 0, string standardError = null)
        {
            string directory = CreateDirectory();
            File.WriteAllText(Path.Combine(directory, "response.txt"), standardOutput ?? string.Empty);

            var script = new StringBuilder();
            script.AppendLine("@echo off");
            script.AppendLine("type \"%~dp0response.txt\"");

            if (standardError != null)
            {
                File.WriteAllText(Path.Combine(directory, "error.txt"), standardError);
                script.AppendLine("type \"%~dp0error.txt\" 1>&2");
            }

            script.AppendLine("exit /b " + exitCode);

            return Write(directory, script.ToString());
        }

        /// <summary>
        /// A utility that echoes back whatever arguments it was given.
        /// </summary>
        public static FakeUtility EchoingArguments()
        {
            string directory = CreateDirectory();
            var script = new StringBuilder();
            script.AppendLine("@echo off");
            script.AppendLine("echo %*");
            script.AppendLine("exit /b 0");
            return Write(directory, script.ToString());
        }

        /// <summary>
        /// A utility that sleeps for roughly <paramref name="seconds"/> before exiting,
        /// holding no handle on the redirected streams so a timeout test returns promptly.
        /// </summary>
        public static FakeUtility SleepingFor(int seconds)
        {
            string directory = CreateDirectory();
            var script = new StringBuilder();
            script.AppendLine("@echo off");
            script.AppendLine("ping -n " + (seconds + 1) + " 127.0.0.1 >nul 2>nul");
            script.AppendLine("exit /b 0");
            return Write(directory, script.ToString());
        }

        public ServiceControlClient CreateClient(Action<ServiceControlOptions> configure = null)
        {
            var options = new ServiceControlOptions(ExecutablePath);
            configure?.Invoke(options);
            return new ServiceControlClient(options);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
                // A straggling cmd.exe can hold the folder for a moment; leaving a temp
                // folder behind is not worth failing a test over.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static string CreateDirectory()
        {
            string directory = Path.Combine(Path.GetTempPath(), "scdsvcctl-fake-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static FakeUtility Write(string directory, string script)
        {
            string executablePath = Path.Combine(directory, "ScdSvcCtl.cmd");
            File.WriteAllText(executablePath, script);
            return new FakeUtility(directory, executablePath);
        }
    }
}
