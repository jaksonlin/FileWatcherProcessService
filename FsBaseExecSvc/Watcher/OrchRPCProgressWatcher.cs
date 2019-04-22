using FsBaseExecSvc.Abstract;
using FsBaseExecSvc.Client;
using FsBaseExecSvc.Interface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FsBaseExecSvc.DIFactory
{
    /// <summary>
    /// a reposiotry maintaining a set of IRPCWatcher for doing RPC call; 
    /// after an RPC is sent to the server,this will help monitoring the target server's working progress
    /// Container should include RPCClientRegistry for runtime injection
    /// </summary>
    class OrchRPCProgressWatcher : IOrchRPCProgressWatcher
    {
        private readonly List<IRPCWatcher> watchers = new List<IRPCWatcher>();
        private readonly List<Task> restartWorkerList = new List<Task>();
        private readonly object locker = new object();
        private readonly Lamar.IContainer factoryContainer;
        private readonly ILogger<IOrchRPCProgressWatcher> logger;
        public OrchRPCProgressWatcher(Lamar.IContainer container, ILogger<IOrchRPCProgressWatcher> logger)
        {
            this.factoryContainer = container;
            this.logger = logger;
        }

        public void DeRegisterWatcher(IServiceContext serviceContext)
        {
            foreach (var watcher in watchers)
            {
                watcher.OrchFileChangeEventHandler -= serviceContext.OnProcessFinishRequest;
            }
        }

        public bool RegisterWatcher(ServiceProfile profile, string host, IServiceContext serviceContext)
        {
            lock (locker)
            {
                IRPCWatcher watcher = watchers.Find(w => w.HostInfo.Node.Equals(host, StringComparison.OrdinalIgnoreCase));
                if (watcher == null)
                {
                    this.logger.LogDebug($@"setting up an Remote fs watcher for {host}...");
                    var currentHostInfo = new RPCHostInfo(host, profile.GetNodeServiceNetLocation(host));
                    this.logger.LogDebug($@"targeting {currentHostInfo.WatchDir} on server {host}...");
                    using (var subContainer = this.factoryContainer.GetNestedContainer())
                    {
                        subContainer.Inject<IRPCHostInfo>(currentHostInfo);
                        watcher = subContainer.GetInstance<IRPCWatcher>();
                        watcher.OrchWatcherErrorEventHandler += HandleWatcherFailure;
                        this.logger.LogDebug($@"{host} watchdog setup ok ");
                        var result = watcher.StartWatching();
                        if (result)
                        {
                            this.logger.LogDebug($@"{host} watchdog working...");
                            watchers.Add(watcher);
                        }
                        else
                        {
                            this.logger.LogError($@"{host} watchdog setup ok but failed to start");
                            return false;
                        }
                    }
                }
                watcher.OrchFileChangeEventHandler += serviceContext.OnProcessFinishRequest;
                return true;
            }
        }

        private void HandleWatcherFailure(object sender, ErrorEventArgs args)
        {
            IRPCWatcher watcher = sender as IRPCWatcher;
            if (watcher != null)
            {
                logger.LogWarning($@"Restart watcher on {watcher.WatchingDir}");
                PushToRestart(watcher);
            }
        }

        private void PushToRestart(ILocalCallWatcher watcher)
        {
            var task = RestartWatcherLogic(watcher);
            restartWorkerList.Add(task);
            task.ContinueWith(t =>
            {
                if (t.IsCompleted)
                {
                    logger.LogWarning($@"Watcher for dir {watcher.WatchingDir} restart ok");
                    restartWorkerList.Remove(task);
                }
            });
        }

        private async Task RestartWatcherLogic(ILocalCallWatcher watcher)
        {
            bool result = false;
            while (!result)
            {
                await Task.Delay(15000);
                result = watcher.StartWatching();
            }
        }
    }
}
