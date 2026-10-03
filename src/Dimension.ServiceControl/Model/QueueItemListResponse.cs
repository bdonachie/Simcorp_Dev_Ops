using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Dimension.ServiceControl.Model
{
    /// <summary>
    /// The document produced by "ScdSvcCtl.exe queueitem list -format:xml".
    /// Mirrors QueueitemList.xsd.
    /// </summary>
    /// <remarks>
    /// Queue items are the start, stop and move actions the controller has accepted but not
    /// yet completed. Polling this listing is how an asynchronous start or stop is followed
    /// to completion without blocking on the utility's own wait switch.
    /// </remarks>
    [XmlRoot("QueueitemList")]
    public sealed class QueueItemListResponse : IVersionedResponse
    {
        /// <summary>The schema major version this library was written against.</summary>
        public const int ExpectedVersion = 2201;

        /// <summary>The schema minor version this library was written against.</summary>
        public const int ExpectedMinorVersion = 0;

        /// <inheritdoc />
        [XmlAttribute("Version")]
        public int Version { get; set; }

        /// <inheritdoc />
        [XmlAttribute("MinorVersion")]
        public int MinorVersion { get; set; }

        /// <inheritdoc />
        [XmlElement("UnexpectedError")]
        public string UnexpectedError { get; set; }

        /// <summary>The service segment the listing came from, when reported.</summary>
        [XmlElement("ServiceSegment")]
        public string ServiceSegment { get; set; }

        /// <summary>The directory services used to produce the listing, when reported.</summary>
        [XmlElement("DirectoryServices")]
        public string DirectoryServices { get; set; }

        /// <summary>The pending actions currently queued.</summary>
        [XmlArray("QueueItems")]
        [XmlArrayItem("QueueItem")]
        public List<QueueItemEntry> QueueItems { get; set; } = new List<QueueItemEntry>();

        /// <inheritdoc />
        [XmlElement("Warnings")]
        public ResponseWarnings Warnings { get; set; }
    }

    /// <summary>
    /// A single pending action.
    /// Mirrors the QueueItem element in QueueitemList.xsd.
    /// </summary>
    public sealed class QueueItemEntry
    {
        /// <summary>The queue id, which is the handle accepted by "queueitem cancel".</summary>
        [XmlElement("QueueId")]
        public string QueueId { get; set; }

        /// <summary>Whether this is a start, a stop or a move.</summary>
        [XmlElement("ActionType")]
        public QueueItemActionType ActionType { get; set; }

        /// <summary>Whether the item is waiting or being processed.</summary>
        [XmlElement("ActionState")]
        public QueueItemState ActionState { get; set; }

        /// <summary>The service the action applies to.</summary>
        [XmlElement("Service")]
        public QueueItemService Service { get; set; }

        /// <summary>The agent the action applies to, when one is recorded.</summary>
        [XmlElement("Agent")]
        public string Agent { get; set; }

        /// <summary>For a move, the agent being moved from.</summary>
        [XmlElement("AgentFrom")]
        public string AgentFrom { get; set; }

        /// <summary>For a move, the agent being moved to.</summary>
        [XmlElement("AgentTo")]
        public string AgentTo { get; set; }

        /// <summary>The management mode recorded against the action.</summary>
        [XmlElement("ManagementMode")]
        public ControllerManagementMode ManagementMode { get; set; }

        /// <summary>
        /// Whether the queued start relates to an elastic service, or null when the
        /// utility did not state it.
        /// </summary>
        [XmlElement("Elastic")]
        public ElasticFlag? Elastic { get; set; }

        /// <summary>When the item was placed on the queue.</summary>
        [XmlElement("QueuedAt")]
        public DateTime QueuedAt { get; set; }

        /// <summary>When the action was originally requested.</summary>
        [XmlElement("RequestedAt")]
        public DateTime RequestedAt { get; set; }

        /// <summary>The component that requested the action.</summary>
        [XmlElement("Source")]
        public QueueItemSource Source { get; set; }

        /// <summary>The tag recorded against the action.</summary>
        [XmlElement("Tag")]
        public string Tag { get; set; }

        /// <summary>Why the item is still queued.</summary>
        [XmlElement("Reason")]
        public QueueItemReason Reason { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            string serviceName = Service == null ? string.Empty : Service.Name;
            return ActionType + " " + serviceName + " [" + ActionState + ": " + Reason + "]";
        }
    }

    /// <summary>
    /// The service a queue item applies to.
    /// Mirrors the Service element nested inside QueueItem in QueueitemList.xsd.
    /// </summary>
    public sealed class QueueItemService
    {
        /// <summary>
        /// The service id. Kept as text because the schema allows an asterisk here,
        /// meaning the action is not tied to one specific instance.
        /// </summary>
        [XmlElement("Id")]
        public string Id { get; set; }

        /// <summary>The service type name.</summary>
        [XmlElement("Name")]
        public string Name { get; set; }

        /// <summary>The instance name, when the action targets a specific instance.</summary>
        [XmlElement("Instance")]
        public string Instance { get; set; }

        /// <summary>For a move, the instance being moved from.</summary>
        [XmlElement("InstanceFrom")]
        public string InstanceFrom { get; set; }

        /// <summary>For a move, the instance being moved to.</summary>
        [XmlElement("InstanceTo")]
        public string InstanceTo { get; set; }

        /// <summary>The configuration the action applies to.</summary>
        [XmlElement("Configuration")]
        public QueueItemDatabaseElementReference Configuration { get; set; }

        /// <summary>The nickname recorded against the action.</summary>
        [XmlElement("Nickname")]
        public string Nickname { get; set; }

        /// <summary>Whether the id is the asterisk wildcard rather than a specific instance.</summary>
        [XmlIgnore]
        public bool IsWildcard
        {
            get { return Id == "*"; }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// A database element reference inside a queue item.
    /// Mirrors the DatabaseElementType type in QueueitemList.xsd.
    /// </summary>
    /// <remarks>
    /// Deliberately separate from <see cref="DatabaseElementReference"/>. The queue item
    /// schema widens the id to a string so it can carry an asterisk for "not specified",
    /// while the service list schema keeps it numeric.
    /// </remarks>
    public sealed class QueueItemDatabaseElementReference
    {
        /// <summary>The id, either a number or an asterisk.</summary>
        [XmlElement("Id")]
        public string Id { get; set; }

        /// <summary>The name, or null when not supplied.</summary>
        [XmlElement("Name")]
        public string Name { get; set; }

        /// <summary>The description, or null when not supplied.</summary>
        [XmlElement("Description")]
        public string Description { get; set; }

        /// <summary>Whether the id is the asterisk wildcard.</summary>
        [XmlIgnore]
        public bool IsWildcard
        {
            get { return Id == "*"; }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name;
        }
    }
}
