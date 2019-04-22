using FsBaseExecSvc.Abstract;
using FsBaseExecSvc.Interface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace FsBaseExecSvc.Client
{
    class RPCServiceContext : IServiceContext
    {
        private readonly AutoResetEvent sync = new AutoResetEvent(false);
        private readonly object CallLocker = new object();
        private readonly IOrchRPCProgressWatcher repository;
        private readonly ILogger<IServiceContext> logger;
        public RPCServiceContext(ILogger<IServiceContext> logger, IOrchRPCProgressWatcher repository)
        {
            this.logger = logger;
            this.repository = repository;
        }

        private string currentFile = string.Empty;
        public string ProcessRequest(string node, string filename, string request, ServiceProfile serviceProfile)
        {
            return PutRequestOnBox(
                node,
                filename,
                request,
                serviceProfile,
                Path.Combine(Path.Combine(serviceProfile.GetNodeServiceNetLocation(node), "boxes", "inbox"), filename));
        }

        public string ProcessRequest(string node, string filename, string request, ServiceProfile serviceProfile, int timeoutSeconds)
        {
            return PutRequestOnBox(
                node,
                filename,
                request,
                serviceProfile,
                Path.Combine(Path.Combine(serviceProfile.GetNodeServiceNetLocation(node), "boxes", "inbox"), filename),
                timeoutSeconds);
        }
        public string ProcessRequestAfterReboot(string node, string filename, string request, ServiceProfile serviceProfile)
        {
            return PutRequestOnBox(
                node,
                filename,
                request,
                serviceProfile,
                Path.Combine(Path.Combine(serviceProfile.GetNodeServiceNetLocation(node), "boxes", "rcbox"), filename));
        }

        private string PutRequestOnBox(string node, string filename, string request, ServiceProfile serviceProfile, string fileInBox)
        {
            lock (CallLocker)
            {
                this.logger.LogDebug($@"Start checking {filename} request into {node}");
                if (!serviceProfile.VerifyServiceProfileOnNode(node))
                {
                    this.logger.LogDebug($@"Put Request on {node} fail, either {node} is not up or server-client profile incorrect");
                    return $"[ORCH-ERR]Put Request on {node} fail, either {node} is not up or profile incorrect.";
                }
                var outBoxFile = Path.Combine(Path.Combine(serviceProfile.GetNodeServiceNetLocation(node), "boxes", "outbox"), filename);
                WriteFileRequest(node, filename, request, serviceProfile, fileInBox);
                this.logger.LogDebug($@"Request {filename} is written to {node}, and will wait till the server process the request");
                sync.WaitOne();
                return File.ReadAllText(outBoxFile, Encoding.UTF8);
            }
        }

        private void WriteFileRequest(string node, string filename, string request, ServiceProfile serviceProfile, string fileInBox)
        {
            this.repository.RegisterWatcher(serviceProfile, node, this);
            currentFile = filename;
            using (FileStream sw = File.OpenWrite(fileInBox))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(request);
                sw.Write(bytes, 0, bytes.Length);
            }
        }
        private string PutRequestOnBox(string node, string filename, string request, ServiceProfile serviceProfile, string fileInBox, int timeoutSeconds)
        {
            lock (CallLocker)
            {
                this.logger.LogDebug($@"Start checking {filename} request into {node}");
                if (!serviceProfile.VerifyServiceProfileOnNode(node))
                {
                    this.logger.LogDebug($@"Put Request on {node} fail, either {node} is not up or server-client profile incorrect");
                    return $"[ORCH-ERR]Put Request on {node} fail, either {node} is not up or profile incorrect.";
                }
                var outBoxFile = Path.Combine(Path.Combine(serviceProfile.GetNodeServiceNetLocation(node), "boxes", "outbox"), filename);
                WriteFileRequest(node, filename, request, serviceProfile, fileInBox);
                this.logger.LogDebug($@"Request {filename} is written to {node}, and will timeout in {timeoutSeconds} seconds");
                if (sync.WaitOne(timeoutSeconds * 1000))
                {
                    this.logger.LogDebug($@"Request {filename} comes back, returning result");
                    return File.ReadAllText(outBoxFile, Encoding.UTF8);
                }
                else
                {
                    this.logger.LogDebug($@"Request {filename} timeout  ({timeoutSeconds})");
                    return $@"[ORCH-ERR]the operation time out ({timeoutSeconds})";
                }
                
            }
        }

        public void OnProcessFinishRequest(object sender, FileSystemEventArgs args)
        {
            if (!string.IsNullOrEmpty(currentFile) && currentFile.Equals(args.Name))
            {
                this.logger.LogDebug($@"Result for  {args.Name} comes back");
                try
                {
                    sync.Set();
                }catch(Exception ex)
                {
                    this.logger.LogError(ex, 
                        $"set the event to signal failed, but this error is consumed. Report to dev only if you see RPC call hung in waiting result from {args.Name}");
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                currentFile = string.Empty;
                this.repository.DeRegisterWatcher(this);
            }
        }
    }
}
