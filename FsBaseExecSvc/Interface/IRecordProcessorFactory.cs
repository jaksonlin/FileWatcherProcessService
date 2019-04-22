using FsBaseExecSvc.Abstract;

namespace FsBaseExecSvc.Interface
{
    interface IRecordProcessorFactory
    {
        IRecordProcessorBase GetRecordProcessor(string fileFullPath);
    }

}
