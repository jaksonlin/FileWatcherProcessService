namespace FsBaseExecSvc.Interface
{
    interface IServiceFacade
    {
        ILocalCallWatcher InBoxFileWatcher { get; }
        ILocalCallWatcher RebootWatcher { get; }
        IOrchExecutor OrchExecutor { get; }
        void StartService();
        void StopService();
    }
}
