using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Knapcode.Procommand
{
    public class ArgumentsBuilder
    {
        public ArgumentsBuilder()
        {
            Arguments = new List<string>();
        }

        public ArgumentsBuilder(IEnumerable<string> arguments)
        {
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            Arguments = new List<string>(arguments);
        }
        
        public IList<string> Arguments { get; }

        public string Build()
        {
            var stringBuilder = new StringBuilder();

            foreach (var argument in Arguments)
            {
                if (argument == null)
                {
                    throw new InvalidOperationException("An argument cannot be null.");
                }

                var escapedArgument = Escape(argument);
                if (stringBuilder.Length > 0)
                {
                    stringBuilder.Append(' ');
                }

                stringBuilder.Append(escapedArgument);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Escapes a string so that it can be safely passed as a command line argument when starting a process. This is
        /// intended to be used as part of the string set on <see cref="ProcessStartInfo.Arguments"/>. This code is
        /// based on this StackOverflow answer: http://stackoverflow.com/a/12364234.
        /// </summary>
        /// <param name="argument">The string to escaped.</param>
        /// <returns>The escaped string.</returns>
        public static string Escape(string argument)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(nameof(argument));
            }

            if (argument == string.Empty)
            {
                return "\"\"";
            }

            if (argument.Contains('\0'))
            {
                throw new ArgumentException("The null character cannot be passed as a command line argument.", nameof(argument));
            }

            var escaped = Regex.Replace(
                argument,
                @"(\\*)""", @"$1\$0");

            escaped = Regex.Replace(
                escaped,
                @"^(.*\s.*?)(\\*)$", @"""$1$2$2""",
                RegexOptions.Singleline);

            return escaped;
        }
    }
}
