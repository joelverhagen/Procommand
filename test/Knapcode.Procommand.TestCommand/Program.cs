using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;

namespace Knapcode.Procommand.TestCommand
{
    public class Program
    {
        public static int Main(string[] args)
        {
            if (args.Contains("--debug", StringComparer.OrdinalIgnoreCase))
            {
                args = args
                    .Except(new[] { "--debug" }, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                Debugger.Launch();
            }

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
                "echo",
                command =>
                {
                    command.Description = "Write STDIN to STDOUT.";

                    command.OnExecute(() =>
                    {
                        var stdin = Console.OpenStandardInput();
                        var stdout = Console.OpenStandardOutput();

                        stdin.CopyTo(stdout);

                        return 0;
                    });
                });

            application.Command(
                "exit-code",
                command =>
                {
                    command.Description = "Returns an arbitrary exit code.";

                    var codeArgument = command.Argument("value", "The exit code to return.");

                    command.OnExecute(() =>
                    {
                        var value = int.Parse(codeArgument.Value);

                        return value;
                    });
                });

            application.Command(
                "interactive",
                command =>
                {
                    command.Description = "Runs an interactive command line application.";
                    
                    command.OnExecute(() =>
                    {
                        Console.WriteLine("Welcome to a silly interactive thing.");

                        Console.Write("Would you like to quit right now? (Y/N) ");
                        var continueAnswer = Console.ReadLine().Trim().ToUpperInvariant();
                        if (continueAnswer == "Y")
                        {
                            Console.WriteLine("Okay. We'll just quit right now.");
                            return 1;
                        }
                        else if (continueAnswer != "N")
                        {
                            Console.WriteLine("Invalid input. We will quit.");
                            return 1;
                        }
                        else
                        {
                            Console.WriteLine("Great! Let's continue.");
                        }

                        Console.WriteLine("What is your name?");
                        var name = Console.ReadLine().Trim();
                        Console.WriteLine($"Hello, {name}! How is your day?");
                        var dayStatus = Console.ReadLine().Trim();
                        Console.WriteLine($"Your day is {dayStatus}? I hope that's a good thing...");

                        return 0;
                    });
                });

            application.Command(
                "output",
                command =>
                {
                    command.Description = "Write things to STDOUT or STDERR.";

                    var linesArgument = command.Argument(
                        "lines",
                        "Lines to write to STDOUT or STDERR. Use the 'o:' prefix for STDOUT and 'e:' for STDERR.",
                        multipleValues: true);

                    command.OnExecute(() =>
                    {
                        foreach (var line in linesArgument.Values)
                        {
                            if (line.Length < 2)
                            {
                                continue;
                            }

                            var prefix = line.Substring(0, 2);
                            var value = line.Substring(2);

                            if (prefix == "o:")
                            {
                                Console.Out.WriteLine(value);
                                Console.Out.Flush();
                                Thread.Sleep(5);
                            }
                            else if (prefix == "e:")
                            {
                                Console.Error.WriteLine(value);
                                Console.Error.Flush();
                                Thread.Sleep(5);
                            }
                        }

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
                "wait",
                command =>
                {
                    command.Description = "Wait for a certain amount of time before exiting.";

                    var durationArgument = command.Argument("duration", "The duration in integer millisconds to wait.");

                    command.OnExecute(() =>
                    {
                        var milliseconds = int.Parse(durationArgument.Value);
                        var duration = TimeSpan.FromMilliseconds(milliseconds);

                        Console.WriteLine($"About to sleep {milliseconds} milliseconds.");
                        Thread.Sleep(duration);
                        Console.WriteLine("Done sleeping.");

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
