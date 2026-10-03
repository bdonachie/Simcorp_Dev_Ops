using System;
using System.Collections.Generic;

namespace Dimension.ServiceControl
{
    /// <summary>
    /// Base class for every failure raised by <see cref="ServiceControlClient"/>.
    /// </summary>
    public class ServiceControlException : Exception
    {
        /// <summary>
        /// Creates a failure carrying the command that produced it.
        /// </summary>
        public ServiceControlException(string message, string commandLine)
            : base(message)
        {
            CommandLine = commandLine;
        }

        /// <summary>
        /// Creates a failure carrying the command that produced it and an underlying cause.
        /// </summary>
        public ServiceControlException(string message, string commandLine, Exception innerException)
            : base(message, innerException)
        {
            CommandLine = commandLine;
        }

        /// <summary>
        /// The command line that was run. Reproduced as passed, so it can be pasted into a
        /// console to reproduce the failure by hand.
        /// </summary>
        public string CommandLine { get; }
    }

    /// <summary>
    /// Raised when ScdSvcCtl.exe returned a failing exit code.
    /// </summary>
    /// <remarks>
    /// Only positive exit codes are treated as failures. Negative codes report state, such
    /// as the platform being in maintenance mode, and are returned to the caller instead.
    /// </remarks>
    public sealed class CommandFailedException : ServiceControlException
    {
        /// <summary>
        /// Creates a failure describing a failing exit code.
        /// </summary>
        public CommandFailedException(
            ServiceControlExitCode exitCode,
            int rawExitCode,
            string commandLine,
            string standardError,
            string standardOutput)
            : base(BuildMessage(exitCode, rawExitCode, standardError, standardOutput), commandLine)
        {
            ExitCode = exitCode;
            RawExitCode = rawExitCode;
            StandardError = standardError;
            StandardOutput = standardOutput;
        }

        /// <summary>The exit code mapped onto the documented list.</summary>
        public ServiceControlExitCode ExitCode { get; }

        /// <summary>
        /// The exit code exactly as the process returned it, for the case where the utility
        /// returns a code this library does not know about yet.
        /// </summary>
        public int RawExitCode { get; }

        /// <summary>Whatever the utility wrote to standard error.</summary>
        public string StandardError { get; }

        /// <summary>Whatever the utility wrote to standard output.</summary>
        public string StandardOutput { get; }

        private static string BuildMessage(
            ServiceControlExitCode exitCode,
            int rawExitCode,
            string standardError,
            string standardOutput)
        {
            string exitCodeText = Enum.IsDefined(typeof(ServiceControlExitCode), rawExitCode)
                ? exitCode + " (" + rawExitCode + ")"
                : "undocumented exit code " + rawExitCode;

            string details = !string.IsNullOrWhiteSpace(standardError)
                ? standardError.Trim()
                : (standardOutput ?? string.Empty).Trim();

            string message = "ScdSvcCtl.exe failed with " + exitCodeText + ".";

            return details.Length == 0 ? message : message + " " + details;
        }
    }

    /// <summary>
    /// Raised when a response declares a schema version this library was not written for.
    /// </summary>
    public sealed class SchemaVersionMismatchException : ServiceControlException
    {
        /// <summary>
        /// Creates a failure describing the version mismatch.
        /// </summary>
        public SchemaVersionMismatchException(
            string documentName,
            int expectedVersion,
            int expectedMinorVersion,
            int actualVersion,
            int actualMinorVersion,
            string commandLine)
            : base(
                BuildMessage(documentName, expectedVersion, expectedMinorVersion, actualVersion, actualMinorVersion),
                commandLine)
        {
            DocumentName = documentName;
            ExpectedVersion = expectedVersion;
            ExpectedMinorVersion = expectedMinorVersion;
            ActualVersion = actualVersion;
            ActualMinorVersion = actualMinorVersion;
        }

        /// <summary>The root element name of the response.</summary>
        public string DocumentName { get; }

        /// <summary>The major version this library expects.</summary>
        public int ExpectedVersion { get; }

        /// <summary>The minor version this library expects.</summary>
        public int ExpectedMinorVersion { get; }

        /// <summary>The major version the response declared.</summary>
        public int ActualVersion { get; }

        /// <summary>The minor version the response declared.</summary>
        public int ActualMinorVersion { get; }

        private static string BuildMessage(
            string documentName,
            int expectedVersion,
            int expectedMinorVersion,
            int actualVersion,
            int actualMinorVersion)
        {
            return "The " + documentName + " response declared schema version "
                + actualVersion + "." + actualMinorVersion
                + " but this library was written against "
                + expectedVersion + "." + expectedMinorVersion
                + ". The installed Dimension version has changed the contract. Re-export the "
                + "schemas with 'ScdSvcCtl.exe exportschemas' and update the models.";
        }
    }

    /// <summary>
    /// Raised when the utility reported an unexpected error in place of a payload.
    /// </summary>
    /// <remarks>
    /// Every schema allows the payload to be replaced by a single UnexpectedError element,
    /// and that can arrive with a success exit code, so it is checked separately.
    /// </remarks>
    public sealed class UnexpectedErrorException : ServiceControlException
    {
        /// <summary>
        /// Creates a failure carrying the reported error text.
        /// </summary>
        public UnexpectedErrorException(string reportedError, string commandLine)
            : base("ScdSvcCtl.exe reported an unexpected error: " + reportedError, commandLine)
        {
            ReportedError = reportedError;
        }

        /// <summary>The error text the utility reported.</summary>
        public string ReportedError { get; }
    }

    /// <summary>
    /// Raised when the utility did not finish within the configured timeout.
    /// </summary>
    public sealed class CommandTimeoutException : ServiceControlException
    {
        /// <summary>
        /// Creates a failure describing the timeout.
        /// </summary>
        public CommandTimeoutException(TimeSpan commandTimeout, string commandLine)
            : base(
                "ScdSvcCtl.exe did not finish within " + commandTimeout + " and was stopped.",
                commandLine)
        {
            CommandTimeout = commandTimeout;
        }

        /// <summary>The timeout that elapsed.</summary>
        public TimeSpan CommandTimeout { get; }
    }

    /// <summary>
    /// Raised when a response could not be read as the expected XML document.
    /// </summary>
    public sealed class ResponseParseException : ServiceControlException
    {
        /// <summary>
        /// Creates a failure carrying the output that could not be read.
        /// </summary>
        public ResponseParseException(
            string documentName,
            string standardOutput,
            string commandLine,
            Exception innerException)
            : base(
                "The " + documentName + " response could not be read as XML. "
                + "Check the command was run with -format:xml.",
                commandLine,
                innerException)
        {
            DocumentName = documentName;
            StandardOutput = standardOutput;
        }

        /// <summary>The root element name that was expected.</summary>
        public string DocumentName { get; }

        /// <summary>The raw output that failed to parse.</summary>
        public string StandardOutput { get; }
    }

    /// <summary>
    /// Raised when a request does not make sense, before anything is run.
    /// </summary>
    public sealed class InvalidRequestException : ServiceControlException
    {
        /// <summary>
        /// Creates a failure describing why the request was rejected.
        /// </summary>
        public InvalidRequestException(string message)
            : base(message, commandLine: null)
        {
        }

        /// <summary>
        /// Creates a failure describing several reasons the request was rejected.
        /// </summary>
        public InvalidRequestException(IEnumerable<string> reasons)
            : base(string.Join(" ", reasons), commandLine: null)
        {
        }
    }
}
