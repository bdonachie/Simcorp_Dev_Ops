using System.Collections.Generic;
using System.Xml.Serialization;

namespace Dimension.ServiceControl.Model
{
    /// <summary>
    /// The document produced by "ScdSvcCtl.exe service list -format:xml".
    /// Mirrors ServiceList.xsd.
    /// </summary>
    [XmlRoot("ServiceList")]
    public sealed class ServiceListResponse : IVersionedResponse
    {
        /// <summary>The schema major version this library was written against.</summary>
        public const int ExpectedVersion = 2307;

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

        /// <summary>The agents, each carrying the service instances it hosts.</summary>
        [XmlArray("Agents")]
        [XmlArrayItem("Agent")]
        public List<ServiceListAgent> Agents { get; set; } = new List<ServiceListAgent>();

        /// <inheritdoc />
        [XmlElement("Warnings")]
        public ResponseWarnings Warnings { get; set; }
    }

    /// <summary>
    /// An agent in a service listing, with the service instances hosted on it.
    /// Mirrors the Agent element in ServiceList.xsd.
    /// </summary>
    public sealed class ServiceListAgent
    {
        /// <summary>The agent name, which is the hosting server name.</summary>
        [XmlElement("Name")]
        public string Name { get; set; }

        /// <summary>The agent description.</summary>
        [XmlElement("Description")]
        public string Description { get; set; }

        /// <summary>The tag assigned to the agent, when it has one.</summary>
        [XmlElement("Tag")]
        public string Tag { get; set; }

        /// <summary>The service instances hosted on this agent.</summary>
        [XmlArray("Services")]
        [XmlArrayItem("Service")]
        public List<ServiceListEntry> Services { get; set; } = new List<ServiceListEntry>();

        /// <inheritdoc />
        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// One service instance in a service listing.
    /// Mirrors the Service element in ServiceList.xsd.
    /// </summary>
    public sealed class ServiceListEntry
    {
        /// <summary>The instance name.</summary>
        [XmlElement("Instance")]
        public string Instance { get; set; }

        /// <summary>The management mode the instance runs under.</summary>
        [XmlElement("ManagementMode")]
        public ManagementMode ManagementMode { get; set; }

        /// <summary>The lifecycle state of the instance.</summary>
        [XmlElement("ServiceState")]
        public ServiceState ServiceState { get; set; }

        /// <summary>
        /// Whether the reported information is stale, meaning the controller has not heard
        /// from the instance recently and the state may no longer be accurate.
        /// </summary>
        [XmlElement("Stale")]
        public bool IsStale { get; set; }

        /// <summary>What the instance is currently doing.</summary>
        [XmlElement("Action")]
        public ServiceAction Action { get; set; }

        /// <summary>
        /// The service type id. Absent from the plain text output.
        /// </summary>
        /// <remarks>
        /// This identifies the type, not the instance, so every instance of a type reports
        /// the same value and the instance name usually embeds it, as in "215-1". It is not
        /// the handle the commands take: "service stop" and "service explain" want the
        /// agent\instance form.
        /// </remarks>
        [XmlElement("Id")]
        public long Id { get; set; }

        /// <summary>The service type name.</summary>
        [XmlElement("Type")]
        public string Type { get; set; }

        /// <summary>
        /// The configuration the instance runs with. Empty when the service type takes
        /// no configuration.
        /// </summary>
        [XmlElement("Configuration")]
        public DatabaseElementReference Configuration { get; set; }

        /// <summary>The nickname assigned to the instance, or empty when it has none.</summary>
        [XmlElement("Nickname")]
        public string Nickname { get; set; }

        /// <summary>
        /// The processes hosting the instance. Only filled in when the listing asked for
        /// processes to be included.
        /// </summary>
        [XmlArray("Processes")]
        [XmlArrayItem("Process")]
        public List<HostProcess> Processes { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Instance + " [" + ServiceState + "]";
        }
    }
}
