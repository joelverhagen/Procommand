using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Knapcode.Procommand.Test.TestSupport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Knapcode.Procommand.Test
{
    public class CommandRunnerTest
    {
        [Fact]
        public void Constructor_EscapesArguments()
        {
            // Arrange
            var testCommandPath = Utility.GetTestCommandPath();
            var expected = new[] { "dump", "foo", "\"bar baz\"" };
            var command = new Command(testCommandPath, expected);

            var target = new CommandRunner();

            // Act
            var result = target.Run(command);

            // Assert
            Assert.Equal(CommandStatus.Exited, result.Status);
            Assert.Equal(0, result.ExitCode);

            var dump = JsonConvert.DeserializeObject<JObject>(result.Output);
            var actual = dump["Arguments"].ToObject<string[]>();
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(true, -1)]
        [InlineData(true, int.MaxValue)]
        [InlineData(true, 60 * 1000)]
        [InlineData(false, -1)]
        [InlineData(false, int.MaxValue)]
        [InlineData(false, 60 * 1000)]
        public async Task Run_StressTest(bool output, int timeoutMilliseconds)
        {
            // Arrange
            var taskCount = 8;
            var repititions = 200;

            // Act & Assert
            var tasks = Enumerable
                .Range(1, taskCount)
                .Select(x => Task.Run(() =>
                {
                    for (var i = 0; i < repititions; i++)
                    {
                        // Arrange
                        var expected = Guid.NewGuid().ToString();
                        var testCommandPath = Utility.GetTestCommandPath();
                        var arguments = new[] { "output", $"{(output ? 'o' : 'e')}:{expected}" };

                        var command = new Command(testCommandPath, arguments);
                        command.Timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                        var target = new CommandRunner();

                        // Act
                        var result = target.Run(command);

                        // Assert
                        Assert.Equal(CommandStatus.Exited, result.Status);
                        Assert.Equal(0, result.ExitCode);
                        Assert.Contains(expected, output ? result.Output : result.Error);
                    }
                }))
                .ToArray();
            await Task.WhenAll(tasks);
        }

        [Fact]
        public void Run_CanUseSystemEcho()
        {
            // Arrange
            var expected = "echo me this!";
            var command = Utility.GetEchoCommand(expected);

            var target = new CommandRunner();

            // Act
            var result = target.Run(command);

            // Assert
            Assert.Equal(CommandStatus.Exited, result.Status);
            Assert.Equal(0, result.ExitCode);

            Assert.Contains(expected, result.Output);
        }

        [Fact]
        public void Run_AllowsEnvironmentVariablesToBeSet() 
        {
            // Arrange
            var testCommandPath = Utility.GetTestCommandPath();
            var command = new Command(testCommandPath, "dump");
            var environmentKey = "ProcommandTest";
            command.Environment[environmentKey] = Guid.NewGuid().ToString();

            var target = new CommandRunner();

            // Act
            var result = target.Run(command);

            // Assert
            Assert.Equal(CommandStatus.Exited, result.Status);
            Assert.Equal(0, result.ExitCode);

            var dump = JsonConvert.DeserializeObject<JObject>(result.Output);
            var actual = dump["Environment"][environmentKey].Value<string>();
            Assert.Equal(command.Environment[environmentKey], actual);
        }

        [Fact]
        public void Run_UsesProvidedWorkingDirectory()
        {
            // Arrange
            using (var directory = TestDirectory.Create())
            {
                var contents = "Foo" + Environment.NewLine + "Bar";
                var path = Path.Combine(directory, "file.txt");
                File.WriteAllText(path, contents);

                var testCommandPath = Utility.GetTestCommandPath();
                var command = new Command(testCommandPath, "read-file file.txt");
                command.WorkingDirectory = directory;

                var target = new CommandRunner();

                // Act
                var result = target.Run(command);

                // Assert
                Assert.Equal(CommandStatus.Exited, result.Status);
                Assert.Equal(0, result.ExitCode);

                Assert.Equal(contents, result.Output.TrimEnd());
                Assert.Empty(result.Error);
            }
        }

        [Fact]
        public void Run_InterceptsStderrAndStdout()
        {
            // Arrange
            var testCommandPath = Utility.GetTestCommandPath();
            var command = new Command(testCommandPath, "output o:STDOUT e:STDERR");

            var target = new CommandRunner();

            // Act
            var result = target.Run(command);

            // Assert
            Assert.Equal(CommandStatus.Exited, result.Status);
            Assert.Equal(0, result.ExitCode);

            Assert.Equal("STDOUT", result.Output.TrimEnd());
            Assert.Equal("STDERR", result.Error.TrimEnd());
        }

        [Fact]
        public void Run_InterceptsStdoutAndStderrInCorrectOrder()
        {
            // Arrange
            var testCommandPath = Utility.GetTestCommandPath();
            var command = new Command(testCommandPath, "output o:o1 e:e1 e:e2 o:o2 e:e3 o:o3 o:o4");

            var target = new CommandRunner();

            // Act
            var result = target.Run(command);

            // Assert
            Assert.Equal(CommandStatus.Exited, result.Status);
            Assert.Equal(0, result.ExitCode);

            Assert.Equal(7, result.Lines.Count);

            // Sort by type. Since these lines are coming from different streams, it's possible we read them out of
            // order.
            var lines = result.Lines.OrderBy(x => x?.Type).ToList();
            Assert.Equal(new CommandOutputLine(CommandOutputLineType.Out, "o1"), lines[0]);
            Assert.Equal(new CommandOutputLine(CommandOutputLineType.Out, "o2"), lines[1]);
            Assert.Equal(new CommandOutputLine(CommandOutputLineType.Out, "o3"), lines[2]);
            Assert.Equal(new CommandOutputLine(CommandOutputLineType.Out, "o4"), lines[3]);
            Assert.Equal(new CommandOutputLine(CommandOutputLineType.Err, "e1"), lines[4]);
            Assert.Equal(new CommandOutputLine(CommandOutputLineType.Err, "e2"), lines[5]);
            Assert.Equal(new CommandOutputLine(CommandOutputLineType.Err, "e3"), lines[6]);
        }

        [Fact]
        public void Run_TimesOutCommand()
        {
            // Arrange
            var testCommandPath = Utility.GetTestCommandPath();

            var command = new Command(testCommandPath, "wait 100");
            command.Timeout = TimeSpan.FromMilliseconds(99);

            var target = new CommandRunner();

            // Warm up the command
            target.Run(new Command(testCommandPath));

            // Act
            var result = target.Run(command);

            // Assert
            Assert.Equal(CommandStatus.Timeout, result.Status);
            Assert.Equal(-1, result.ExitCode);
            Assert.Equal("About to sleep 100 milliseconds.", result.Output.TrimEnd());
        }

        [Fact]
        public void Run_FailsToStartCommand()
        {
            // Arrange
            var command = new Command(Guid.NewGuid().ToString());

            var target = new CommandRunner();

            // Act
            var result = target.Run(command);

            // Assert
            Assert.Equal(CommandStatus.FailedToStartCommand, result.Status);
            Assert.Equal(-1, result.ExitCode);
            Assert.NotNull(result.Exception);
            Assert.Equal("The system cannot find the file specified", result.Exception.Message);
        }

        [Fact]
        public void Run_SendsInputToStdin()
        {
            // Arrange
            var testCommandPath = Utility.GetTestCommandPath();
            var command = new Command(testCommandPath, "input");
            var expected = "foo" + Environment.NewLine + "bar";
            command.Input = new MemoryStream(Encoding.ASCII.GetBytes(expected));

            var target = new CommandRunner();

            // Act
            var result = target.Run(command);

            // Assert
            Assert.Equal(CommandStatus.Exited, result.Status);
            Assert.Equal(0, result.ExitCode);

            Assert.Equal(expected, result.Output.TrimEnd());
        }

        [Fact]
        public void Run_StdinCanBeUsedForAnInteractiveConsoleApplication()
        {
            // Arrange
            var testCommandPath = Utility.GetTestCommandPath();
            var command = new Command(testCommandPath, "interactive");
            var input = "N" + Environment.NewLine + "Joel" + Environment.NewLine + "fantastic";
            command.Input = new MemoryStream(Encoding.ASCII.GetBytes(input));

            var target = new CommandRunner();

            // Act
            var result = target.Run(command);

            // Assert
            Assert.Equal(CommandStatus.Exited, result.Status);
            Assert.Equal(0, result.ExitCode);

            Assert.Equal(
                "Welcome to a silly interactive thing." + Environment.NewLine +
                "Would you like to quit right now? (Y/N) Great! Let's continue." + Environment.NewLine +
                "What is your name?" + Environment.NewLine +
                "Hello, Joel! How is your day?" + Environment.NewLine +
                "Your day is fantastic? I hope that's a good thing...",
                result.Output.TrimEnd());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(42)]
        public void Run_ReturnsPositiveExitCode(int expected)
        {
            // Arrange
            var testCommandPath = Utility.GetTestCommandPath();
            var command = new Command(testCommandPath, $"exit-code {expected}");

            var target = new CommandRunner();

            // Act
            var result = target.Run(command);

            // Assert
            Assert.Equal(CommandStatus.Exited, result.Status);
            Assert.Equal(expected, result.ExitCode);
        }
    }
}
