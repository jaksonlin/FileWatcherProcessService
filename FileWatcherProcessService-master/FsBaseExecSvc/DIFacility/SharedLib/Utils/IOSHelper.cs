using System.Collections.Generic;
using System.Security;

namespace DIFacility.SharedLib.Utils
{
    interface IOSHelper
    {
        string CreateService(string serviceName, string svcBin, string svcUser, string svcPwd, string depend);
        string CreateService(string serviceName, string server, string svcBin, string svcUser, string svcPwd, string depend);
        string GetService(string serviceName);
        string GetService(string serviceName, string server);
        string GetServiceImagePath(string serviceName);
        string GetServiceImagePath(string serviceName, string node);
        bool IsUserValid(string domain, string user, SecureString pwd, string node);
        bool NetUseSMBDrive(string driveName, string node, string domain, string user, string pwd);
        (bool result, string output) PureCmdExector(string filename, string arguments, IDictionary<string, string> env = null, string working_dir = "", string usr = "", SecureString pwd = null, string domain = "", bool redirect_input = false, string content_in = "");
        void Reboot(int timeleft);
        (bool result, string details) RemoteCopyFiles(string hostname, string source, string target, string domain, string user, string pwd);
        string SecureStringToString(SecureString value);
        bool StartService(string serviceName, int timeout = 2);
        bool StartService(string serviceName, string server, int timeout = 2);
        bool StopService(string serviceName, int timeout = 2);
        bool StopService(string serviceName, string server, int timeout = 2);
        SecureString StringToSecureString(string value);
    }
}