using DIFacility.SharedLib.Utils;
using FsBaseExecSvc.Abstract;
using FsBaseExecSvc.Interface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace FsBaseExecSvc.Client
{
    //this is an NT Platform deployment
    class ServiceDeployment : IServiceDeployment
    {
        ILogger<ServiceDeployment> logger;
        IOSHelper helper;
        public ServiceDeployment(ILogger<ServiceDeployment> logger, IOSHelper helper)
        {
            this.logger = logger;
            this.helper = helper;
        }
        public bool DeployService(string node, ServiceProfile profile, bool reInstall)
        {
            var result = DeployOperation(
                node,
                profile.ServiceBinFullName,
                profile.ServiceName,
                profile.Domain,
                profile.Username,
                profile.Pwd,
                profile.SvcInstallMediaLoc,
                reInstall);
            if (result == false)
            {
                this.logger.LogWarning($@"DeployService on {node} failed");
            }
            return result;
        }
        protected bool BringUpStoppedService(string serviceName, string node)
        {
            Thread.Sleep(2000);
            this.helper.StartService(serviceName, node);
            string outStart = this.helper.GetService(serviceName, node);
            if (outStart.Contains("RUNNING"))
            {
                return true;
            }
            else
            {
                this.logger.LogWarning($@"Service start failed on {node}, {outStart}");
                return false;
            }
        }
        protected bool CheckStartPendingService(string serviceName, string node)
        {
            Thread.Sleep(5000);
            string outStart = this.helper.GetService(serviceName, node);
            if (outStart.Contains("RUNNING"))
            {
                return true;
            }
            else
            {
                this.logger.LogWarning($@"Service start failed on {node}, {outStart}");
                return false;
            }
        }

        private bool DeployOperation(string node, string binName, string serviceName, string domain, string user, SecureString pwd, string srcFiles, bool reInstall)
        {
            // net use, otherwise all other commands will fail when the machine is in different domain.
            string drive = binName.Trim().Substring(0, 1);
            if (!this.helper.NetUseSMBDrive(drive, node, domain: domain, user: user, pwd: this.helper.SecureStringToString(pwd)))
            {
                this.logger.LogWarning($@"Net use {node} on drive {drive}: failed; cred incorrect or server not up");
                return false;
            }
            if (!this.helper.IsUserValid(domain, user, pwd, node))
            {
                this.logger.LogWarning("User info is not valid");
                return false;
            }
            var serviceState = this.helper.GetService(serviceName, node);
            if (reInstall)
            {
                return InstallLogic(serviceName, binName, node, domain, user, pwd, srcFiles);
            }
            else
            {
                //password expired, fix using new password
                if (serviceState.Contains("FAILED 1060"))
                {
                    return InstallLogic(serviceName, binName, node, domain, user, pwd, srcFiles);
                }
                //all others are just bring up the down service
                else if (serviceState.Contains("STOPPED") || serviceState.Contains("STOP_PENDING"))
                {
                    return BringUpStoppedService(serviceName, node);
                }
                else if (serviceState.Contains("START_PENDING"))
                {
                    return CheckStartPendingService(serviceName, node);
                }
                else if (serviceState.Contains("RUNNING"))
                {
                    return true;
                }
                else
                {
                    this.logger.LogWarning($@"Fail to contact the server about the service info: {serviceState} ");
                    return false;
                }
            }
        }

        /// <summary>
        /// install/re-installing the service
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="svcBin"></param>
        /// <param name="node"></param>
        /// <param name="domain"></param>
        /// <param name="user"></param>
        /// <param name="pwd"></param>
        /// <param name="srcFiles"></param>
        /// <returns></returns>
        private bool InstallLogic(string serviceName, string svcBin, string node, string domain, string user, SecureString pwd, string srcFiles)
        {
            try
            {
                var serviceState = this.helper.GetService(serviceName, node);
                if (serviceState.Contains("RUNNING") || serviceState.Contains("START_PENDING") || serviceState.Contains("STOP_PENDING"))
                {
                    this.logger.LogInformation($@"Stop service on {node} for re-installing service operation.");
                    var currentState = this.helper.StopService(serviceName, node);
                    if (!currentState)
                    {
                        this.logger.LogWarning($@"Fail to stop the helper for install");
                        return false;
                    }
                }

                (bool ok, string output) result = (false, "");
                var runningDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (svcBin.IndexOf(runningDir, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    this.logger.LogWarning($@"copy service binary ({runningDir}) to the remote server ({node})");
                    //copy the current dir to the remote server exclude the boxes directory
                    result = RemoteCopyServiceFiles(runningDir, Directory.GetParent(svcBin).FullName, node, domain, user, this.helper.SecureStringToString(pwd));
                }
                else
                {
                    this.logger.LogWarning($@"copy service binary ({srcFiles}) to the remote server ({node})");

                    result = RemoteCopyServiceFiles(srcFiles, Directory.GetParent(svcBin).FullName, node, domain, user, this.helper.SecureStringToString(pwd));
                }

                if (result.ok)
                {
                    this.logger.LogInformation("Creating service after recopy copy files...");
                    var rs = this.helper.CreateService(serviceName, node, svcBin, $@"{domain}\{user}", this.helper.SecureStringToString(pwd), @"Tcpip/Dhcp/Dnscache");
                    if (rs.Contains("RUNNING"))
                    {
                        this.logger.LogInformation("Create service success...");
                        return true;
                    }
                    else
                    {
                        this.logger.LogWarning($@"Create service failed on {node}, {rs}");
                        return false;
                    }
                }
                else
                {
                    this.logger.LogWarning($@"remote copy file to node for reinstall {node} failed: {result.output}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $@"Failed in installing the service");
                return false;
            }
        }

        private (bool ok, string output) RemoteCopyServiceFiles(string localDir, string targetDir, string node, string domain, string user, string pwd)
        {
            (bool ok, string output) result = this.helper.RemoteCopyFiles(node, localDir, targetDir, domain: domain, user: user, pwd: pwd);
            var boxesDir = $@"\\{node}\{targetDir.Replace(":", "$")}\boxes";
            if (Directory.Exists(boxesDir))
            {
                Directory.Delete(boxesDir, true);
            }
            return result;
        }

    }
}
