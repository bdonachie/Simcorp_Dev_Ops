using System.Xml.Serialization;

namespace Dimension.ServiceControl.Model
{
    /// <summary>
    /// The management mode a service instance runs under.
    /// Mirrors the ManagementModes type in ServiceList.xsd.
    /// </summary>
    public enum ManagementMode
    {
        /// <summary>Not specified. As a filter this matches every mode.</summary>
        Undefined,

        /// <summary>System service, managed by the platform itself.</summary>
        System,

        /// <summary>Started and stopped automatically by the automation model.</summary>
        Automated,

        /// <summary>Started and stopped manually by an operator.</summary>
        Manual,

        /// <summary>Managed outside of Dimension.</summary>
        External,

        /// <summary>Infrastructure service.</summary>
        Infrastructure,

        /// <summary>Transitioning between management modes.</summary>
        Transitional
    }

    /// <summary>
    /// The lifecycle state of a service instance.
    /// Mirrors the ServiceState type in ServiceList.xsd.
    /// </summary>
    public enum ServiceState
    {
        /// <summary>State could not be determined.</summary>
        Undefined,

        /// <summary>Initialising.</summary>
        Initialising,

        /// <summary>Starting.</summary>
        Starting,

        /// <summary>Online and active.</summary>
        Online,

        /// <summary>Online but passive, acting as a standby.</summary>
        OnlinePassive,

        /// <summary>Running but unable to serve requests.</summary>
        Inoperable,

        /// <summary>Finishing its current work.</summary>
        Finishing,

        /// <summary>Closing down.</summary>
        Closing,

        /// <summary>Closed.</summary>
        Closed,

        /// <summary>Failed.</summary>
        Failed
    }

    /// <summary>
    /// What a service instance is currently doing.
    /// Mirrors the Actions type in ServiceList.xsd.
    /// </summary>
    public enum ServiceAction
    {
        /// <summary>The instance does not disclose its current action.</summary>
        Undisclosed,

        /// <summary>Executing work.</summary>
        Executing,

        /// <summary>Preparing to execute work.</summary>
        Preparing,

        /// <summary>Idle.</summary>
        Idle
    }

    /// <summary>
    /// Whether an agent is a plain agent or the main agent on its server.
    /// Mirrors the AgentType element in AgentList.xsd.
    /// </summary>
    public enum AgentType
    {
        /// <summary>A regular service agent.</summary>
        Agent,

        /// <summary>The main service agent on the server.</summary>
        MainAgent
    }

    /// <summary>
    /// Severity of a warning log entry returned with a response.
    /// Mirrors the Log/Severity restriction shared by every exported schema.
    /// </summary>
    public enum LogSeverity
    {
        /// <summary>An error occurred.</summary>
        Error,

        /// <summary>A fatal error occurred.</summary>
        Fatal,

        /// <summary>Informational message.</summary>
        Info,

        /// <summary>A start event.</summary>
        Start,

        /// <summary>A status message.</summary>
        Status,

        /// <summary>A status message that is not persisted.</summary>
        StatusNonPersistent,

        /// <summary>A stop event.</summary>
        Stop,

        /// <summary>A warning.</summary>
        Warning
    }

    /// <summary>
    /// The kind of pending action a queue item represents.
    /// Mirrors the QueueitemTypes type in QueueitemList.xsd.
    /// </summary>
    public enum QueueItemActionType
    {
        /// <summary>A pending service start.</summary>
        Start,

        /// <summary>A pending service stop.</summary>
        Stop,

        /// <summary>A pending service move between agents.</summary>
        Move
    }

    /// <summary>
    /// The processing state of a queue item.
    /// Mirrors the QueueitemStates type in QueueitemList.xsd.
    /// </summary>
    public enum QueueItemState
    {
        /// <summary>Waiting to be processed.</summary>
        Waiting,

        /// <summary>Currently being processed.</summary>
        Processing
    }

    /// <summary>
    /// The management mode recorded against a queue item.
    /// Mirrors the ControllerManagementModes type in QueueitemList.xsd, which also
    /// allows a literal asterisk meaning "not specified".
    /// </summary>
    public enum ControllerManagementMode
    {
        /// <summary>Not specified. Written as an asterisk in the XML.</summary>
        [XmlEnum("*")]
        Any,

        /// <summary>Manual.</summary>
        Manual,

        /// <summary>Automated.</summary>
        Automated,

        /// <summary>Infrastructure.</summary>
        Infrastructure,

        /// <summary>Transitional.</summary>
        Transitional
    }

    /// <summary>
    /// Whether a queued start relates to an elastic service.
    /// Mirrors the ElasticTypes type in QueueitemList.xsd, which also allows an
    /// empty string meaning "not stated".
    /// </summary>
    public enum ElasticFlag
    {
        /// <summary>Not stated. Written as an empty string in the XML.</summary>
        [XmlEnum("")]
        Unspecified,

        /// <summary>Not elastic.</summary>
        No,

        /// <summary>Elastic.</summary>
        Yes
    }

    /// <summary>
    /// The component that requested a queued action.
    /// Mirrors the SourceTypes type in QueueitemList.xsd.
    /// </summary>
    public enum QueueItemSource
    {
        /// <summary>Unknown.</summary>
        Unknown,

        /// <summary>The Service Manager user interface.</summary>
        ServiceManager,

        /// <summary>The service control utility, ScdSvcCtl.exe.</summary>
        ServiceControlUtility,

        /// <summary>The dependency strategy.</summary>
        DependencyStrategy,

        /// <summary>The infrastructure strategy.</summary>
        InfrastructureStrategy,

        /// <summary>Dependency scaling.</summary>
        DependencyScaling,

        /// <summary>Rebalancing.</summary>
        Rebalancing,

        /// <summary>The automation service.</summary>
        AutomationService,

        /// <summary>The license checker.</summary>
        LicenseChecker
    }

    /// <summary>
    /// Why a queue item is still queued.
    /// Mirrors the QueueItemReasons type in QueueitemList.xsd.
    /// </summary>
    public enum QueueItemReason
    {
        /// <summary>The request was added to the queue.</summary>
        RequestAddedToQueue,

        /// <summary>No license is available.</summary>
        NoLicenseAvailable,

        /// <summary>No controller was found.</summary>
        NoControllerFound,

        /// <summary>The service instance is already starting.</summary>
        ServiceIsStarting,

        /// <summary>The service instance is already stopping.</summary>
        ServiceIsStopping,

        /// <summary>The controller is busy.</summary>
        ControllerBusy,

        /// <summary>The maximum number of running instances has been reached.</summary>
        MaxRunningReached,

        /// <summary>The maximum for the capability has been reached.</summary>
        MaxRunningReachedCapabilities,

        /// <summary>The maximum for the tag has been reached.</summary>
        MaxRunningReachedTagged,

        /// <summary>No controller was found for the tag.</summary>
        NoControllerFoundTagged,

        /// <summary>No controller was found for the capability.</summary>
        NoControllerFoundCapability,

        /// <summary>The service type is blocked.</summary>
        ServiceTypeBlocked,

        /// <summary>The service type is blacklisted.</summary>
        ServiceTypeBlacklisted,

        /// <summary>A service instance id could not be assigned.</summary>
        UnableToAssignServiceInstanceId,

        /// <summary>The service faulted and a new start attempt was scheduled.</summary>
        ServiceFaultedWithNewStartAttempt,

        /// <summary>The service instance is already started.</summary>
        ServiceIsStarted,

        /// <summary>Not enough available resources to start the service.</summary>
        NotEnoughAvailableResources
    }
}
