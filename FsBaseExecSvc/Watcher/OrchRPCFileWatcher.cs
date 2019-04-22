using FsBaseExecSvc.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FsBaseExecSvc.Watcher
{
    class OrchRPCFileWatcher : OrchFileWatcher, IRPCWatcher
    {
        public IRPCHostInfo HostInfo { get; private set; }
        public OrchRPCFileWatcher(Microsoft.Extensions.Logging.ILogger<IRPCWatcher> logger, IRPCHostInfo hostInfo) : base(logger, hostInfo.WatchDir)
        {
            this.HostInfo = hostInfo;
            // for rpc, we only care about the outbox
            this.WatchingDir = Path.Combine(HostInfo.WatchDir, "boxes", "outbox");
        }
    }
}
