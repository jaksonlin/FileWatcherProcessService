using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FsBaseExecSvc.Interface
{
    interface IOrchExecutor
    {
        void OnOrchFileCreated(object sender, FileSystemEventArgs record);
        void Shutdown();
        void ProcessRequest(string fileFullPath);
        void ProcessRcRequest();
    }
}
