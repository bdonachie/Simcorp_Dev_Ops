using System.Collections.Generic;

namespace Dimension.ServiceControl.Model
{
    /// <summary>
    /// The parts every listing returns alongside its payload.
    /// </summary>
    public abstract class ListResult
    {
        /// <summary>The service segment the listing came from, when reported.</summary>
        public string ServiceSegment { get; set; }

        /// <summary>The directory services used to produce the listing, when reported.</summary>
        public string DirectoryServices { get; set; }

        /// <summary>
        /// Non-fatal warnings raised while producing the listing. Worth checking before
        /// treating a short listing as complete, since an unreachable agent is reported
        /// here rather than as a failure.
        /// </summary>
        public IReadOnlyList<string> Warnings { get; set; } = new List<string>();

        /// <summary>Whether the listing came back with any warnings attached.</summary>
        public bool HasWarnings
        {
            get { return Warnings != null && Warnings.Count > 0; }
        }
    }

    /// <summary>
    /// The result of listing service instances.
    /// </summary>
    public sealed class ServiceListResult : ListResult
    {
        /// <summary>
        /// Every service instance, flattened so each one carries its agent details.
        /// This is the list most callers want.
        /// </summary>
        public IReadOnlyList<ServiceInstance> Services { get; set; } = new List<ServiceInstance>();

        /// <summary>
        /// The same services in the nested shape the utility reported them in, kept for
        /// callers that need to work agent by agent.
        /// </summary>
        public IReadOnlyList<ServiceListAgent> Agents { get; set; } = new List<ServiceListAgent>();
    }

    /// <summary>
    /// The result of listing agents.
    /// </summary>
    public sealed class AgentListResult : ListResult
    {
        /// <summary>The agents currently running.</summary>
        public IReadOnlyList<AgentEntry> Agents { get; set; } = new List<AgentEntry>();
    }

    /// <summary>
    /// The result of listing queued actions.
    /// </summary>
    public sealed class QueueItemListResult : ListResult
    {
        /// <summary>The pending actions currently queued.</summary>
        public IReadOnlyList<QueueItemEntry> QueueItems { get; set; } = new List<QueueItemEntry>();
    }
}
