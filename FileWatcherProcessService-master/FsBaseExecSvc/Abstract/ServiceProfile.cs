using DIFacility.SharedLib.Utils;
using FsBaseExecSvc.Interface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;

namespace FsBaseExecSvc.Abstract
{
    abstract class ServiceProfile
    {
        protected readonly IOSHelper helper;
        protected readonly ILogger<ServiceProfile> logger;
        protected readonly IServiceDeployment serviceDeployment;
        public ServiceProfile(ILogger<ServiceProfile> logger, IServiceDeployment serviceDeployment
            ,IOSHelper oshelper)
        {
            this.helper = oshelper;
            this.logger = logger;
            this.serviceDeployment = serviceDeployment;
            this.LoadProfile();
        }
        public string Username { get; set; }
        public string Domain { get; set; }
        public SecureString Pwd { get; set; }
        public string SvcInstallMediaLoc { get; set; }
        public string ServiceName { get; set; }
        public string ServiceBinFullName { get; set; }
        public virtual bool VerifyServiceProfileOnNode(string node)
        {
            
            var svcImage = this.helper.GetServiceImagePath(ServiceName, node);
            if (!string.IsNullOrEmpty(svcImage))
            {
                if (!svcImage.Equals(ServiceBinFullName, StringComparison.OrdinalIgnoreCase)
                    ||
                    this.ServiceUserPwdChanged(svcImage, node))
                {
                    this.logger.LogWarning("ReInstall service due to profile content not match");
                    if (!serviceDeployment.DeployService(node, this, true))
                    {
                        this.logger.LogWarning("[VerifyServiceProfileOnNode] Service reinstallation for fix profile mismatch failed");
                        return false;
                    }
                }
                else
                {
                    var remoteLoc = $@"\\{node}\{svcImage.Replace(":", "$").Replace("exe","dll")}";
                    var svcExeNameFileInfo = new FileInfo(remoteLoc);
                    var localMedia = Path.Combine(SvcInstallMediaLoc, svcExeNameFileInfo.Name.Replace("exe","dll"));
                    FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(localMedia);
                    FileVersionInfo fileVersionRemote = FileVersionInfo.GetVersionInfo(remoteLoc);
                    if (fileVersionInfo.IsNewer(fileVersionRemote))
                    {
                        this.logger.LogInformation($@"Update {remoteLoc} from {fileVersionRemote.FileVersion} to version {fileVersionInfo.FileVersion}, using local media {localMedia}");
                        var result = serviceDeployment.DeployService(node, this, reInstall: true);
                        if (result)
                        {
                            this.logger.LogInformation($@"Update completes");
                        }
                    }
                }
            }
            else
            {
                if (!serviceDeployment.DeployService(node, this, false))
                {
                    this.logger.LogWarning("[VerifyServiceProfileOnNode] Service installation failed, either cred not correct or server not up");
                    return false;
                }
            }
            return true;
        }
        public virtual bool IsUserValidOnNode(string node)
        {
            return this.helper.IsUserValid(Domain, Username, Pwd, node);
        }
        public virtual bool ServiceUserPwdChanged(string imagePath, string node)
        {
            FileInfo imageInfo = new FileInfo(imagePath);
            string imageDir = imageInfo.DirectoryName;
            string remoteProfile = $@"\\{node}\{imageDir.Replace(':', '$')}\profile.ini";
            string oldConfig = File.ReadAllText(remoteProfile);
            if (oldConfig.Contains($"PASSWD={this.helper.SecureStringToString(this.Pwd)}"))
            {
                return false;
            }else
            {
                return true;
            }
        }

        public abstract void LoadProfile();

        /// <summary>
        /// should return the network path location of the service running, not local fs location
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public abstract string GetNodeServiceNetLocation(string node);
    }

    class SimpleServiceProfile : ServiceProfile
    {
        public SimpleServiceProfile(ILogger<ServiceProfile> logger,
            IServiceDeployment serviceDeployment, IOSHelper oshelper) : base(logger, serviceDeployment, oshelper)
        {
        }

        public override string GetNodeServiceNetLocation(string node)
        {
            var parent = Directory.GetParent(this.ServiceBinFullName).FullName;
            return $@"\\{node}\{parent.Replace(":", "$")}";
        }
        readonly string runningDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().FullName);
        public override void LoadProfile()
        {
            var content = File.ReadAllLines(Path.Combine(this.runningDir, "profile.ini"));

            foreach (var lines in content)
            {
                switch (lines)
                {
                    case var line when line.StartsWith("DOMAIN="):
                        this.Domain = line.Replace("DOMAIN=", "");
                        break;
                    case var line when line.StartsWith("USERNAME="):
                        this.Username = line.Replace("USERNAME=", "");
                        break;
                    case var line when line.StartsWith("PASSWD="):
                        this.Pwd = this.helper.StringToSecureString(line.Replace("PASSWD=", ""));
                        break;
                    default:
                        Debug.Fail($@"not valid content in profile.ini");
                        break;
                }
                this.ServiceName = "FileWatcherProcessService2";
                this.SvcInstallMediaLoc = Path.Combine(this.runningDir, this.ServiceName);
                this.ServiceBinFullName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), this.ServiceName, "FileWatcherProcessService.exe");
            }
        }
    }
}
