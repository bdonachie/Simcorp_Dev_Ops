using System.Management.Automation;
using Dimension.ServiceControl;
using Dimension.ServiceControl.Model;

namespace Dimension.ServiceControl.PowerShell
{
    /// <summary>
    /// Lists Dimension service instances that are not closed.
    /// </summary>
    /// <example>
    ///   <code>Get-DimensionService -Online | Where-Object Type -like '*Calculation*'</code>
    /// </example>
    [Cmdlet(VerbsCommon.Get, "DimensionService")]
    [OutputType(typeof(ServiceInstance))]
    public sealed class GetDimensionServiceCmdlet : DimensionCmdletBase
    {
        /// <summary>Only service instances hosted on this agent.</summary>
        [Parameter(Position = 0)]
        [Alias("Agent")]
        public string AgentName { get; set; }

        /// <summary>Only service instances with this nickname. Not case sensitive.</summary>
        [Parameter]
        public string Nickname { get; set; }

        /// <summary>Only service instances that have no nickname.</summary>
        [Parameter]
        public SwitchParameter WithoutNickname { get; set; }

        /// <summary>Only service instances of this service type.</summary>
        [Parameter]
        public string ServiceType { get; set; }

        /// <summary>Only service instances with this configuration. Needs ServiceType too.</summary>
        [Parameter]
        public string ServiceConfiguration { get; set; }

        /// <summary>Only service instances with no configuration. Needs ServiceType too.</summary>
        [Parameter]
        public SwitchParameter WithoutConfiguration { get; set; }

        /// <summary>Only service instances in this management mode.</summary>
        [Parameter]
        public ManagementMode ManagementMode { get; set; } = ManagementMode.Undefined;

        /// <summary>Only online service instances.</summary>
        [Parameter]
        public SwitchParameter Online { get; set; }

        /// <summary>Only failed service instances.</summary>
        [Parameter]
        public SwitchParameter Failed { get; set; }

        /// <summary>
        /// Match on stale information. Leave unset for no stale filter, or pass
        /// -Stale $true / -Stale $false to match one or the other.
        /// </summary>
        [Parameter]
        public bool? Stale { get; set; }

        /// <summary>Include the hosting processes of each instance.</summary>
        [Parameter]
        public SwitchParameter IncludeProcesses { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            var filter = new ServiceListFilter
            {
                AgentName = AgentName,
                Nickname = Nickname,
                WithoutNickname = WithoutNickname.IsPresent,
                ServiceType = ServiceType,
                ServiceConfiguration = ServiceConfiguration,
                WithoutConfiguration = WithoutConfiguration.IsPresent,
                ManagementMode = ManagementMode,
                OnlineOnly = Online.IsPresent,
                FailedOnly = Failed.IsPresent,
                IsStale = Stale,
                IncludeProcesses = IncludeProcesses.IsPresent
            };

            try
            {
                ServiceListResult result = CreateClient().ListServices(filter);

                WriteListWarnings(result);
                WriteObject(result.Services, enumerateCollection: true);
            }
            catch (ServiceControlException failure)
            {
                WriteError(ToErrorRecord(failure));
            }
        }
    }

    /// <summary>
    /// Lists the Dimension service agents currently running.
    /// </summary>
    /// <example>
    ///   <code>Get-DimensionAgent | Sort-Object LoadIndex -Descending</code>
    /// </example>
    [Cmdlet(VerbsCommon.Get, "DimensionAgent")]
    [OutputType(typeof(AgentEntry))]
    public sealed class GetDimensionAgentCmdlet : DimensionCmdletBase
    {
        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            try
            {
                AgentListResult result = CreateClient().ListAgents();

                WriteListWarnings(result);
                WriteObject(result.Agents, enumerateCollection: true);
            }
            catch (ServiceControlException failure)
            {
                WriteError(ToErrorRecord(failure));
            }
        }
    }

    /// <summary>
    /// Lists the start, stop and move actions the controller has accepted but not finished.
    /// </summary>
    /// <remarks>
    /// Useful for watching an asynchronous start or stop. An action leaving this list means
    /// the controller is done with it; whether it worked is read from Get-DimensionService.
    /// </remarks>
    /// <example>
    ///   <code>Get-DimensionQueueItem | Format-Table QueueId,ActionType,ActionState,Reason</code>
    /// </example>
    [Cmdlet(VerbsCommon.Get, "DimensionQueueItem")]
    [OutputType(typeof(QueueItemEntry))]
    public sealed class GetDimensionQueueItemCmdlet : DimensionCmdletBase
    {
        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            try
            {
                QueueItemListResult result = CreateClient().ListQueueItems();

                WriteListWarnings(result);
                WriteObject(result.QueueItems, enumerateCollection: true);
            }
            catch (ServiceControlException failure)
            {
                WriteError(ToErrorRecord(failure));
            }
        }
    }
}
