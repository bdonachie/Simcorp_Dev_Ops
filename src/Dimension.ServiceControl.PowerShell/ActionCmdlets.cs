using System.Collections.Generic;
using System.Management.Automation;
using Dimension.ServiceControl;
using Dimension.ServiceControl.Model;

namespace Dimension.ServiceControl.PowerShell
{
    /// <summary>
    /// Starts Dimension service instances.
    /// </summary>
    /// <example>
    ///   <code>Start-DimensionService -ServiceType 'Calculation service' -Count 2 -Wait</code>
    /// </example>
    [Cmdlet(VerbsLifecycle.Start, "DimensionService", SupportsShouldProcess = true)]
    [OutputType(typeof(ServiceControlExitCode))]
    public sealed class StartDimensionServiceCmdlet : DimensionCmdletBase
    {
        /// <summary>The service type to start.</summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string ServiceType { get; set; }

        /// <summary>The configuration id, for service types that take one.</summary>
        [Parameter]
        public string ServiceConfiguration { get; set; }

        /// <summary>The agent to start on. The platform picks one when not given.</summary>
        [Parameter]
        [Alias("Agent")]
        public string AgentName { get; set; }

        /// <summary>How many instances to start. Defaults to one.</summary>
        [Parameter]
        [ValidateRange(ServiceStartRequest.MinimumCount, ServiceStartRequest.MaximumCount)]
        public int? Count { get; set; }

        /// <summary>A nickname to assign to the started instances.</summary>
        [Parameter]
        public string Nickname { get; set; }

        /// <summary>Start in automated management mode rather than manual.</summary>
        [Parameter]
        public SwitchParameter Automated { get; set; }

        /// <summary>Wait until the instances have started before returning.</summary>
        [Parameter]
        public SwitchParameter Wait { get; set; }

        /// <summary>Return the exit code instead of writing nothing to the pipeline.</summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            string target = ServiceType + (Count.HasValue ? " x" + Count.Value : string.Empty);

            if (!ShouldProcess(target, "Start service"))
            {
                return;
            }

            var request = new ServiceStartRequest
            {
                ServiceType = ServiceType,
                ServiceConfiguration = ServiceConfiguration,
                AgentName = AgentName,
                Count = Count,
                Nickname = Nickname,
                StartAutomated = Automated.IsPresent ? true : (bool?)null,
                WaitForCompletion = Wait.IsPresent
            };

