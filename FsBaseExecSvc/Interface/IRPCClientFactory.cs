using FsBaseExecSvc.Client;

namespace FsBaseExecSvc.Interface
{
    interface IRPCClientFactory
    {
        IFsRPCBase GetRPCObject(string token);
    }
}