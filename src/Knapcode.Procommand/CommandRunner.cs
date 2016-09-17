using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;

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
                var queue = new ConcurrentQueue<CommandOutputLine>();

                process.OutputDataReceived += (sender, e) =>
                {
                    queue.Enqueue(new CommandOutputLine(
                        CommandOutputLineType.Out,
                        e.Data));
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    queue.Enqueue(new CommandOutputLine(
                        CommandOutputLineType.Err,
                        e.Data));
                };

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
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

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
                        exitCode = process.ExitCode;
                    }
                }

                return new CommandResult
                {
                    Command = command,
                    Status = status,
                    Exception = exception,
                    Lines = queue.ToList(),
                    ExitCode = exitCode
                };
            }
        }
    }
}
