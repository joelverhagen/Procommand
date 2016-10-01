using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Knapcode.Procommand
{
    public class CommandRunner
    {
        public CommandResult Run(Command command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (command.FileName == null)
            {
                throw new ArgumentException("The FileName on the command must not be null.");
            }

            var process = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = command.WorkingDirectory ?? Directory.GetCurrentDirectory(),
                    FileName = command.FileName,
                    Arguments = command.Arguments,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            if (command.Environment != null)
            {
                foreach (var pair in command.Environment)
                {
#if NET_FRAMEWORK
                    process.StartInfo.EnvironmentVariables[pair.Key] = pair.Value;
#elif NET_CORE
                    process.StartInfo.Environment[pair.Key] = pair.Value;
#endif
                }
            }

            using (process)
            {
                var lines = new ConcurrentQueue<CommandOutputLine>();                
                var status = CommandStatus.Exited;
                Exception exception = null;
                var exitCode = -1;
                bool started;

                try
                {
                    process.Start();
                    started = true;
                }
                catch (Exception e)
                {
                    status = CommandStatus.FailedToStartCommand;
                    exception = e;
                    started = false;
                }

                if (started)
                {
                    var outTask = ConsumeStreamReaderAsync(lines, process.StandardOutput, CommandOutputLineType.Out);
                    var errTask = ConsumeStreamReaderAsync(lines, process.StandardError, CommandOutputLineType.Err);
                    
                    if (command.Input != null)
                    {
                        command.Input.CopyTo(process.StandardInput.BaseStream);
                        process.StandardInput.Dispose();
                    }

                    var exited = process.WaitForExit((int)command.Timeout.TotalMilliseconds);

                    if (!exited)
                    {
                        try
                        {
                            process.Kill();
                            status = CommandStatus.Timeout;
                        }
                        catch (Exception e)
                        {
                            exception = e;
                            status = CommandStatus.FailedToKillAfterTimeout;
                            // Nothing else we can do here.
                        }
                    }
                    else
                    {
                        Task.WaitAll(outTask, errTask);
                        exitCode = process.ExitCode;
                    }
                }

                return new CommandResult
                {
                    Command = command,
                    Status = status,
                    Exception = exception,
                    Lines = lines.ToList(),
                    ExitCode = exitCode
                };
            }
        }

        private async Task ConsumeStreamReaderAsync(
            ConcurrentQueue<CommandOutputLine> lines,
            StreamReader reader,
            CommandOutputLineType type)
        {
            await Task.Yield();

            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                lines.Enqueue(new CommandOutputLine(type, line));
            }
        }
    }
}
