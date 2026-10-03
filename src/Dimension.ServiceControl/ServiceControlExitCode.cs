namespace Dimension.ServiceControl
{
    /// <summary>
    /// Exit codes returned by ScdSvcCtl.exe.
    /// </summary>
    /// <remarks>
    /// Taken from the table the utility itself publishes through "exitcode list".
    /// Zero is success, positive values are failures, and negative values carry state
    /// information rather than an error. See <see cref="ExitCodeExtensions.IsFailure"/>.
    /// </remarks>
    public enum ServiceControlExitCode
    {
        /// <summary>Command completed successfully.</summary>
        Success = 0,

        // Negative values report state, not failure.

        /// <summary>Enabled, meaning started.</summary>
        Enabled = -1,

        /// <summary>Disabled, meaning paused.</summary>
        Disabled = -2,

        /// <summary>The platform is suspended.</summary>
        PlatformSuspended = -3,

        /// <summary>The platform is restarting.</summary>
        PlatformRestarting = -4,

        /// <summary>Blacklisted services were found.</summary>
        Blacklisted = -5,

        /// <summary>The condition was not fulfilled.</summary>
        NotFulfilled = -6,

        /// <summary>Import succeeded and replaced an existing model.</summary>
        ImportModelReplaced = -7,

        /// <summary>Import succeeded and an identical model already existed.</summary>
        ImportModelIdenticalExist = -8,

        /// <summary>User blocks exist.</summary>
        UserBlocksExist = -9,

        /// <summary>The system is in production mode.</summary>
        ProductionMode = -10,

        /// <summary>The system is in maintenance mode.</summary>
        MaintenanceMode = -11,

        // Positive values report failure.

        /// <summary>The command line parameters could not be parsed.</summary>
        ParseError = 1,

        /// <summary>The command entered is not known.</summary>
        UnknownCommandLineAction = 2,

        /// <summary>The command failed.</summary>
        CommandFailed = 3,

        /// <summary>An automation operation failed.</summary>
        AutomationFailed = 7,

        /// <summary>
        /// Authorization failed. The platform may not be running, or the calling account
        /// is not authenticated or not authorized.
        /// </summary>
        AuthorizationFailed = 11,

        /// <summary>Some or all service instances could not be stopped.</summary>
        StopServicesSomeFailed = 15,

        /// <summary>Internal error. The output carries more detail.</summary>
        InternalError = 17,

        /// <summary>The system must be in maintenance mode for this command.</summary>
        MustBeInMaintenanceMode = 20,

        /// <summary>The service type cannot be pinged because it defines no endpoints.</summary>
        CannotBePinged = 21,

        /// <summary>No active automation was found.</summary>
        AutomationNotActive = 22,

        /// <summary>No blacklisted services were found.</summary>
        NoneBlacklisted = 23,

        /// <summary>
        /// No service instances are online for the filter, or a count was requested and
        /// the number online does not match it.
        /// </summary>
        ServicesNotOnline = 24,

        /// <summary>Import failed because the model name already exists.</summary>
        ImportModelNameExist = 28,

        /// <summary>Import failed because the model was invalid.</summary>
        ImportModelInvalidModel = 29,

        /// <summary>Export failed because an identical file already exists.</summary>
        ExportFileExists = 30,

        /// <summary>The version is not supported.</summary>
        VersionViolation = 31,

        /// <summary>No active service operations controller was found.</summary>
        SvcOpsControllerNotActive = 32
    }

    /// <summary>
    /// Helpers for reading an exit code.
    /// </summary>
    public static class ExitCodeExtensions
    {
        /// <summary>
        /// Whether the exit code means the command failed.
        /// </summary>
        /// <remarks>
        /// Only positive codes are failures. A negative code such as MaintenanceMode is a
        /// successful answer to a question, so treating "not zero" as failure would report
        /// errors that did not happen.
        /// </remarks>
        public static bool IsFailure(this ServiceControlExitCode exitCode)
        {
            return (int)exitCode > 0;
        }

        /// <summary>
        /// Whether the exit code reports state rather than success or failure.
        /// </summary>
        public static bool IsStateInformation(this ServiceControlExitCode exitCode)
        {
            return (int)exitCode < 0;
        }
    }
}
