using FsBaseExecSvc.Interface;

namespace FsBaseExecSvc.Executor
{
    class ExecuteRecordInfo : IExecuteRecordInfo
    {
        public string ConfigFile { get; private set; }
        public string GUID { get; private set; }
        public ExecuteRecordInfo(string file, string guid)
        {
            this.ConfigFile = file;
            this.GUID = guid;
        }
    }
}
