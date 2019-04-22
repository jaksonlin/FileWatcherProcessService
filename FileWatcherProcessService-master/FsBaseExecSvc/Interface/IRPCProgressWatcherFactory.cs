using FsBaseExecSvc.Abstract;

namespace FsBaseExecSvc.Interface
{
    interface IOrchRPCProgressWatcher
    {
        void DeRegisterWatcher(IServiceContext serviceContext);
        bool RegisterWatcher(ServiceProfile profile, string host, IServiceContext serviceContext);
    }
}