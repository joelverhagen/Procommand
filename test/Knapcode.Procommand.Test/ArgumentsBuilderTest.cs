using System;
using System.Collections.Generic;
using Knapcode.Procommand.Test.TestSupport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Knapcode.Procommand.Test
{
    public class ArgumentsBuilderTest
    {
        [Theory]
        [MemberData(nameof(SingleParameters))]
        public void Escape_ReturnsExpectedEscapedSequence(string input, string expected)
        {
            // Arrange & Act
            var actual = ArgumentsBuilder.Escape(input);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Escape_RejectsNullInput()
        {
            // Arrange & Act & Assert
            var actual = Assert.Throws<ArgumentNullException>(() => ArgumentsBuilder.Escape(null));
            Assert.Equal("argument", actual.ParamName);
        }

        [Fact]
        public void Escape_RejectsNullCharacterInput()
        {
            // Arrange & Act & Assert
            var actual = Assert.Throws<ArgumentException>(() => ArgumentsBuilder.Escape("foo \0 bar"));
            Assert.Equal("argument", actual.ParamName);
            Assert.StartsWith("The null character cannot be passed as a command line argument.", actual.Message);
        }

        [Fact]
        public void Build_RejectNullArguments()
        {
            // Arrange
            var target = new ArgumentsBuilder();
            target.Arguments.Add("foo");
            target.Arguments.Add(null);
            target.Arguments.Add("bar");

            // Act & Assert
            var actual = Assert.Throws<InvalidOperationException>(() => target.Build());
            Assert.Equal("An argument cannot be null.", actual.Message);
        }

        [Fact]
        public void Build_UsesConstructorParameters()
        {
            // Arrange
            var target = new ArgumentsBuilder(new[] { "foo", "bar" });
            target.Arguments.Add("baz");

            // Act
            var actual = target.Build();

            // Assert
            Assert.Equal("foo bar baz", actual);
        }

        [Fact]
        public void Build_SplitsArgumentsWithASingleSpace()
        {
            // Arrange
            var target = new ArgumentsBuilder();
            target.Arguments.Add("foo");
            target.Arguments.Add("bar");
            target.Arguments.Add("baz");

            // Act
            var actual = target.Build();

            // Assert
            Assert.Equal("foo bar baz", actual);
        }

        [Fact]
        public void Build_AllowsNoArguments()
        {
            // Arrange
            var target = new ArgumentsBuilder();

            // Act
            var actual = target.Build();

            // Assert
            Assert.Equal(string.Empty, actual);
        }

        [Fact]
        public void Build_CanBeCalledAsAStatic()
        {
            // Arrange
            var input = new[] { "foo", "bar", "baz" };

            // Act
            var actual = ArgumentsBuilder.Build(input);

            // Assert
            Assert.Equal("foo bar baz", actual);
        }

        [Theory]
        [MemberData(nameof(SingleParameters))]
        public void Build_EscapesMultipleParameters(string input, string escaped)
        {
            // Arrange
            var testCommandPath = Utility.GetTestCommandPath();
            var expectedArguments = new[] { "dump", "foo", input, "bar", input, "baz" };
            var target = new ArgumentsBuilder(expectedArguments);
            var runner = new CommandRunner();

            // Act
            var command = new Command(testCommandPath, target.Build());
            var result = runner.Run(command);

            // Assert
            Assert.Equal(CommandStatus.Exited, result.Status);
            Assert.Equal(0, result.ExitCode);

            var output = JsonConvert.DeserializeObject<JObject>(result.Output);
            var actualArguments = output["Arguments"].ToObject<string[]>();
            Assert.Equal(expectedArguments, actualArguments);
        }

        public static IEnumerable<object[]> SingleParameters
        {
            get
            {
                return new[]
                {
                    new object[] { "foo", "foo" },
                    new object[] { "", "\"\"" },
                    new object[] { " ", "\" \"" },
                    new object[] { "          ", "\"          \"" },
                    new object[] { "foo bar", "\"foo bar\"" },
                    new object[] { "'foo bar'", "\"'foo bar'\"" },
                    new object[] { "'foo \" bar'", "\"'foo \\\" bar'\"" },
                    new object[] { "\"foo bar\"", "\"\\\"foo bar\\\"\"" },
                    new object[] { "\"foo bar", "\"\\\"foo bar\"" },
                    new object[] { "'", "'" },
                    new object[] { "\\", "\\" },
                    new object[] { "\\\\", "\\\\" },
                    new object[] { "/", "/" },
                    new object[] { "//", "//" },
                    new object[] { "&", "&" },
                    new object[] { "&&", "&&" },
                    new object[] { "^", "^" },
                    new object[] { "^^", "^^" },
                    new object[] { "-/?=`!@#", "-/?=`!@#" },
                    new object[] { "*/foo.txt", "*/foo.txt" },
                    new object[] { "*\\foo.txt", "*\\foo.txt" },
                    new object[] { "**\\foo.txt", "**\\foo.txt" },
                    new object[] { "<p>some html</p>", "\"<p>some html</p>\"" },
                    new object[] { "\"", "\\\"" },
                    new object[] { "\"\"", "\\\"\\\"" },
                    new object[] { "\"\"\"", "\\\"\\\"\\\"" },
                    new object[] { "\"\"\"\"", "\\\"\\\"\\\"\\\"" },
                    new object[] { "\"\"\"\"\"", "\\\"\\\"\\\"\\\"\\\"" },
                    new object[] { "\a", "\a" },
                    new object[] { "\b", "\b" },
                    new object[] { "\f", "\"\f\"" },
                    new object[] { "\n", "\"\n\"" },
                    new object[] { "\t", "\"\t\"" },
                    new object[] { "\r", "\"\r\"" },
                    new object[] { "\r\n", "\"\r\n\"" },
                    new object[] { "\v", "\"\v\"" }
                };
            }
        }
    }
}
