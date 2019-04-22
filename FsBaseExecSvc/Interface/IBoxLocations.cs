namespace FsBaseExecSvc.Interface
{
    interface IBoxLocations
    {
        string BaseDirectory { get; }
        string EndDir { get; }
        string MonitoringDir { get; }
        string OutputDir { get; }
        string ProcessRunningDir { get; }
        string RcDir { get; }
        int RPCRestartDelayMs { get; }
        string RunningDir { get; }
        int ServerRestartProcessDelayMs { get; }
    }
}