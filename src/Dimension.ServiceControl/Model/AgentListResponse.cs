using System.Collections.Generic;
using System.Xml.Serialization;

namespace Dimension.ServiceControl.Model
{
    /// <summary>
    /// The document produced by "ScdSvcCtl.exe agent list -format:xml".
    /// Mirrors AgentList.xsd.
    /// </summary>
    [XmlRoot("AgentList")]
    public sealed class AgentListResponse : IVersionedResponse
    {
        /// <summary>The schema major version this library was written against.</summary>
        public const int ExpectedVersion = 641;

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

        /// <summary>The agents currently running.</summary>
        [XmlArray("RunningAgents")]
        [XmlArrayItem("Agent")]
        public List<AgentEntry> RunningAgents { get; set; } = new List<AgentEntry>();

        /// <inheritdoc />
        [XmlElement("Warnings")]
        public ResponseWarnings Warnings { get; set; }
    }

    /// <summary>
    /// A running agent and the resource usage it reports.
    /// Mirrors the Agent element in AgentList.xsd.
    /// </summary>
    /// <remarks>
    /// The resource figures are optional in the schema, so they are nullable here. A null
    /// means the agent did not report the figure, which is not the same as reporting zero.
    /// </remarks>
    public sealed class AgentEntry
    {
        /// <summary>The agent name, which is the hosting server name.</summary>
        [XmlElement("Name")]
        public string Name { get; set; }

        /// <summary>Whether this is a regular agent or the main agent on the server.</summary>
        [XmlElement("AgentType")]
        public AgentType AgentType { get; set; }

        /// <summary>The agent description.</summary>
        [XmlElement("Description")]
        public string Description { get; set; }

        /// <summary>The tag assigned to the agent, when it has one.</summary>
        [XmlElement("Tag")]
        public string Tag { get; set; }

        /// <summary>The reported load index, or null when not reported.</summary>
        [XmlElement("LoadIndex")]
        public long? LoadIndex { get; set; }

        /// <summary>The reported CPU utilisation, or null when not reported.</summary>
        [XmlElement("CpuUtilization")]
        public long? CpuUtilization { get; set; }

        /// <summary>The reported available memory, or null when not reported.</summary>
        [XmlElement("AvailableMemory")]
        public long? AvailableMemory { get; set; }

        /// <summary>The number of service instances on this agent, or null when not reported.</summary>
        [XmlElement("ServiceCountOnAgent")]
        public long? ServiceCountOnAgent { get; set; }

        /// <summary>
        /// The number of service instances across every agent on the same server, or null
        /// when not reported.
        /// </summary>
        [XmlElement("ServiceCountOnServer")]
        public long? ServiceCountOnServer { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name + " (" + AgentType + ")";
        }
    }
}
