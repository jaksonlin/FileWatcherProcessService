namespace FsBaseExecSvc.Interface
{
    interface IRecordProcessorBase
    {
        string ConfigFile { get; }
        string GUID { get; }

        string ProcessingLogic();
    }
}