using System;
using System.IO;
using System.Xml.Serialization;
using Dimension.ServiceControl.Model;
using Xunit;

namespace Dimension.ServiceControl.Tests
{
    /// <summary>
    /// Reads the sample documents through the same serializer the client uses, so a model
    /// that drifts from the schema fails here rather than against a live platform.
    /// </summary>
    public class ResponseModelTests
    {
        private static T Read<T>(string xml)
        {
            var serializer = new XmlSerializer(typeof(T));

            using (var reader = new StringReader(xml))
            {
                return (T)serializer.Deserialize(reader);
            }
        }

        [Fact]
        public void ServiceList_ReadsTheVersionAttributesAndNestedAgents()
        {
            ServiceListResponse response = Read<ServiceListResponse>(SampleDocuments.ServiceList);

            Assert.Equal(ServiceListResponse.ExpectedVersion, response.Version);
            Assert.Equal(ServiceListResponse.ExpectedMinorVersion, response.MinorVersion);
            Assert.Null(response.UnexpectedError);
            Assert.Equal("Default", response.ServiceSegment);
            Assert.Equal(2, response.Agents.Count);
            Assert.Equal("APPSERVER01", response.Agents[0].Name);
            Assert.Equal("blue", response.Agents[0].Tag);
            Assert.Equal(2, response.Agents[0].Services.Count);
            Assert.Empty(response.Agents[1].Services);
        }

        [Fact]
        public void ServiceList_ReadsEveryFieldOfAService()
        {
            ServiceListEntry service = Read<ServiceListResponse>(SampleDocuments.ServiceList).Agents[0].Services[0];

            Assert.Equal("215-1", service.Instance);
            Assert.Equal(ManagementMode.Automated, service.ManagementMode);
            Assert.Equal(ServiceState.Online, service.ServiceState);
            Assert.False(service.IsStale);
            Assert.Equal(ServiceAction.Idle, service.Action);
            Assert.Equal(215, service.Id);
            Assert.Equal("Calculation service", service.Type);
            Assert.Equal(7, service.Configuration.Id);
            Assert.Equal("Default (7)", service.Configuration.ToString());
            Assert.Equal("calc-a", service.Nickname);
            Assert.Single(service.Processes);
            Assert.Equal("4120", service.Processes[0].ProcessId);
        }

        [Fact]
        public void ServiceList_TreatsAnEmptyConfigurationElementAsNoConfiguration()
        {
            ServiceListEntry service = Read<ServiceListResponse>(SampleDocuments.ServiceList).Agents[0].Services[1];

            Assert.NotNull(service.Configuration);
            Assert.True(service.Configuration.IsEmpty);
            Assert.Equal(string.Empty, service.Configuration.ToString());
            Assert.True(service.IsStale);
            Assert.Equal(ServiceState.Inoperable, service.ServiceState);
            // No <Processes> element: XmlSerializer still supplies an empty list, so callers
            // can enumerate it without a null check.
            Assert.NotNull(service.Processes);
            Assert.Empty(service.Processes);
        }

        [Fact]
        public void ServiceList_FlattensWarningLogsAndValidationsIntoOneList()
        {
            ServiceListResponse response = Read<ServiceListResponse>(SampleDocuments.ServiceList);

            Assert.Equal(
                new[] { "Warning: Agent APPSERVER02 did not answer.", "Listing may be incomplete." },
                response.Warnings.AllMessages);
        }

        [Fact]
        public void ServiceList_ReadsAnUnexpectedErrorInPlaceOfThePayload()
        {
            ServiceListResponse response = Read<ServiceListResponse>(SampleDocuments.ServiceListWithUnexpectedError);

            Assert.Equal("The service operations controller is not reachable.", response.UnexpectedError);
            Assert.Empty(response.Agents);
        }

        [Fact]
        public void AgentList_LeavesUnreportedResourceFiguresNullRatherThanZero()
        {
            AgentListResponse response = Read<AgentListResponse>(SampleDocuments.AgentList);

            Assert.Equal(AgentListResponse.ExpectedVersion, response.Version);
            Assert.Equal(2, response.RunningAgents.Count);

            AgentEntry main = response.RunningAgents[0];
            Assert.Equal(AgentType.MainAgent, main.AgentType);
            Assert.Equal(3, main.LoadIndex);
            Assert.Equal(8192, main.AvailableMemory);
            Assert.Null(main.CpuUtilization);

            AgentEntry secondary = response.RunningAgents[1];
            Assert.Equal(AgentType.Agent, secondary.AgentType);
            Assert.Null(secondary.Tag);
            Assert.Null(secondary.LoadIndex);
        }

        [Fact]
        public void QueueItemList_ReadsTheAsteriskWildcardsAndAnEmptyElasticElement()
        {
            QueueItemEntry item = Read<QueueItemListResponse>(SampleDocuments.QueueItemList).QueueItems[0];

            Assert.Equal("q-100", item.QueueId);
            Assert.Equal(QueueItemActionType.Start, item.ActionType);
            Assert.Equal(QueueItemState.Waiting, item.ActionState);
            Assert.True(item.Service.IsWildcard);
            Assert.True(item.Service.Configuration.IsWildcard);
            Assert.Equal(ControllerManagementMode.Any, item.ManagementMode);
            Assert.Equal(ElasticFlag.Unspecified, item.Elastic);
            Assert.Equal(new DateTime(2026, 9, 29, 10, 15, 0), item.QueuedAt);
            Assert.Equal(QueueItemSource.ServiceControlUtility, item.Source);
            Assert.Equal(QueueItemReason.ControllerBusy, item.Reason);
            Assert.Equal("Start Calculation service [Waiting: ControllerBusy]", item.ToString());
        }

        [Fact]
        public void QueueItemList_ReadsAMoveAndLeavesAMissingElasticNull()
        {
            QueueItemEntry item = Read<QueueItemListResponse>(SampleDocuments.QueueItemList).QueueItems[1];

            Assert.Equal(QueueItemActionType.Move, item.ActionType);
            Assert.Equal("215", item.Service.Id);
            Assert.False(item.Service.IsWildcard);
            Assert.Equal("215-1", item.Service.InstanceFrom);
            Assert.Equal("215-2", item.Service.InstanceTo);
            Assert.Equal("APPSERVER01", item.AgentFrom);
            Assert.Equal("APPSERVER02", item.AgentTo);
            Assert.Equal(ControllerManagementMode.Automated, item.ManagementMode);
            Assert.Null(item.Elastic);
            Assert.Equal(DateTimeKind.Utc, item.QueuedAt.Kind);
            Assert.Equal(QueueItemSource.Rebalancing, item.Source);
        }
    }
}
