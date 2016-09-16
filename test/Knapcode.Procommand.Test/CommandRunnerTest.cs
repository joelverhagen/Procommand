using System;
using System.IO;
using System.Linq;
using Knapcode.Procommand.Test.TestSupport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Knapcode.Procommand.Test
{
    public class CommandRunnerTest
    {
        [Fact]
        public void Run_AllowsEnvironmentVariablesToBeSet() 
        {
            // Arrange
            var testCommandPath = GetTestCommandPath();
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
                var contents = "Foo" + Environment.NewLine + "Bar" + Environment.NewLine;
                var path = Path.Combine(directory, "file.txt");
                File.WriteAllText(path, contents);

                var testCommandPath = GetTestCommandPath();
                var command = new Command(testCommandPath, "read-file file.txt");
                command.WorkingDirectory = directory;

                var target = new CommandRunner();

                // Act
                var result = target.Run(command);

                // Assert
                Assert.Equal(CommandStatus.Exited, result.Status);
                Assert.Equal(0, result.ExitCode);

                Assert.Equal(contents, result.Output);
                Assert.Empty(result.Error);
            }
                
        }

        private string GetTestCommandPath()
        {
            var repositoryRoot = GetRepositoryRoot();

            return Path.Combine(
                repositoryRoot,
                "test",
                "Knapcode.Procommand.TestCommand",
                "bin",
                "Debug",
                "net451",
                "win7-x64",
                "Knapcode.Procommand.TestCommand.exe");
        }

        private static string GetRepositoryRoot()
        {
            var current = Directory.GetCurrentDirectory();
            while (current != null &&
                   !Directory.GetFiles(current).Any(x => Path.GetFileName(x) == "Procommand.sln"))
            {
                current = Path.GetDirectoryName(current);
            }

            return current;
        }
    }
}
