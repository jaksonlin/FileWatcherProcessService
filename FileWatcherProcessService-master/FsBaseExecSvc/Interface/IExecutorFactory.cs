using FsBaseExecSvc.Interface;

namespace FsBaseExecSvc.Interface
{
    interface IExecutorFactory
    {
        IRecordProcessorManager GetExecuteRecord(string fileFullPath);
    }
}