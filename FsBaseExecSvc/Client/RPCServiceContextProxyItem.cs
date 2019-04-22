using DIFacility.SharedLib.Utils.Pooling;
using FsBaseExecSvc.Abstract;
using FsBaseExecSvc.DIFactory;
using FsBaseExecSvc.Interface;
using FsBaseExecSvc.Watcher;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace FsBaseExecSvc.Client
{

    class RPCServiceContextProxyItem : IServiceContext
    {
        private IServiceContext innerContext;
        private IContextPool<IServiceContext> pool;
        public RPCServiceContextProxyItem(ILogger<IServiceContext> logger, IContextPool<IServiceContext> pool, IOrchRPCProgressWatcher repository)
        {
            this.pool = pool ?? throw new ArgumentNullException("pool");
            this.innerContext = new RPCServiceContext(logger, repository);
        }

        public void Dispose()
        {
            if (pool.IsDisposed)
            {
                innerContext.Dispose();
            }
            else
            {
                pool.Release(this);
            }
        }

        public void OnProcessFinishRequest(object sender, FileSystemEventArgs args)
        {
            innerContext.OnProcessFinishRequest(sender, args);
        }

        public string ProcessRequest(string node, string filename, string request, ServiceProfile profile)
        {
            return innerContext.ProcessRequest(node, filename, request, profile);
        }

        public string ProcessRequest(string node, string filename, string request, ServiceProfile profile, int timeoutSeconds)
        {
            return innerContext.ProcessRequest(node, filename, request, profile, timeoutSeconds);
        }

        public string ProcessRequestAfterReboot(string node, string filename, string request, ServiceProfile serviceProfile)
        {
            return innerContext.ProcessRequestAfterReboot(node, filename, request, serviceProfile);
        }
    }
}