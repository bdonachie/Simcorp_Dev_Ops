using System.Collections.Generic;
using Dimension.ServiceControl.Model;

namespace Dimension.ServiceControl
{
    /// <summary>
    /// Filters narrowing a service listing. Everything is optional, and leaving it all
    /// unset lists every service instance that is not closed.
    /// </summary>
    /// <remarks>
    /// These mirror the switches on "service list". The utility applies them itself, so
    /// filtering here is much cheaper than listing everything and filtering afterwards.
    /// </remarks>
    public sealed class ServiceListFilter
    {
        /// <summary>Only service instances hosted on this agent.</summary>
        public string AgentName { get; set; }

        /// <summary>Only service instances with this nickname. Not case sensitive.</summary>
        public string Nickname { get; set; }

        /// <summary>Only service instances that have no nickname.</summary>
        public bool WithoutNickname { get; set; }

        /// <summary>Only service instances of this service type.</summary>
        public string ServiceType { get; set; }

        /// <summary>
        /// Only service instances running with this configuration.
        /// Needs <see cref="ServiceType"/> as well.
        /// </summary>
        public string ServiceConfiguration { get; set; }

        /// <summary>
        /// Only service instances that have no configuration.
        /// Needs <see cref="ServiceType"/> as well.
        /// </summary>
        public bool WithoutConfiguration { get; set; }

        /// <summary>
        /// Only service instances in this management mode. Undefined matches every mode.
        /// </summary>
        public ManagementMode ManagementMode { get; set; } = ManagementMode.Undefined;

        /// <summary>Only online service instances.</summary>
        public bool OnlineOnly { get; set; }

        /// <summary>Only failed service instances.</summary>
        public bool FailedOnly { get; set; }

        /// <summary>
        /// Match on stale information. Null applies no stale filter, true matches only
        /// stale instances, false matches only instances that are not stale.
        /// </summary>
        public bool? IsStale { get; set; }

        /// <summary>Include the hosting processes of each instance in the response.</summary>
        public bool IncludeProcesses { get; set; }

        /// <summary>
        /// Reasons this filter cannot be used, or an empty list when it is fine.
        /// </summary>
        public IReadOnlyList<string> Validate()
        {
            var problems = new List<string>();

            if (!string.IsNullOrWhiteSpace(ServiceConfiguration) && string.IsNullOrWhiteSpace(ServiceType))
            {
                problems.Add("Filtering by service configuration also needs a service type.");
            }

            if (WithoutConfiguration && string.IsNullOrWhiteSpace(ServiceType))
            {
                problems.Add("Filtering for instances without a configuration also needs a service type.");
            }

            if (WithoutNickname && !string.IsNullOrWhiteSpace(Nickname))
            {
                problems.Add("A nickname cannot be matched while also filtering for instances without one.");
            }

            if (!string.IsNullOrWhiteSpace(ServiceConfiguration) && WithoutConfiguration)
            {
                problems.Add("A configuration cannot be matched while also filtering for instances without one.");
            }

            return problems;
        }
    }

    /// <summary>
    /// Describes service instances to start. Mirrors the switches on "service start".
    /// </summary>
    public sealed class ServiceStartRequest
    {
        /// <summary>The smallest instance count the utility accepts.</summary>
        public const int MinimumCount = 1;

        /// <summary>The largest instance count the utility accepts.</summary>
        public const int MaximumCount = 100;

        /// <summary>The service type to start. Required.</summary>
        public string ServiceType { get; set; }

        /// <summary>
        /// The configuration id. Required for service types that take a configuration.
        /// </summary>
        public string ServiceConfiguration { get; set; }

        /// <summary>
        /// The agent to start on. The platform picks one when this is not set.
        /// </summary>
        public string AgentName { get; set; }

        /// <summary>How many instances to start. The utility defaults to one.</summary>
        public int? Count { get; set; }

        /// <summary>A nickname to assign, used instead of the full instance id.</summary>
        public string Nickname { get; set; }

        /// <summary>Whether to start in automated management mode rather than manual.</summary>
        public bool? StartAutomated { get; set; }

        /// <summary>
        /// Whether to wait until the instances have started. When false the command returns
        /// as soon as the request is queued, and progress can be followed through the queue
        /// item listing.
        /// </summary>
        public bool WaitForCompletion { get; set; }

