using FsBaseExecSvc.Abstract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FsBaseExecSvc.Interface
{
    interface IServiceContext : IDisposable
    {
        string ProcessRequest(string node, string filename, string request, ServiceProfile profile);
        string ProcessRequest(string node, string filename, string request, ServiceProfile serviceProfile, int timeoutSeconds);
        void OnProcessFinishRequest(object sender, FileSystemEventArgs args);
        string ProcessRequestAfterReboot(string node, string filename, string request, ServiceProfile serviceProfile);
    }
}
