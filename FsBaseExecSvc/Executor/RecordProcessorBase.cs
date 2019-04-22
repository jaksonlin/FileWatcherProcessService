using FsBaseExecSvc.Interface;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;

namespace FsBaseExecSvc.Executor
{
    class RecordProcessorBase : IRecordProcessorBase
    {
        private readonly IExecuteLogic logic;
        private readonly IExecuteRecordInfo executeRecordInfo;
        public RecordProcessorBase(IExecuteRecordInfo recordInfo, IExecuteLogic logic)
        {
            this.logic = logic;
            this.executeRecordInfo = recordInfo;
        }

        public string ConfigFile 
        {
            get { return this.executeRecordInfo.ConfigFile; }
        }

        public string GUID
        {
            get { return this.executeRecordInfo.GUID; }
        }

        public string ProcessingLogic()
        {
            IEnumerable<string> requestContent = File.ReadAllLines(this.executeRecordInfo.ConfigFile);
            return this.logic.ProcessingLogic(requestContent);
        }
        
    }
}
