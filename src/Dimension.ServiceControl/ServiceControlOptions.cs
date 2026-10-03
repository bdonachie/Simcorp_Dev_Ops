using System;
using System.IO;

namespace Dimension.ServiceControl
{
    /// <summary>
    /// Settings controlling how <see cref="ServiceControlClient"/> runs ScdSvcCtl.exe.
    /// </summary>
    public sealed class ServiceControlOptions
    {
        private static readonly TimeSpan DefaultCommandTimeout = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Creates options pointing at a copy of the utility.
        /// </summary>
        /// <param name="executablePath">
        /// Full path to ScdSvcCtl.exe, normally a UNC path into the Dimension installation
        /// share such as \\dimension-share\DEVT1\bin\ScdSvcCtl.exe.
        /// </param>
        public ServiceControlOptions(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                throw new ArgumentException(
                    "A path to ScdSvcCtl.exe must be supplied.",
                    nameof(executablePath));
            }

            ExecutablePath = executablePath;
        }

        /// <summary>Full path to ScdSvcCtl.exe.</summary>
        public string ExecutablePath { get; }

        /// <summary>
        /// How long to wait for one invocation before giving up. Defaults to five minutes.
        /// </summary>
        /// <remarks>
        /// The generous default is deliberate. Commands against a busy or partly unreachable
        /// platform can take minutes, and a start or stop issued with WaitForCompletion takes
        /// as long as the service itself does.
        /// </remarks>
        public TimeSpan CommandTimeout { get; set; } = DefaultCommandTimeout;

        /// <summary>
        /// Whether to reject a response whose schema version differs from the one this
        /// library was written against. Defaults to true.
        /// </summary>
        /// <remarks>
        /// Every response pins a fixed version, so a mismatch means the installed Dimension
        /// version changed the contract. Failing loudly beats deserialising a document whose
        /// meaning may have shifted, which is exactly how parsing the plain text output went
        /// wrong. Turn this off only as a stopgap while the models are being updated.
        /// </remarks>
        public bool ValidateSchemaVersions { get; set; } = true;

        /// <summary>
        /// Working directory for the utility. Defaults to the folder holding the executable
        /// so it resolves its own configuration and dependencies from the install share.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// The working directory to use, falling back to the executable's own folder.
        /// </summary>
        public string ResolveWorkingDirectory()
        {
            if (!string.IsNullOrWhiteSpace(WorkingDirectory))
            {
                return WorkingDirectory;
            }

            return Path.GetDirectoryName(ExecutablePath) ?? string.Empty;
        }
    }
}
