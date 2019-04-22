using FsBaseExecSvc.Interface;
using FsBaseExecSvc.Registry;
using Lamar;
using System;
using System.Collections.Generic;
using System.Text;

namespace FsBaseExecSvc.Client
{
    //for client user do not need to touch into the container.
    public class ClientEntraceFactory
    {
        class ClientEntrance : IClientEntrance
        {
            RPCClientRegistry clientReg;
            IContainer container;
            IRPCClientFactory factory;
            public ClientEntrance(RPCClientRegistry reg)
            {
                this.clientReg = reg;
                this.container = new Container(reg);
                this.factory = container.GetInstance<IRPCClientFactory>();
            }

            public IFsRPCBase GetRPCClient(string rpcToken)
            {
                IFsRPCBase rpcObj = factory.GetRPCObject(rpcToken);
                return rpcObj;
            }
        }
        public static IClientEntrance GetClientEntrance(ITokenProvider reg)
        {
            return new ClientEntrance(new RPCClientRegistry(reg));
        }
    }
    
}
