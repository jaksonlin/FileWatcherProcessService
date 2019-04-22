using FsBaseExecSvc.Abstract;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;

namespace FsBaseExecSvc.Interface
{
    interface IServiceDeployment
    {
        Task<bool> DeployService(IEnumerable<string> nodes, string binName, string serviceName, string domain, string user, SecureString pwd, string srcFiles, bool reInstall = false);
        bool DeployService(string node, ServiceProfile profile, bool reInstall);
    }
}