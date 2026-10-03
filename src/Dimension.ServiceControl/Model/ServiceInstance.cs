using System.Collections.Generic;

namespace Dimension.ServiceControl.Model
{
    /// <summary>
    /// A service instance flattened together with the agent hosting it.
    /// </summary>
    /// <remarks>
    /// The utility nests services inside their agent, which is faithful to the platform but
    /// awkward to filter. This projection lifts the agent details onto each instance, so a
    /// single list can be sorted and filtered directly. It is what the PowerShell cmdlets
    /// emit, so Where-Object and Format-Table see a flat record.
    /// </remarks>
    public sealed class ServiceInstance
    {
        /// <summary>The name of the hosting agent, which is the server name.</summary>
        public string AgentName { get; set; }

        /// <summary>The description of the hosting agent.</summary>
        public string AgentDescription { get; set; }

        /// <summary>The tag on the hosting agent, when it has one.</summary>
        public string AgentTag { get; set; }

        /// <summary>The instance name.</summary>
        public string Instance { get; set; }

        /// <summary>
        /// The service type id. Absent from the plain text output.
        /// </summary>
        /// <remarks>
        /// This identifies the type, not the instance, so every instance of a type reports
        /// the same value. To address one instance use <see cref="FullName"/>.
        /// </remarks>
        public long Id { get; set; }

        /// <summary>The service type name.</summary>
        public string Type { get; set; }

        /// <summary>The management mode the instance runs under.</summary>
        public ManagementMode ManagementMode { get; set; }

        /// <summary>The lifecycle state of the instance.</summary>
        public ServiceState ServiceState { get; set; }

        /// <summary>What the instance is currently doing.</summary>
        public ServiceAction Action { get; set; }

        /// <summary>
        /// Whether the reported information is stale, meaning the controller has not heard
        /// from the instance recently and the state may no longer be accurate.
        /// </summary>
        public bool IsStale { get; set; }

        /// <summary>
        /// The configuration the instance runs with. Empty when the service type takes no
        /// configuration.
        /// </summary>
        public DatabaseElementReference Configuration { get; set; }

        /// <summary>The nickname assigned to the instance, or empty when it has none.</summary>
        public string Nickname { get; set; }

        /// <summary>
        /// The processes hosting the instance. Only filled in when the listing asked for
        /// processes to be included.
        /// </summary>
        public IReadOnlyList<HostProcess> Processes { get; set; }

        /// <summary>
        /// Whether the instance is serving, covering both the active and passive online
        /// states.
        /// </summary>
        /// <remarks>
        /// Inoperable is deliberately excluded. Such an instance is running but cannot serve
        /// requests, so counting it as online would hide a fault.
        /// </remarks>
        public bool IsOnline
        {
            get
            {
                return ServiceState == ServiceState.Online
                    || ServiceState == ServiceState.OnlinePassive;
            }
        }

        /// <summary>
        /// The instance name qualified by its agent, as "agent\instance". This is the
        /// handle "service stop" and "service explain" accept, not <see cref="Id"/>.
        /// </summary>
        public string FullName
        {
            get { return AgentName + "\\" + Instance; }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return FullName + " [" + ServiceState + "]";
        }
    }
}
