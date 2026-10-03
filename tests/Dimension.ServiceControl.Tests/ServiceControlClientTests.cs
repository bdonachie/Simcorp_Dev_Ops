using System;
using Dimension.ServiceControl.Model;
using Xunit;

namespace Dimension.ServiceControl.Tests
{
    /// <summary>
    /// Drives the client end to end against a stand-in utility, so the contract checks
    /// happen on real process output rather than on hand-built responses.
    /// </summary>
    public class ServiceControlClientTests
    {
        [Fact]
        public void ListServices_FlattensEveryServiceOntoItsAgentAndKeepsTheWarnings()
        {
            using (var utility = FakeUtility.Returning(SampleDocuments.ServiceList))
            {
                ServiceListResult result = utility.CreateClient().ListServices();

                Assert.Equal(2, result.Services.Count);
                Assert.Equal(2, result.Agents.Count);

                ServiceInstance online = result.Services[0];
                Assert.Equal("APPSERVER01", online.AgentName);
                Assert.Equal("Primary application server", online.AgentDescription);
                Assert.Equal("blue", online.AgentTag);
                Assert.Equal(@"APPSERVER01\215-1", online.FullName);
                Assert.True(online.IsOnline);
                Assert.Equal(@"APPSERVER01\215-1 [Online]", online.ToString());

                ServiceInstance inoperable = result.Services[1];
                Assert.False(inoperable.IsOnline);
                Assert.True(inoperable.IsStale);

                Assert.True(result.HasWarnings);
                Assert.Equal(2, result.Warnings.Count);
                Assert.Equal("Default", result.ServiceSegment);
            }
        }

        [Fact]
        public void RunCommand_SendsTheRenderedSwitchesToTheUtility()
        {
            using (var utility = FakeUtility.EchoingArguments())
            {
                var arguments = new CommandArgumentBuilder("service", "list")
                    .AddSwitch("format", "xml")
                    .AddSwitchIfSet("agent", "APPSERVER01")
                    .AddFlag("online")
                    .AddYesNoSwitchIfSet("stale", true)
                    .AddSwitch("managementmode", ManagementMode.Automated.ToString());

                CommandResult result = utility.CreateClient().RunCommand(arguments);

                Assert.Equal(
                    "service list -format:xml -agent:APPSERVER01 -online -stale:Yes -managementmode:Automated",
                    result.StandardOutput.Trim());
            }
        }

        [Fact]
        public void ListServices_RejectsASchemaVersionTheLibraryWasNotWrittenFor()
        {
            using (var utility = FakeUtility.Returning(SampleDocuments.ServiceListWithWrongVersion))
            {
                var failure = Assert.Throws<SchemaVersionMismatchException>(() => utility.CreateClient().ListServices());

                Assert.Equal("ServiceList", failure.DocumentName);
                Assert.Equal(ServiceListResponse.ExpectedVersion, failure.ExpectedVersion);
                Assert.Equal(9999, failure.ActualVersion);
                Assert.Equal(1, failure.ActualMinorVersion);
                Assert.Contains("exportschemas", failure.Message);
                Assert.EndsWith("service list -format:xml", failure.CommandLine);
            }
        }

        [Fact]
        public void ListServices_CanBeToldToAcceptAnotherSchemaVersionAsAStopgap()
        {
            using (var utility = FakeUtility.Returning(SampleDocuments.ServiceListWithWrongVersion))
            {
                ServiceListResult result = utility
                    .CreateClient(options => options.ValidateSchemaVersions = false)
                    .ListServices();

                Assert.Empty(result.Services);
            }
        }

        [Fact]
        public void ListServices_ReportsAnUnexpectedErrorEvenWhenTheExitCodeSaysSuccess()
        {
            using (var utility = FakeUtility.Returning(SampleDocuments.ServiceListWithUnexpectedError, exitCode: 0))
            {
                var failure = Assert.Throws<UnexpectedErrorException>(() => utility.CreateClient().ListServices());

                Assert.Equal("The service operations controller is not reachable.", failure.ReportedError);
            }
        }