        /// <summary>
        /// Reasons this request cannot be used, or an empty list when it is fine.
        /// </summary>
        public IReadOnlyList<string> Validate()
        {
            var problems = new List<string>();

            if (string.IsNullOrWhiteSpace(ServiceType))
            {
                problems.Add("A service type is needed to start a service.");
            }

            if (Count.HasValue && (Count.Value < MinimumCount || Count.Value > MaximumCount))
            {
                problems.Add(
                    "The instance count must be between " + MinimumCount + " and " + MaximumCount
                    + " but was " + Count.Value + ".");
            }

            return problems;
        }
    }

    /// <summary>
    /// Describes service instances to stop. Mirrors the switches on "service stop".
    /// </summary>
    /// <remarks>
    /// Either name instances through <see cref="ServiceInstanceNames"/> or describe them
    /// with the filters. The utility ignores every filter once explicit names are given, so
    /// mixing the two is rejected here rather than quietly stopping more than intended.
    /// </remarks>
    public sealed class ServiceStopRequest
    {
        /// <summary>
        /// Specific instances to stop, each written as "agent\instance". When set, the
        /// utility ignores every filter below.
        /// </summary>
        /// <remarks>
        /// The utility wants the qualified name here, not the numeric instance id from a
        /// listing. <see cref="Model.ServiceInstance.FullName"/> produces the right form.
        /// </remarks>
        public IList<string> ServiceInstanceNames { get; set; } = new List<string>();

        /// <summary>Only instances with this nickname. Not case sensitive.</summary>
        public string Nickname { get; set; }

        /// <summary>Only instances that have no nickname.</summary>
        public bool WithoutNickname { get; set; }

        /// <summary>Only instances of this service type.</summary>
        public string ServiceType { get; set; }

        /// <summary>
        /// Only instances running with this configuration. Needs <see cref="ServiceType"/>.
        /// </summary>
        public string ServiceConfiguration { get; set; }

        /// <summary>
        /// Only instances that have no configuration. Needs <see cref="ServiceType"/>.
        /// </summary>
        public bool WithoutConfiguration { get; set; }

        /// <summary>Only instances in this management mode.</summary>
        public ManagementMode ManagementMode { get; set; } = ManagementMode.Undefined;

        /// <summary>How many instances to stop. Must be positive when set.</summary>
        public int? Count { get; set; }

        /// <summary>Whether to wait until the instances have stopped.</summary>
        public bool WaitForCompletion { get; set; }

        /// <summary>Whether this request names instances rather than describing them.</summary>
        public bool NamesInstances
        {
            get { return ServiceInstanceNames != null && ServiceInstanceNames.Count > 0; }
        }

        /// <summary>Whether any filter has been set on this request.</summary>
        public bool HasFilters
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Nickname)
                    || WithoutNickname
                    || !string.IsNullOrWhiteSpace(ServiceType)
                    || !string.IsNullOrWhiteSpace(ServiceConfiguration)
                    || WithoutConfiguration
                    || ManagementMode != ManagementMode.Undefined;
            }
        }

        /// <summary>
        /// Reasons this request cannot be used, or an empty list when it is fine.
        /// </summary>
        public IReadOnlyList<string> Validate()
        {
            var problems = new List<string>();

            if (!NamesInstances && !HasFilters)
            {
                problems.Add(
                    "Stopping services needs either explicit instance names or at least one "
                    + "filter, so that every service instance is not stopped by accident.");
            }

            if (NamesInstances && HasFilters)
            {
                problems.Add(
                    "Explicit instance names cannot be combined with filters, because the "
                    + "utility ignores the filters once names are supplied.");
            }

            if (!string.IsNullOrWhiteSpace(ServiceConfiguration) && string.IsNullOrWhiteSpace(ServiceType))
            {
                problems.Add("Filtering by service configuration also needs a service type.");
            }

            if (WithoutConfiguration && string.IsNullOrWhiteSpace(ServiceType))
            {
                problems.Add("Filtering for instances without a configuration also needs a service type.");
            }

            if (Count.HasValue && Count.Value <= 0)
            {
                problems.Add("The instance count must be positive but was " + Count.Value + ".");
            }

            return problems;
        }
    }
}
