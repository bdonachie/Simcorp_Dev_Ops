using System.IO;
using System.Management.Automation;
using Dimension.ServiceControl;

namespace Dimension.ServiceControl.PowerShell
{
    /// <summary>
    /// Sets the ScdSvcCtl.exe path the other cmdlets use for the rest of the session.
    /// </summary>
    /// <example>
    ///   <code>Set-DimensionServiceControlPath '\\dimension-share\DEVT1\bin\ScdSvcCtl.exe'</code>
    /// </example>
    [Cmdlet(VerbsCommon.Set, "DimensionServiceControlPath")]
    [OutputType(typeof(string))]
    public sealed class SetDimensionServiceControlPathCmdlet : PSCmdlet
    {
        /// <summary>Full path to ScdSvcCtl.exe.</summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        /// <summary>
        /// Accept the path even when it cannot be reached right now, which is useful when
        /// preparing a session for a share that is not mounted yet.
        /// </summary>
        [Parameter]
        public SwitchParameter SkipValidation { get; set; }

        /// <summary>Write the path back to the pipeline.</summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            if (!SkipValidation.IsPresent && !File.Exists(Path))
            {
                WriteError(new ErrorRecord(
                    new FileNotFoundException("ScdSvcCtl.exe was not found at '" + Path + "'.", Path),
                    "ServiceControlExecutableNotFound",
                    ErrorCategory.ObjectNotFound,
                    Path));
                return;
            }

            ServiceControlPathDefault.Set(Path);

            if (PassThru.IsPresent)
            {
                WriteObject(Path);
            }
        }
    }

    /// <summary>
    /// Shows the ScdSvcCtl.exe path currently in force.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "DimensionServiceControlPath")]
    [OutputType(typeof(string))]
    public sealed class GetDimensionServiceControlPathCmdlet : PSCmdlet
    {
        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            string path = ServiceControlPathDefault.Current;

            if (string.IsNullOrWhiteSpace(path))
            {
                WriteWarning(
                    "No path is set. Use Set-DimensionServiceControlPath, or set the "
                    + ServiceControlPathDefault.EnvironmentVariableName + " environment variable.");
                return;
            }

            WriteObject(path);
        }
    }

    /// <summary>
    /// Checks that the utility can be reached and reports the version it prints.
    /// </summary>
    /// <remarks>
    /// Worth running first from a new machine or service account, because it separates a
    /// path or permissions problem from a platform one.
    /// </remarks>
    [Cmdlet(VerbsDiagnostic.Test, "DimensionServiceControl")]
    [OutputType(typeof(string))]
    public sealed class TestDimensionServiceControlCmdlet : DimensionCmdletBase
    {
        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            try
            {
                WriteObject(CreateClient().GetUtilityVersion());
            }
            catch (ServiceControlException failure)
            {
                WriteError(ToErrorRecord(failure));
            }
        }
    }
}
