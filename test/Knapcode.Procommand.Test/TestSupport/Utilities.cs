using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Knapcode.Procommand.Test.TestSupport
{
    public static class Utility
    {
        public static Command GetEchoCommand(string echo)
        {
            string fileName;
            var arguments = new List<string>();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fileName = "cmd.exe";
                arguments.Add("/c");
                arguments.Add("echo");
                arguments.Add(echo);
            }
            else
            {
                fileName = "echo";
                arguments.Add(echo);
            }

            var command = new Command(fileName, arguments);
            return command;
        }

        public static string GetTestCommandPath()
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
