using FsBaseExecSvc.Interface;

namespace FsBaseExecSvc.Client
{
    class RPCHostInfo : IRPCHostInfo
    {
        public  string Node { get; private set; }
        public  string WatchDir { get; private set; }
        public RPCHostInfo(string node, string watchDir)
        {
            this.Node = node;
            this.WatchDir = watchDir;
        }
    }
}
