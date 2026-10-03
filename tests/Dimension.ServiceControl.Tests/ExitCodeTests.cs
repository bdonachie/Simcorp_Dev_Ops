using Xunit;

namespace Dimension.ServiceControl.Tests
{
    public class ExitCodeTests
    {
        [Theory]
        [InlineData(ServiceControlExitCode.Success, false, false)]
        [InlineData(ServiceControlExitCode.MaintenanceMode, false, true)]
        [InlineData(ServiceControlExitCode.PlatformSuspended, false, true)]
        [InlineData(ServiceControlExitCode.AuthorizationFailed, true, false)]
        [InlineData(ServiceControlExitCode.StopServicesSomeFailed, true, false)]
        [InlineData((ServiceControlExitCode)99, true, false)]
        public void OnlyPositiveCodesAreFailuresAndOnlyNegativeOnesAreState(
            ServiceControlExitCode exitCode, bool isFailure, bool isState)
        {
            Assert.Equal(isFailure, exitCode.IsFailure());
            Assert.Equal(isState, exitCode.IsStateInformation());
        }
    }

    public class CommandFailedExceptionTests
    {
        [Fact]
        public void Message_NamesTheDocumentedCodeAndPrefersStandardError()
        {
            var failure = new CommandFailedException(
                ServiceControlExitCode.AuthorizationFailed, 11, "ScdSvcCtl.exe service list",
                standardError: "Access denied.\r\n", standardOutput: "ignored when stderr is present");

            Assert.Equal("ScdSvcCtl.exe failed with AuthorizationFailed (11). Access denied.", failure.Message);
            Assert.Equal("ScdSvcCtl.exe service list", failure.CommandLine);
            Assert.Equal(11, failure.RawExitCode);
        }

        [Fact]
        public void Message_FallsBackToStandardOutputWhenStandardErrorIsEmpty()
        {
            var failure = new CommandFailedException(
                ServiceControlExitCode.CommandFailed, 3, "x", standardError: "", standardOutput: "No such agent.");

            Assert.Equal("ScdSvcCtl.exe failed with CommandFailed (3). No such agent.", failure.Message);
        }

        [Fact]
        public void Message_SaysWhenTheCodeIsUndocumented()
        {
            var failure = new CommandFailedException((ServiceControlExitCode)99, 99, "x", "", "");

            Assert.Equal("ScdSvcCtl.exe failed with undocumented exit code 99.", failure.Message);
        }
    }
}