        [Fact]
        public void ListServices_ThrowsOnAFailingExitCodeAndCarriesWhatTheUtilitySaid()
        {
            using (var utility = FakeUtility.Returning(string.Empty, exitCode: 11, standardError: "Not authorized."))
            {
                var failure = Assert.Throws<CommandFailedException>(() => utility.CreateClient().ListServices());

                Assert.Equal(ServiceControlExitCode.AuthorizationFailed, failure.ExitCode);
                Assert.Equal(11, failure.RawExitCode);
                Assert.Equal("Not authorized.", failure.StandardError.Trim());
                Assert.Contains("AuthorizationFailed (11)", failure.Message);
            }
        }

        [Fact]
        public void ListServices_ExplainsWhenTheOutputIsNotXml()
        {
            using (var utility = FakeUtility.Returning("Usage: ScdSvcCtl <command> [switches]"))
            {
                var failure = Assert.Throws<ResponseParseException>(() => utility.CreateClient().ListServices());

                Assert.Equal("ServiceList", failure.DocumentName);
                Assert.Contains("Usage:", failure.StandardOutput);
                Assert.Contains("-format:xml", failure.Message);
            }
        }

        [Fact]
        public void ListAgents_ReadsTheAgentListDocument()
        {
            using (var utility = FakeUtility.Returning(SampleDocuments.AgentList))
            {
                AgentListResult result = utility.CreateClient().ListAgents();

                Assert.Equal(2, result.Agents.Count);
                Assert.Equal("APPSERVER01 (MainAgent)", result.Agents[0].ToString());
                Assert.False(result.HasWarnings);
            }
        }

        [Fact]
        public void ListQueueItems_ReadsTheQueueItemListDocument()
        {
            using (var utility = FakeUtility.Returning(SampleDocuments.QueueItemList))
            {
                QueueItemListResult result = utility.CreateClient().ListQueueItems();

                Assert.Equal(2, result.QueueItems.Count);
                Assert.Equal("q-100", result.QueueItems[0].QueueId);
                Assert.Equal(QueueItemActionType.Move, result.QueueItems[1].ActionType);
            }
        }

        [Fact]
        public void StartServices_ReturnsANegativeStateCodeInsteadOfThrowing()
        {
            // -11 is MaintenanceMode: an answer, not a failure, so it comes back to the caller.
            using (var utility = FakeUtility.Returning(string.Empty, exitCode: -11))
            {
                var request = new ServiceStartRequest { ServiceType = "Calculation service", Count = 2 };

                ServiceControlExitCode exitCode = utility.CreateClient().StartServices(request);

                Assert.Equal(ServiceControlExitCode.MaintenanceMode, exitCode);
                Assert.True(exitCode.IsStateInformation());
            }
        }

        [Fact]
        public void StopServices_RejectsAnEmptyRequestBeforeRunningAnything()
        {
            var client = new ServiceControlClient(@"C:\definitely\missing\ScdSvcCtl.exe");

            var failure = Assert.Throws<InvalidRequestException>(() => client.StopServices(new ServiceStopRequest()));

            Assert.Contains("by accident", failure.Message);
            Assert.Null(failure.CommandLine);
        }

        [Fact]
        public void CancelQueueItem_RequiresAQueueId()
        {
            var client = new ServiceControlClient(@"C:\definitely\missing\ScdSvcCtl.exe");

            Assert.Throws<InvalidRequestException>(() => client.CancelQueueItem("  "));
        }

        [Fact]
        public void GetUtilityVersion_ReturnsTheTrimmedBanner()
        {
            using (var utility = FakeUtility.Returning("ScdSvcCtl 7.5.0.1234\r\n\r\n"))
            {
                Assert.Equal("ScdSvcCtl 7.5.0.1234", utility.CreateClient().GetUtilityVersion());
            }
        }

        [Fact]
        public void RunCommand_CanBeAskedNotToThrowOnAFailingExitCode()
        {
            using (var utility = FakeUtility.Returning(string.Empty, exitCode: 24))
            {
                CommandResult result = utility.CreateClient().RunCommand(
                    new CommandArgumentBuilder("maintenance", "getmode"), throwOnFailure: false);

                Assert.Equal(ServiceControlExitCode.ServicesNotOnline, result.ExitCode);
            }
        }

        [Fact]
        public void AnyCommand_TimesOutWhenTheUtilityHangs()
        {
            using (var utility = FakeUtility.SleepingFor(15))
            {
                ServiceControlClient client = utility.CreateClient(options => options.CommandTimeout = TimeSpan.FromSeconds(1));

                Assert.Throws<CommandTimeoutException>(() => client.GetUtilityVersion());
            }
        }
    }
}
