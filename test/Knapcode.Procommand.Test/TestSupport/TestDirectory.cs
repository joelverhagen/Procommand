using System;
using System.IO;

namespace Knapcode.Procommand.Test.TestSupport
{
    public class TestDirectory : IDisposable
    {
        private TestDirectory(string directory)
        {
            FullPath = directory;
        }

        private string FullPath { get; }

        public static implicit operator string(TestDirectory directory)
        {
            return directory.FullPath;
        }

        public static TestDirectory Create()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "Knapcode.Procommand.Test",
                Path.GetRandomFileName());

            Directory.CreateDirectory(directory);

            return new TestDirectory(directory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(FullPath, recursive: true);
            }
            catch
            {
                // Ignore failures.
            }
        }
    }
}
