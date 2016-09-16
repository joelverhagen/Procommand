namespace Knapcode.Procommand
{
    public enum CommandStatus
    {
        FailedToStartCommand,
        Timeout,
        FailedToKillAfterTimeout,
        Exited
    }
}
