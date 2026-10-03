using System;
using System.Diagnostics;
using Xunit;

namespace Dimension.ServiceControl.Tests
{
    /// <summary>
    /// Exercises the process layer against cmd.exe, which is always present on Windows and
    /// can produce any exit code and any amount of output on demand.
    /// </summary>
    public class CommandRunnerTests
    {
        private static ServiceControlOptions CommandPrompt(TimeSpan? timeout = null)
        {
            var options = new ServiceControlOptions(Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe");

            if (timeout.HasValue)
            {
                options.CommandTimeout = timeout.Value;
            }

            return options;
        }

        [Fact]
        public void Run_CapturesOutputAndASuccessExitCode()
        {
            CommandResult result = CommandRunner.Run(CommandPrompt(), "/c echo hello");

            Assert.Equal(0, result.RawExitCode);
            Assert.Equal(ServiceControlExitCode.Success, result.ExitCode);
            Assert.True(result.IsKnownExitCode);
            Assert.Equal("hello", result.StandardOutput.Trim());
            Assert.Equal(string.Empty, result.StandardError.Trim());
            Assert.EndsWith(" /c echo hello", result.CommandLine);
        }

        [Fact]
        public void Run_ReturnsAFailingExitCodeWithoutThrowing()
        {
            CommandResult result = CommandRunner.Run(CommandPrompt(), "/c exit 11");

            Assert.Equal(11, result.RawExitCode);
            Assert.Equal(ServiceControlExitCode.AuthorizationFailed, result.ExitCode);
            Assert.True(result.ExitCode.IsFailure());
        }

        [Fact]
        public void Run_KeepsAnExitCodeTheLibraryDoesNotKnow()
        {
            CommandResult result = CommandRunner.Run(CommandPrompt(), "/c exit 99");

            Assert.Equal(99, result.RawExitCode);
            Assert.False(result.IsKnownExitCode);
        }

        [Fact]
        public void Run_CapturesStandardErrorSeparatelyFromStandardOutput()
        {
            CommandResult result = CommandRunner.Run(CommandPrompt(), "/c echo oops 1>&2");

            Assert.Equal("oops", result.StandardError.Trim());
            Assert.Equal(string.Empty, result.StandardOutput.Trim());
        }

        [Fact]
        public void Run_DrainsMoreOutputThanThePipeBufferHolds()
        {
            // A 4 KB pipe fills long before this finishes. Reading after WaitForExit would
            // deadlock here, which is the failure the background readers exist to prevent.
            CommandResult result = CommandRunner.Run(
                CommandPrompt(),
                "/c for /L %i in (1,1,20000) do @echo line %i of a service listing long enough to fill the pipe");

            string[] lines = result.StandardOutput.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(20000, lines.Length);
            Assert.Contains("line 20000 ", result.StandardOutput);
        }

        [Fact]
        public void Run_ExplainsWhenTheExecutableCannotBeStarted()
        {
            var options = new ServiceControlOptions(@"C:\definitely\missing\ScdSvcCtl.exe");

            var failure = Assert.Throws<ServiceControlException>(() => CommandRunner.Run(options, "version"));

            Assert.Contains("could not be started", failure.Message);
            Assert.Equal(@"C:\definitely\missing\ScdSvcCtl.exe version", failure.CommandLine);
            Assert.NotNull(failure.InnerException);
        }

        [Fact]
        public void Run_StopsAProcessThatOutlivesTheTimeout()
        {
            var options = CommandPrompt(TimeSpan.FromSeconds(1));
            var stopwatch = Stopwatch.StartNew();

            var failure = Assert.Throws<CommandTimeoutException>(
                () => CommandRunner.Run(options, "/c ping -n 15 127.0.0.1 >nul 2>nul"));

            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), "the runner waited for the whole ping");
            Assert.Equal(TimeSpan.FromSeconds(1), failure.CommandTimeout);
        }
    }
}
