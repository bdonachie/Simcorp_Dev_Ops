using System.Collections.Generic;
using System.Xml.Serialization;

namespace Dimension.ServiceControl.Model
{
    /// <summary>
    /// Implemented by every XML response returned by ScdSvcCtl.exe.
    /// </summary>
    /// <remarks>
    /// Each schema pins its root element to a fixed version, and allows the whole payload
    /// to be replaced by a single UnexpectedError element. Exposing both through one
    /// interface lets the client check the contract in a single place.
    /// </remarks>
    public interface IVersionedResponse
    {
        /// <summary>The schema major version declared by the response.</summary>
        int Version { get; }

        /// <summary>The schema minor version declared by the response.</summary>
        int MinorVersion { get; }

        /// <summary>
        /// Set instead of the payload when the utility hit an unexpected error.
        /// Null on a normal response.
        /// </summary>
        string UnexpectedError { get; }

        /// <summary>Non-fatal warnings raised while producing the response, if any.</summary>
        ResponseWarnings Warnings { get; }
    }

    /// <summary>
    /// A single warning log entry returned with a response.
    /// </summary>
    public sealed class LogEntry
    {
        /// <summary>Severity of the entry.</summary>
        [XmlElement("Severity")]
        public LogSeverity Severity { get; set; }

        /// <summary>The message text.</summary>
        [XmlElement("Message")]
        public string Message { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Severity + ": " + Message;
        }
    }

    /// <summary>
    /// The log entries inside a warnings block.
    /// </summary>
    public sealed class WarningLogs
    {
        /// <summary>The individual entries.</summary>
        [XmlElement("Log")]
        public List<LogEntry> Entries { get; set; } = new List<LogEntry>();
    }

    /// <summary>
    /// The validation messages inside a warnings block.
    /// </summary>
    public sealed class WarningValidations
    {
        /// <summary>The individual messages.</summary>
        [XmlElement("Message")]
        public List<string> Messages { get; set; } = new List<string>();
    }

    /// <summary>
    /// Warnings attached to a response.
    /// </summary>
    /// <remarks>
    /// Warnings are not fatal, but they routinely explain partial results, such as an agent
    /// that could not be reached and whose services are therefore missing from a listing.
    /// They are surfaced rather than discarded so a short listing is never mistaken for a
    /// complete one.
    /// </remarks>
    public sealed class ResponseWarnings
    {
        /// <summary>Log entries raised while producing the response.</summary>
        [XmlElement("Logs")]
        public WarningLogs Logs { get; set; }

        /// <summary>Validation messages raised while producing the response.</summary>
        [XmlElement("Validations")]
        public WarningValidations Validations { get; set; }

        /// <summary>Every warning flattened into one list of readable strings.</summary>
        [XmlIgnore]
        public IReadOnlyList<string> AllMessages
        {
            get
            {
                var messages = new List<string>();

                if (Logs != null && Logs.Entries != null)
                {
                    foreach (LogEntry entry in Logs.Entries)
                    {
                        messages.Add(entry.ToString());
                    }
                }

                if (Validations != null && Validations.Messages != null)
                {
                    messages.AddRange(Validations.Messages);
                }

                return messages;
            }
        }
    }

    /// <summary>
    /// A reference to a Dimension database element, carrying its id, name and description.
    /// Mirrors the DatabaseElementType type in ServiceList.xsd.
    /// </summary>
    /// <remarks>
    /// Every part is optional. A service instance with no configuration is written as an
    /// empty Configuration element, which reads back as an instance where
    /// <see cref="IsEmpty"/> is true.
    /// </remarks>
    public sealed class DatabaseElementReference
    {
        /// <summary>The numeric id, or null when the element carries no id.</summary>
        [XmlElement("Id")]
        public long? Id { get; set; }

        /// <summary>The name, or null when not supplied.</summary>
        [XmlElement("Name")]
        public string Name { get; set; }

        /// <summary>The description, or null when not supplied.</summary>
        [XmlElement("Description")]
        public string Description { get; set; }

        /// <summary>Whether this reference carries no information at all.</summary>
        [XmlIgnore]
        public bool IsEmpty
        {
            get { return Id == null && string.IsNullOrEmpty(Name); }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (IsEmpty)
            {
                return string.Empty;
            }

            return Id == null ? Name : Name + " (" + Id + ")";
        }
    }

    /// <summary>
    /// An operating system process hosting a service instance.
    /// Mirrors the Process element in ServiceList.xsd.
    /// </summary>
    public sealed class HostProcess
    {
        /// <summary>The process name.</summary>
        [XmlElement("Name")]
        public string Name { get; set; }

        /// <summary>
        /// The process id as reported by the agent. Kept as text because the schema
        /// declares it as a string rather than a number.
        /// </summary>
        [XmlElement("Id")]
        public string ProcessId { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name + " (" + ProcessId + ")";
        }
    }
}
