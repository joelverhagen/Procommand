using System.IO;
using System.Linq;

namespace Knapcode.Procommand.Test.TestSupport
{
    public static class Utility
    {
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
