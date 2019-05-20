using FsBaseExecSvc.Abstract;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;

namespace FsBaseExecSvc.Interface
{
    interface IServiceDeployment
    {
        bool DeployService(string node, ServiceProfile profile, bool reInstall);
    }
}