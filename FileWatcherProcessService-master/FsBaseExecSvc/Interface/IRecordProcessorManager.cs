using System;
using System.Threading.Tasks;

namespace FsBaseExecSvc.Interface
{
    interface IRecordProcessorManager
    {
        Task BgTask { get; }
        string ConfigFile { get; }
        string EndConfigFile { get; }
        bool Finish { get; }
        string OutputFile { get; }
        string RunningConfigFile { get; }
        string TaskId { get; }
        event EventHandler<string> FinishProcessEventHandler;

        void RunJob();
    }
}