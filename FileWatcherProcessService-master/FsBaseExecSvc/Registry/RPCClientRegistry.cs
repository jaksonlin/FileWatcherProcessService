using DIFacility.SharedLib.Utils.Pooling;
using DIFacility.SharedRegistry;
using FsBaseExecSvc.Abstract;
using FsBaseExecSvc.Client;
using FsBaseExecSvc.Client.Factory;
using FsBaseExecSvc.DIFactory;
using FsBaseExecSvc.Executor;
using FsBaseExecSvc.Interface;
using FsBaseExecSvc.Watcher;
using Lamar;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security;

namespace FsBaseExecSvc.Registry
{
    /// <summary>
    /// Client registry, user should provide ITokenProvider for registry the string token
    /// </summary>
    class RPCClientRegistry : ServiceRegistry
    {
        private readonly ITokenProvider tokenRecorder;

        public RPCClientRegistry(ITokenProvider tokenRecorder)
        {
            this.tokenRecorder = tokenRecorder;
            this.IncludeRegistry<CommonRegistry>();

            this.SetupFileNameProviders();
            //bunch of code for service deployment
            For<IServiceDeployment>().Use<ServiceDeployment>();
            //the target node information is runtime determined
            this.Injectable<IRPCHostInfo>();
            //when the file is written on the listener directory, this repository will start cooresponding linstener on the outbox for the result of processing.
            For<IOrchRPCProgressWatcher>().Use<OrchRPCProgressWatcher>().Singleton();
            //this is the item IOrchRPCProgressWatcher will use to watch the outbox of a target server through SMB path
            For<IRPCWatcher>().Use<OrchRPCFileWatcher>();
            
            //a resource pool for pooling RPC request
            For<IContextPool<FsBaseExecSvc.Interface.IServiceContext>>().Use(
                c => {
                    var factory = c.GetInstance<IOrchRPCProgressWatcher>();
                    var logger = c.GetInstance<ILogger<FsBaseExecSvc.Interface.IServiceContext>>();
                    return new ContextPool<FsBaseExecSvc.Interface.IServiceContext>(
                        10,
                        p => new RPCServiceContextProxyItem(logger, p, factory),
                        LoadingMode.LazyExpanding,
                        AccessMode.Circular);
                }
            ).Singleton();
            //the rpc client user will use.
            For<IFsRPCBase>().Use<FsRPCBase>();
            //factory for user to find the corresponding rpc client.
            For<IRPCClientFactory>().Use<RPCClientFactory>();
        }

       
        private void SetupFileNameProviders()
        {
            var userDefinedNames = this.tokenRecorder.GetClientTokenNames();
            foreach (var name in userDefinedNames)
            {
                For<IFileNameProvider>().Use<FileNameProvider>()
                        .Ctor<string>("name").Is(name).Named(name);
            }
            For<ServiceProfile>().Use<SimpleServiceProfile>().Singleton();

            //Record Provider, providing filename for client to write on the remote server
            this.Injectable<IFileNameProvider>();
        }
    }
}
