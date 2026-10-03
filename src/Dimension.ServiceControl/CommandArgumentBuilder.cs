using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Dimension.ServiceControl
{
    /// <summary>
    /// Builds a ScdSvcCtl.exe command line.
    /// </summary>
    /// <remarks>
    /// The utility takes a command path followed by switches written as -name:value, or a
    /// bare -name for a flag. Values are quoted only when they need it, which keeps the
    /// command line readable enough to paste into a console when chasing a failure.
    /// </remarks>
    public sealed class CommandArgumentBuilder
    {
        private static readonly char[] CharactersNeedingQuotes = { ' ', '\t', '"', '\n', '\r' };

        private readonly List<string> commandPath = new List<string>();
        private readonly List<string> switches = new List<string>();

        /// <summary>
        /// Starts a command line for a command path, such as "service" then "list".
        /// </summary>
        public CommandArgumentBuilder(params string[] command)
        {
            if (command != null)
            {
                commandPath.AddRange(command);
            }
        }

        /// <summary>
        /// Adds a bare flag such as -processes, but only when <paramref name="include"/>
        /// is true.
        /// </summary>
        public CommandArgumentBuilder AddFlag(string name, bool include = true)
        {
            if (include)
            {
                switches.Add("-" + name);
            }

            return this;
        }

        /// <summary>
        /// Adds a -name:value switch.
        /// </summary>
        public CommandArgumentBuilder AddSwitch(string name, string value)
        {
            switches.Add("-" + name + ":" + Quote(value));
            return this;
        }

        /// <summary>
        /// Adds a -name:value switch, skipping it when the value is blank.
        /// </summary>
        /// <remarks>
        /// Leaving a filter out and passing it as an empty string mean different things to
        /// the utility, so a blank value is dropped rather than sent through.
        /// </remarks>
        public CommandArgumentBuilder AddSwitchIfSet(string name, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                AddSwitch(name, value);
            }

            return this;
        }

        /// <summary>
        /// Adds a numeric -name:value switch, skipping it when the value is null.
        /// </summary>
        public CommandArgumentBuilder AddSwitchIfSet(string name, int? value)
        {
            if (value.HasValue)
            {
                AddSwitch(name, value.Value.ToString(CultureInfo.InvariantCulture));
            }

            return this;
        }

        /// <summary>
        /// Adds a switch the utility expects as the literal text Yes or No, skipping it
        /// when the value is null.
        /// </summary>
        public CommandArgumentBuilder AddYesNoSwitchIfSet(string name, bool? value)
        {
            if (value.HasValue)
            {
                AddSwitch(name, value.Value ? "Yes" : "No");
            }

            return this;
        }

        /// <summary>
        /// Renders the command line as a single argument string.
        /// </summary>
        public string Build()
        {
            var arguments = new StringBuilder();

            foreach (string commandWord in commandPath)
            {
                AppendSeparated(arguments, commandWord);
            }

            foreach (string switchText in switches)
            {
                AppendSeparated(arguments, switchText);
            }

            return arguments.ToString();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Build();
        }

        private static void AppendSeparated(StringBuilder arguments, string text)
        {
            if (arguments.Length > 0)
            {
                arguments.Append(' ');
            }

            arguments.Append(text);
        }

        /// <summary>
        /// Quotes a value when it contains characters that would otherwise split the
        /// argument, following the Windows command line rules.
        /// </summary>
        private static string Quote(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            bool needsQuotes = value.Length == 0 || value.IndexOfAny(CharactersNeedingQuotes) >= 0;

            if (!needsQuotes)
            {
                return value;
            }

            var quoted = new StringBuilder(value.Length + 2);
            quoted.Append('"');

            int backslashRun = 0;

            foreach (char character in value)
            {
                if (character == '\\')
                {
                    backslashRun++;
                    continue;
                }

                if (character == '"')
                {
                    // Backslashes directly before a quote have to be doubled and the quote
                    // escaped, or the parser swallows the quote.
                    quoted.Append('\\', (backslashRun * 2) + 1);
                    quoted.Append('"');
                }
                else
                {
                    quoted.Append('\\', backslashRun);
                    quoted.Append(character);
                }

                backslashRun = 0;
            }

            // Trailing backslashes would otherwise escape the closing quote.
            quoted.Append('\\', backslashRun * 2);
            quoted.Append('"');

            return quoted.ToString();
        }
    }
}