            try
            {
                ServiceControlExitCode exitCode = CreateClient().StartServices(request);

                if (PassThru.IsPresent)
                {
                    WriteObject(exitCode);
                }
            }
            catch (ServiceControlException failure)
            {
                WriteError(ToErrorRecord(failure));
            }
        }
    }

    /// <summary>
    /// Stops Dimension service instances, either the ones piped in or the ones matching
    /// the given filters.
    /// </summary>
    /// <remarks>
    /// Stopping is destructive, so this cmdlet prompts by default. Piping from
    /// Get-DimensionService is the safest route, because what will be stopped can be
    /// inspected first.
    /// </remarks>
    /// <example>
    ///   <code>Get-DimensionService -ServiceType 'Calculation service' | Stop-DimensionService -Wait</code>
    /// </example>
    [Cmdlet(VerbsLifecycle.Stop, "DimensionService",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High,
        DefaultParameterSetName = ByInstanceParameterSet)]
    [OutputType(typeof(ServiceControlExitCode))]
    public sealed class StopDimensionServiceCmdlet : DimensionCmdletBase
    {
        private const string ByInstanceParameterSet = "ByInstance";
        private const string ByFilterParameterSet = "ByFilter";

        private readonly List<string> collectedInstanceNames = new List<string>();

        /// <summary>
        /// Service instances to stop, normally piped in from Get-DimensionService.
        /// </summary>
        [Parameter(ParameterSetName = ByInstanceParameterSet, ValueFromPipeline = true)]
        public ServiceInstance[] InputObject { get; set; }

        /// <summary>
        /// Instance names to stop, each written as "agent\instance".
        /// </summary>
        [Parameter(ParameterSetName = ByInstanceParameterSet)]
        [Alias("Name")]
        public string[] ServiceInstanceName { get; set; }

        /// <summary>Stop only instances with this nickname.</summary>
        [Parameter(ParameterSetName = ByFilterParameterSet)]
        public string Nickname { get; set; }

        /// <summary>Stop only instances that have no nickname.</summary>
        [Parameter(ParameterSetName = ByFilterParameterSet)]
        public SwitchParameter WithoutNickname { get; set; }

        /// <summary>Stop only instances of this service type.</summary>
        [Parameter(ParameterSetName = ByFilterParameterSet)]
        public string ServiceType { get; set; }

        /// <summary>Stop only instances with this configuration. Needs ServiceType too.</summary>
        [Parameter(ParameterSetName = ByFilterParameterSet)]
        public string ServiceConfiguration { get; set; }

        /// <summary>Stop only instances with no configuration. Needs ServiceType too.</summary>
        [Parameter(ParameterSetName = ByFilterParameterSet)]
        public SwitchParameter WithoutConfiguration { get; set; }

        /// <summary>Stop only instances in this management mode.</summary>
        [Parameter(ParameterSetName = ByFilterParameterSet)]
        public ManagementMode ManagementMode { get; set; } = ManagementMode.Undefined;

        /// <summary>How many instances to stop.</summary>
        [Parameter]
        public int? Count { get; set; }

        /// <summary>Wait until the instances have stopped before returning.</summary>
        [Parameter]
        public SwitchParameter Wait { get; set; }

        /// <summary>Return the exit code instead of writing nothing to the pipeline.</summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            // Instances arrive one pipeline object at a time, so they are gathered here and
            // stopped together in EndProcessing. That way the utility is run once and the
            // confirmation prompt names everything being stopped.
            if (InputObject != null)
            {
                foreach (ServiceInstance instance in InputObject)
                {
                    collectedInstanceNames.Add(instance.FullName);
                }
            }

            if (ServiceInstanceName != null)
            {
                collectedInstanceNames.AddRange(ServiceInstanceName);
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing()
        {
            var request = new ServiceStopRequest
            {
                Count = Count,
                WaitForCompletion = Wait.IsPresent
            };

            string target;

            if (ParameterSetName == ByFilterParameterSet)
            {
                request.Nickname = Nickname;
                request.WithoutNickname = WithoutNickname.IsPresent;
                request.ServiceType = ServiceType;
                request.ServiceConfiguration = ServiceConfiguration;
                request.WithoutConfiguration = WithoutConfiguration.IsPresent;
                request.ManagementMode = ManagementMode;

                target = "services matching the given filters";
            }
            else
            {
                if (collectedInstanceNames.Count == 0)
                {
                    // An empty pipeline is a legitimate no-op: the upstream filter simply
                    // matched nothing. A bare command with no arguments is a mistake, and
                    // saying so beats exiting quietly as though work had been done.
                    if (MyInvocation.ExpectingInput)
                    {
                        WriteVerbose("No service instances were piped in, so nothing was stopped.");
                        return;
                    }

                    WriteError(ToErrorRecord(new InvalidRequestException(
                        "Nothing was specified to stop. Pipe in service instances from "
                        + "Get-DimensionService, pass -ServiceInstanceName, or use the filter "
                        + "parameters such as -ServiceType.")));
                    return;
                }

                request.ServiceInstanceNames = collectedInstanceNames;

                target = string.Join(", ", collectedInstanceNames);
            }

            if (!ShouldProcess(target, "Stop service"))
            {
                return;
            }

            try
            {
                ServiceControlExitCode exitCode = CreateClient().StopServices(request);

                if (PassThru.IsPresent)
                {
                    WriteObject(exitCode);
                }
            }
            catch (ServiceControlException failure)
            {
                WriteError(ToErrorRecord(failure));
            }
        }
    }

    /// <summary>
    /// Cancels a queued Dimension action.
    /// </summary>
    /// <example>
    ///   <code>Get-DimensionQueueItem | Where-Object ActionState -eq Waiting | Remove-DimensionQueueItem</code>
    /// </example>
    [Cmdlet(VerbsCommon.Remove, "DimensionQueueItem",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(ServiceControlExitCode))]
    public sealed class RemoveDimensionQueueItemCmdlet : DimensionCmdletBase
    {
        /// <summary>
        /// The queue id to cancel. Accepts the QueueId property from Get-DimensionQueueItem.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string QueueId { get; set; }

        /// <summary>Return the exit code instead of writing nothing to the pipeline.</summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            if (!ShouldProcess(QueueId, "Cancel queued action"))
            {
                return;
            }

            try
            {
                ServiceControlExitCode exitCode = CreateClient().CancelQueueItem(QueueId);

                if (PassThru.IsPresent)
                {
                    WriteObject(exitCode);
                }
            }
            catch (ServiceControlException failure)
            {
                WriteError(ToErrorRecord(failure));
            }
        }
    }
}
