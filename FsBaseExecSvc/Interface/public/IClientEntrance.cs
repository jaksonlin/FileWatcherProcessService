using FsBaseExecSvc.Interface;

namespace FsBaseExecSvc.Interface
{
    public interface IClientEntrance
    {
        IFsRPCBase GetRPCClient(string rpcToken);
    }
}