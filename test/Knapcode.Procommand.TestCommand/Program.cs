using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;

namespace Knapcode.Procommand.TestCommand
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var application = new CommandLineApplication();

            application.Name = typeof(Program).GetTypeInfo().Assembly.GetName().Name;
            application.Description = "An application to help testing Knapcode.Procommand.";

            application.Command(
                "dump",
                command =>
                {
                    command.Description = "Dump environment information in JSON format.";

                    command.OnExecute(() =>
                    {
                        var dump = new
                        {
                            Arguments = args,
                            Environment = Environment.GetEnvironmentVariables(),
                            CurrentDirectory = Directory.GetCurrentDirectory()
                        };

                        Console.WriteLine(JsonConvert.SerializeObject(dump, Formatting.Indented));

                        return 0;
                    });
                });

            application.Command(
                "read-file",
                command =>
                {
                    command.Description = "Write out the contents of a file.";

                    var pathArgument = command.Argument("path", "The path (absolute or relative) to the file.");

                    command.OnExecute(() =>
                    {
                        using (var fileStream = File.OpenRead(pathArgument.Value))
                        using (var output = Console.OpenStandardOutput())
                        {
                            fileStream.CopyTo(output);
                        }

                        return 0;
                    });
                });

            application.Command(
                "output",
                command =>
                {
                    command.Description = "Write things to STDOUT or STDERR.";

                    var stdout = command.Option("--stdout", "What to write to STDOUT.", CommandOptionType.SingleValue);
                    var stderr = command.Option("--stderr", "What to write to STDERR.", CommandOptionType.SingleValue);

                    command.OnExecute(() =>
                    {
                        if (stdout.HasValue())
                        {
                            Console.WriteLine(stdout.Value());
                        }

                        if (stderr.HasValue())
                        {
                            Console.Error.WriteLine(stderr.Value());
                        }

                        return 0;
                    });
                });

            application.OnExecute(() =>
            {
                application.ShowHelp();

                return 0;
            });

            return application.Execute(args);
        }
    }
}
