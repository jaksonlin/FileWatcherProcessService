namespace FsBaseExecSvc.Interface
{
    public interface IFsRPCBase
    {
        bool IsNodeValid(string node);
        (bool ok, string output) RunAfterRebootOnNode(string node, string contentToRun);
        (bool ok, string output) RunOnNode(string node, string contentToRun, int timeoutSeconds);
        (bool ok, string output) RunOnNode(string node, string contentToRun);
        void UpdateServiceBinary(string node);
        void UpdateServiceCred(string node);
    }
}