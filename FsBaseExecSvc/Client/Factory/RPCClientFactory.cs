using FsBaseExecSvc.Client;
using FsBaseExecSvc.Interface;
using Lamar;
using System;
using System.Collections.Generic;
using System.IO;

namespace FsBaseExecSvc.Client.Factory
{
    /// <summary>
    /// factory for returning RPCClient, based on token provided by user.
    /// </summary>
    class RPCClientFactory : IRPCClientFactory
    {
        readonly List<string> srvTypes = new List<string>();
        private readonly IContainer container;

        public RPCClientFactory(IContainer container)
        {
            this.container = container;
        }
        public IFsRPCBase GetRPCObject(string token)
        {
            var provider = this.container.GetInstance<IFileNameProvider>(token);
            using (var nestedContainer = container.GetNestedContainer())
            {
                nestedContainer.Inject(provider);
                IFsRPCBase rpcbase = nestedContainer.GetInstance<IFsRPCBase>();
                return rpcbase;
            } 
        }
    }
}