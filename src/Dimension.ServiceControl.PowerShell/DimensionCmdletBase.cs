using System;
using System.Management.Automation;
using Dimension.ServiceControl;
using Dimension.ServiceControl.Model;

namespace Dimension.ServiceControl.PowerShell
{
    /// <summary>
    /// Holds the ScdSvcCtl.exe path the cmdlets use when one is not passed in.
    /// </summary>
    /// <remarks>
    /// Set once per session with Set-DimensionServiceControlPath so that every later call
    /// stays short. An environment variable is also honoured, which is what scheduled tasks
    /// and build agents normally use.
    /// </remarks>
    public static class ServiceControlPathDefault
    {
        /// <summary>
        /// The environment variable read when no path has been set in the session.
        /// </summary>
        public const string EnvironmentVariableName = "DIMENSION_SCDSVCCTL_PATH";

        private static string sessionPath;

        /// <summary>
        /// Sets the path used by later cmdlet calls in this session.
        /// </summary>
        public static void Set(string path)
        {
            sessionPath = path;
        }

        /// <summary>
        /// The path in force, taken from the session first and the environment second,
        /// or null when neither is set.
        /// </summary>
        public static string Current
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(sessionPath))
                {
                    return sessionPath;
                }

                string fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariableName);

                return string.IsNullOrWhiteSpace(fromEnvironment) ? null : fromEnvironment;
            }
        }
    }

    /// <summary>
    /// Shared behaviour for the Dimension cmdlets: finding the utility, building a client
    /// and turning wrapper failures into PowerShell errors.
    /// </summary>
    public abstract class DimensionCmdletBase : PSCmdlet
    {
        /// <summary>
        /// Path to ScdSvcCtl.exe. Falls back to the session default set by
        /// Set-DimensionServiceControlPath, then to the environment variable.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string ExecutablePath { get; set; }

        /// <summary>
        /// How long to wait for the utility before giving up. Defaults to five minutes.
        /// </summary>
        [Parameter]
        [ValidateRange(1, 86400)]
        public int? CommandTimeoutSeconds { get; set; }

        /// <summary>
        /// Do not reject a response whose schema version differs from the one the wrapper
        /// was built against. Only useful as a stopgap right after a Dimension upgrade.
        /// </summary>
        [Parameter]
        public SwitchParameter SkipSchemaVersionCheck { get; set; }

        /// <summary>
        /// Builds a client from the parameters and session defaults.
        /// </summary>
        protected ServiceControlClient CreateClient()
        {
            string resolvedPath = !string.IsNullOrWhiteSpace(ExecutablePath)
                ? ExecutablePath
                : ServiceControlPathDefault.Current;

            if (string.IsNullOrWhiteSpace(resolvedPath))
            {
                ThrowTerminatingError(new ErrorRecord(
                    new PSInvalidOperationException(
                        "No path to ScdSvcCtl.exe is set. Pass -ExecutablePath, run "
                        + "Set-DimensionServiceControlPath, or set the "
                        + ServiceControlPathDefault.EnvironmentVariableName + " environment variable."),
                    "ServiceControlPathNotSet",
                    ErrorCategory.InvalidOperation,
                    targetObject: null));
            }

            var options = new ServiceControlOptions(resolvedPath)
            {
                ValidateSchemaVersions = !SkipSchemaVersionCheck.IsPresent
            };

            if (CommandTimeoutSeconds.HasValue)
            {
                options.CommandTimeout = TimeSpan.FromSeconds(CommandTimeoutSeconds.Value);
            }

            return new ServiceControlClient(options);
        }

        /// <summary>
        /// Surfaces any warnings a listing came back with.
        /// </summary>
        /// <remarks>
        /// Warnings usually mean the listing is incomplete, for example because an agent
        /// could not be reached, so they are shown rather than dropped.
        /// </remarks>
        protected void WriteListWarnings(ListResult result)
        {
            if (result == null || !result.HasWarnings)
            {
                return;
            }

            foreach (string warning in result.Warnings)
            {
                WriteWarning(warning);
            }
        }

        /// <summary>
        /// Turns a wrapper failure into an error record with a category that matches what
        /// actually went wrong.
        /// </summary>
        protected ErrorRecord ToErrorRecord(ServiceControlException failure)
        {
            return new ErrorRecord(
                failure,
                ChooseErrorId(failure),
                ChooseErrorCategory(failure),
                failure.CommandLine);
        }

        private static string ChooseErrorId(ServiceControlException failure)
        {
            var commandFailure = failure as CommandFailedException;

            return commandFailure != null
                ? "ScdSvcCtl" + commandFailure.ExitCode
                : failure.GetType().Name;
        }

        private static ErrorCategory ChooseErrorCategory(ServiceControlException failure)
        {
            if (failure is InvalidRequestException)
            {
                return ErrorCategory.InvalidArgument;
            }

            if (failure is CommandTimeoutException)
            {
                return ErrorCategory.OperationTimeout;
            }

            if (failure is SchemaVersionMismatchException || failure is ResponseParseException)
            {
                return ErrorCategory.InvalidData;
            }

            var commandFailure = failure as CommandFailedException;

            if (commandFailure != null && commandFailure.ExitCode == ServiceControlExitCode.AuthorizationFailed)
            {
                return ErrorCategory.PermissionDenied;
            }

            return ErrorCategory.InvalidOperation;
        }
    }
}
