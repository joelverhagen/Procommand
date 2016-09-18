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
            var arguments = ArgumentsBuilder.Build(new[]
            {
                "-s", "UseDevelopmentStorage=true",
                "-c", "testcontainer",
                "-f", "foo/bar/{0}.txt",
                "-d", "false",
                "-l", "false",
                "--debug"
            });
        }
    }
}
