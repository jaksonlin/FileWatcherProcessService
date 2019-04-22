using System.Collections.Generic;
using System.Security;

namespace FsBaseExecSvc.Interface
{
    interface IServiceProfile
    {
        string Username { get; set; }
        string Domain { get; set; }
        SecureString Pwd { get; set; }
        string SvcInstallMediaLoc { get; set; }
        string ServiceName { get; set; }
        string ServiceBinFullName { get; set; }
        IEnumerable<string> Nodes { get; set; }
        bool VerifyServiceProfileOnNode(string node);
        bool IsUserValidOnAllNodes();
        void LoadProfile();
        string GetNodeServiceNetLocation(string node);
    }
}