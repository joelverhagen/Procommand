using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Knapcode.Procommand.Sandbox
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var testCommandPath = TestCommand.Program.GetAbsolutePath();
            var inputArguments = new[]
            {
                "dump",
                "'",
                "'''",
                "`",
                "!",
                "&",
                "@",
                "^",
                "\'",
                "\\",
                "/",
                "\a",
                "\b",
                "\f",
                "\v",
                "'foo bar'",
                "foo",
                "",
                "-",
                "\\",
                "/",
                "--",
                "---",
                "bar",
                "\"",
                "\"\"",
                "\"\"\"",
                "\"\"\"\"",
                "\"\"\"\"\"",
                "\r",
                "\n",
                "\t",
                "foo\nbar",
                "foo\rbar",
                "foo\r\nbar",
                "\n\r"
            };

            var encoded = inputArguments.Select(EscapeCommandLineArgument).ToArray();

            var command = new Command(testCommandPath, string.Join(" ", encoded));
            var runner = new CommandRunner();
            var result = runner.Run(command);
            var output = JsonConvert.DeserializeObject<JObject>(result.Output);

            var outputArguments = output["Arguments"].ToObject<string[]>();
            
            for (var i = 0; i < outputArguments.Length; i++)
            {
                Console.WriteLine(new string('=', 60));
                var outputArgument = outputArguments[i];

                if (i < inputArguments.Length)
                {
                    var inputArgument = inputArguments[i];
                    Console.WriteLine($"Input: {JsonConvert.SerializeObject(inputArgument)}");
                    Console.WriteLine($"Equal: {inputArgument == outputArgument}");
                }

                Console.WriteLine(JsonConvert.SerializeObject(outputArgument));
                Console.WriteLine(BitConverter.ToString(Encoding.UTF8.GetBytes(outputArgument)));
            }

            Console.WriteLine(new string('=', 60));
            Console.WriteLine($"All equal: {inputArguments.SequenceEqual(outputArguments)}");
        }

        /// <summary>
        /// Escapes a string so that it can be safely passed as a command line argument when starting a process. This is
        /// intended to be used as part of the string set on <see cref="ProcessStartInfo.Arguments"/>. This code is
        /// based on this StackOverflow answer: http://stackoverflow.com/a/12364234.
        /// </summary>
        /// <param name="argument">The string to escaped.</param>
        /// <returns>The escaped string.</returns>
        public static string EscapeCommandLineArgument(string argument)
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
