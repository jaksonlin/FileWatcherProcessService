namespace FsBaseExecSvc.Interface
{
    interface IRPCHostInfo
    {
        string Node { get; }
        string WatchDir { get; }
    }
}