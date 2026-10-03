using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Dimension.ServiceControl.Model;

namespace Dimension.ServiceControl
{
    /// <summary>
    /// A typed wrapper over the SimCorp Dimension service control utility, ScdSvcCtl.exe.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every listing is requested with -format:xml and read through the schemas the utility
    /// publishes with "exportschemas", rather than by picking apart its plain text output.
    /// The XML carries fields the text does not, notably the service type id, and it
    /// declares a schema version so a Dimension upgrade that changes the contract is caught
    /// instead of being silently mis-read.
    /// </para>
    /// <para>
    /// Failures surface as exceptions carrying the exit code and the command line that
    /// produced them. Exit codes that report state rather than failure, such as the platform
    /// being in maintenance mode, are returned to the caller instead of thrown.
    /// </para>
    /// </remarks>
    public sealed class ServiceControlClient
    {
        private readonly ServiceControlOptions options;

        /// <summary>
        /// Creates a client for the utility at the given path.
        /// </summary>
        /// <param name="executablePath">
        /// Full path to ScdSvcCtl.exe, normally on the Dimension installation share.
        /// </param>
        public ServiceControlClient(string executablePath)
            : this(new ServiceControlOptions(executablePath))
        {
        }

        /// <summary>
        /// Creates a client with fully specified options.
        /// </summary>
        public ServiceControlClient(ServiceControlOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>The options this client was created with.</summary>
        public ServiceControlOptions Options
        {
            get { return options; }
        }

        /// <summary>
        /// Lists service instances that are not closed.
        /// </summary>
        /// <param name="filter">
        /// Optional filters applied by the utility. Null lists everything.
        /// </param>
        /// <returns>
        /// The service instances, flattened so each carries its agent details, along with
        /// any warnings raised while producing the listing.
        /// </returns>
        public ServiceListResult ListServices(ServiceListFilter filter = null)
        {
            filter = filter ?? new ServiceListFilter();
            ThrowIfInvalid(filter.Validate());

            var arguments = new CommandArgumentBuilder("service", "list");
            arguments.AddSwitch("format", "xml");
            arguments.AddSwitchIfSet("agent", filter.AgentName);
            arguments.AddSwitchIfSet("nickname", filter.Nickname);
            arguments.AddFlag("nonickname", filter.WithoutNickname);
            arguments.AddSwitchIfSet("servicetype", filter.ServiceType);
            arguments.AddSwitchIfSet("serviceconfig", filter.ServiceConfiguration);
            arguments.AddFlag("noconfiguration", filter.WithoutConfiguration);
            arguments.AddFlag("online", filter.OnlineOnly);
            arguments.AddFlag("failed", filter.FailedOnly);
            arguments.AddFlag("processes", filter.IncludeProcesses);
            arguments.AddYesNoSwitchIfSet("stale", filter.IsStale);

            if (filter.ManagementMode != ManagementMode.Undefined)
            {
                arguments.AddSwitch("managementmode", filter.ManagementMode.ToString());
            }

            ServiceListResponse response = RunXmlCommand<ServiceListResponse>(
                arguments,
                "ServiceList",
                ServiceListResponse.ExpectedVersion,
                ServiceListResponse.ExpectedMinorVersion);

            return new ServiceListResult
            {
                Services = FlattenServices(response),
                Agents = response.Agents ?? new List<ServiceListAgent>(),
                ServiceSegment = response.ServiceSegment,
                DirectoryServices = response.DirectoryServices,
                Warnings = CollectWarnings(response.Warnings)
            };
        }

        /// <summary>
        /// Lists the service agents currently running, with the resource figures they report.
        /// </summary>
        public AgentListResult ListAgents()
        {
            var arguments = new CommandArgumentBuilder("agent", "list");
            arguments.AddSwitch("format", "xml");

            AgentListResponse response = RunXmlCommand<AgentListResponse>(
                arguments,
                "AgentList",
                AgentListResponse.ExpectedVersion,
                AgentListResponse.ExpectedMinorVersion);

            return new AgentListResult
            {
                Agents = response.RunningAgents ?? new List<AgentEntry>(),
                ServiceSegment = response.ServiceSegment,
                DirectoryServices = response.DirectoryServices,
                Warnings = CollectWarnings(response.Warnings)
            };
        }

        /// <summary>
        /// Lists the start, stop and move actions the controller has accepted but not yet
        /// completed.
        /// </summary>
        /// <remarks>
        /// This is how an asynchronous start or stop is followed to completion. An action
        /// disappearing from this list means the controller has finished with it; whether it
        /// worked is then read from the service listing.
        /// </remarks>
        public QueueItemListResult ListQueueItems()
        {
            var arguments = new CommandArgumentBuilder("queueitem", "list");
            arguments.AddSwitch("format", "xml");

            QueueItemListResponse response = RunXmlCommand<QueueItemListResponse>(
                arguments,
                "QueueitemList",
                QueueItemListResponse.ExpectedVersion,
                QueueItemListResponse.ExpectedMinorVersion);

            return new QueueItemListResult
            {
                QueueItems = response.QueueItems ?? new List<QueueItemEntry>(),
                ServiceSegment = response.ServiceSegment,
                DirectoryServices = response.DirectoryServices,
                Warnings = CollectWarnings(response.Warnings)
            };
        }

        /// <summary>
        /// Starts service instances.
        /// </summary>
        /// <returns>
        /// The exit code. Success means the instances started when
        /// <see cref="ServiceStartRequest.WaitForCompletion"/> was set, and that the request
        /// was accepted onto the queue when it was not.
        /// </returns>
        public ServiceControlExitCode StartServices(ServiceStartRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            ThrowIfInvalid(request.Validate());

            var arguments = new CommandArgumentBuilder("service", "start");
            arguments.AddSwitch("servicetype", request.ServiceType);
            arguments.AddSwitchIfSet("serviceconfig", request.ServiceConfiguration);
            arguments.AddSwitchIfSet("agent", request.AgentName);
            arguments.AddSwitchIfSet("count", request.Count);
            arguments.AddSwitchIfSet("nickname", request.Nickname);
            arguments.AddYesNoSwitchIfSet("automated", request.StartAutomated);
            arguments.AddFlag("wait", request.WaitForCompletion);

            return RunActionCommand(arguments);
        }

        /// <summary>
        /// Stops service instances.
        /// </summary>
        /// <returns>
        /// The exit code. <see cref="ServiceControlExitCode.StopServicesSomeFailed"/> is
        /// returned rather than thrown only when it is not a failure; it is positive, so it
        /// surfaces as a <see cref="CommandFailedException"/>.
        /// </returns>
        public ServiceControlExitCode StopServices(ServiceStopRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            ThrowIfInvalid(request.Validate());

            var arguments = new CommandArgumentBuilder("service", "stop");

            if (request.NamesInstances)
            {
                arguments.AddSwitch("service", string.Join(";", request.ServiceInstanceNames));
            }
            else
            {
                arguments.AddSwitchIfSet("nickname", request.Nickname);
                arguments.AddFlag("nonickname", request.WithoutNickname);
                arguments.AddSwitchIfSet("servicetype", request.ServiceType);
                arguments.AddSwitchIfSet("serviceconfig", request.ServiceConfiguration);
                arguments.AddFlag("noconfiguration", request.WithoutConfiguration);

                if (request.ManagementMode != ManagementMode.Undefined)
                {
                    arguments.AddSwitch("managementmode", request.ManagementMode.ToString());
                }
            }

            arguments.AddSwitchIfSet("count", request.Count);
            arguments.AddFlag("wait", request.WaitForCompletion);

            return RunActionCommand(arguments);
        }

        /// <summary>
        /// Cancels a pending action by its queue id.
        /// </summary>
        public ServiceControlExitCode CancelQueueItem(string queueId)
        {
            if (string.IsNullOrWhiteSpace(queueId))
            {
                throw new InvalidRequestException("A queue id is needed to cancel a queued action.");
            }

            var arguments = new CommandArgumentBuilder("queueitem", "cancel");
            arguments.AddSwitch("queueid", queueId);

            return RunActionCommand(arguments);
        }

        /// <summary>
        /// Returns the version banner the utility prints, useful for confirming which
        /// installation is being driven.
        /// </summary>
        public string GetUtilityVersion()
        {
            var arguments = new CommandArgumentBuilder("version");
            CommandResult result = CommandRunner.Run(options, arguments.Build());

            ThrowIfFailed(result);

            return result.StandardOutput.Trim();
        }

        /// <summary>
        /// Runs any command this client does not wrap yet and returns the raw result.
        /// </summary>
        /// <remarks>
        /// The utility exposes far more than is wrapped here, including maintenance mode,
        /// the platform commands and dependency models. This is the way to reach those
        /// without waiting for a typed method, while still going through the same process
        /// handling, timeout and exit code checks.
        /// </remarks>
        /// <param name="arguments">The command and switches to run.</param>
        /// <param name="throwOnFailure">
        /// Whether a failing exit code should raise <see cref="CommandFailedException"/>.
        /// </param>
        public CommandResult RunCommand(CommandArgumentBuilder arguments, bool throwOnFailure = true)
        {
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            CommandResult result = CommandRunner.Run(options, arguments.Build());

            if (throwOnFailure)
            {
                ThrowIfFailed(result);
            }

            return result;
        }

        /// <summary>
        /// Runs a command that produces no XML payload and returns its exit code.
        /// </summary>
        private ServiceControlExitCode RunActionCommand(CommandArgumentBuilder arguments)
        {
            CommandResult result = CommandRunner.Run(options, arguments.Build());
            ThrowIfFailed(result);
            return result.ExitCode;
        }

        /// <summary>
        /// Runs a command that produces XML, checks it and reads it into the model.
        /// </summary>
        private TResponse RunXmlCommand<TResponse>(
            CommandArgumentBuilder arguments,
            string documentName,
            int expectedVersion,
            int expectedMinorVersion)
            where TResponse : class, IVersionedResponse
        {
            CommandResult result = CommandRunner.Run(options, arguments.Build());
            ThrowIfFailed(result);

            TResponse response = Deserialize<TResponse>(documentName, result);

            // An unexpected error replaces the payload but can still arrive with a success
            // exit code, so it is checked separately from the exit code.
            if (!string.IsNullOrWhiteSpace(response.UnexpectedError))
            {
                throw new UnexpectedErrorException(response.UnexpectedError, result.CommandLine);
            }

            if (options.ValidateSchemaVersions
                && (response.Version != expectedVersion || response.MinorVersion != expectedMinorVersion))
            {
                throw new SchemaVersionMismatchException(
                    documentName,
                    expectedVersion,
                    expectedMinorVersion,
                    response.Version,
                    response.MinorVersion,
                    result.CommandLine);
            }

            return response;
        }

        private static TResponse Deserialize<TResponse>(string documentName, CommandResult result)
            where TResponse : class
        {
            try
            {
                // The single argument constructor is the one the framework caches, so this
                // does not leak a generated assembly per call.
                var serializer = new XmlSerializer(typeof(TResponse));

                using (var reader = new StringReader(result.StandardOutput))
                using (var xmlReader = XmlReader.Create(reader))
                {
                    return (TResponse)serializer.Deserialize(xmlReader);
                }
            }
            catch (Exception readFailure) when (readFailure is InvalidOperationException
                                             || readFailure is XmlException)
            {
                throw new ResponseParseException(
                    documentName,
                    result.StandardOutput,
                    result.CommandLine,
                    readFailure);
            }
        }

        /// <summary>
        /// Lifts each service instance out of its agent so the caller gets one flat list.
        /// </summary>
        private static IReadOnlyList<ServiceInstance> FlattenServices(ServiceListResponse response)
        {
            var services = new List<ServiceInstance>();

            if (response.Agents == null)
            {
                return services;
            }

            foreach (ServiceListAgent agent in response.Agents)
            {
                if (agent.Services == null)
                {
                    continue;
                }

                foreach (ServiceListEntry entry in agent.Services)
                {
                    services.Add(new ServiceInstance
                    {
                        AgentName = agent.Name,
                        AgentDescription = agent.Description,
                        AgentTag = agent.Tag,
                        Instance = entry.Instance,
                        Id = entry.Id,
                        Type = entry.Type,
                        ManagementMode = entry.ManagementMode,
                        ServiceState = entry.ServiceState,
                        Action = entry.Action,
                        IsStale = entry.IsStale,
                        Configuration = entry.Configuration,
                        Nickname = entry.Nickname,
                        Processes = entry.Processes
                    });
                }
            }

            return services;
        }

        private static IReadOnlyList<string> CollectWarnings(ResponseWarnings warnings)
        {
            return warnings == null ? new List<string>() : warnings.AllMessages;
        }

        private static void ThrowIfFailed(CommandResult result)
        {
            if (result.ExitCode.IsFailure())
            {
                throw new CommandFailedException(
                    result.ExitCode,
                    result.RawExitCode,
                    result.CommandLine,
                    result.StandardError,
                    result.StandardOutput);
            }
        }

        private static void ThrowIfInvalid(IReadOnlyList<string> problems)
        {
            if (problems != null && problems.Count > 0)
            {
                throw new InvalidRequestException(problems);
            }
        }
    }
}
