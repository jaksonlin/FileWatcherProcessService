using DIFacility.SharedLib.Utils;
using DIFacility.SharedLib.Utils.Pooling;
using FsBaseExecSvc.Abstract;
using FsBaseExecSvc.Interface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace FsBaseExecSvc.Client
{
    sealed class FsRPCBase : IFsRPCBase
    {
        readonly IContextPool<IServiceContext> contextPool;
        readonly ILogger<FsRPCBase> logger;
        readonly IServiceDeployment serviceDeployment;
        readonly ServiceProfile profile;
        readonly IFileNameProvider provider;
        readonly IOSHelper oshelper;
        public FsRPCBase(
            ServiceProfile profile, 
            IServiceDeployment serviceDeployment, 
            IFileNameProvider provider,
            IContextPool<FsBaseExecSvc.Interface.IServiceContext> contextPool,
            ILogger<FsRPCBase> logger,
            IOSHelper oSHelper)
        {
            this.contextPool = contextPool;
            this.profile = profile;
            this.logger = logger;
            this.serviceDeployment = serviceDeployment;
            this.provider = provider;
            this.oshelper = oSHelper;
        }
        
        public (bool ok, string output) RunOnNode(string node, string contentToRun)
        {
            try
            {
                using (IServiceContext context = contextPool.Acquire())
                {
                    this.logger.LogDebug($@"[{this.GetHashCode()}]Starting to run job on node {node}, job content {Environment.NewLine}{contentToRun}{Environment.NewLine}");
                    string fileName = provider.GetFileName();
                    this.logger.LogDebug($@"[{this.GetHashCode()}]The file for remote to bake is {fileName}, start now!");
                    string result = context.ProcessRequest(node, fileName, contentToRun, profile);
                    this.logger.LogDebug($@"[{this.GetHashCode()}]Request comes back with payload: {result}");
                    if (result.IndexOf("[ORCH-ERR]") >= 0)
                    {
                        return (false, result);
                    }else
                    {
                        return (true, result);
                    }
                }
            }
            catch (InvalidOperationException)
            {
                return (false, $@"{node} not in the profile or more than one element matches {node}");
            }
            catch (Exception ex)
            {
                return (false, $@"Hit exception when processing {contentToRun} on {node} : {ex.Message}");
            }
        }

        public (bool ok, string output) RunOnNode(string node, string contentToRun, int timeoutSeconds)
        {
            try
            {
                using (IServiceContext context = contextPool.Acquire())
                {
                    this.logger.LogDebug($@"[{this.GetHashCode()}]Starting to run job on node {node}, job content {Environment.NewLine}{contentToRun}{Environment.NewLine}");
                    string fileName = provider.GetFileName();
                    this.logger.LogDebug($@"[{this.GetHashCode()}]The file for remote to bake is {fileName}, start now! and will timeout in {timeoutSeconds}s");
                    string result = context.ProcessRequest(node, fileName, contentToRun, profile, timeoutSeconds);
                    this.logger.LogDebug($@"[{this.GetHashCode()}]Request comes back with payload: {result}");
                    if (result.IndexOf("[ORCH-ERR]") >= 0)
                    {
                        return (false, result);
                    }
                    else
                    {
                        return (true, result);
                    }
                }
            }
            catch (InvalidOperationException)
            {
                return (false, $@"{node} not in the profile or more than one element matches {node}");
            }
            catch (Exception ex)
            {
                return (false, $@"Hit exception when processing {contentToRun} on {node} : {ex.Message}");
            }
        }
        public (bool ok, string output) RunAfterRebootOnNode(string node, string contentToRun)
        {
            try
            {
                using (IServiceContext context = contextPool.Acquire())
                {
                    this.logger.LogDebug($@"[{this.GetHashCode()}]This is a request to ask server {node} to reboot and then run {Environment.NewLine}{contentToRun}{Environment.NewLine}");
                    string fileName = provider.GetFileName();
                    this.logger.LogDebug($@"[{this.GetHashCode()}]The file for remote to bake after reboot is {fileName}, reboot now!");
                    string result = context.ProcessRequestAfterReboot(node, fileName, contentToRun, profile);
                    this.logger.LogDebug($@"[{this.GetHashCode()}]The run after reboot job completes, with result {result}");
                    if (result.IndexOf("[ORCH-ERR]") >= 0)
                    {
                        return (false, result);
                    }
                    else
                    {
                        return (true, result);
                    }
                }
            }
            catch (InvalidOperationException)
            {
                return (false, $@"{node} not in the profile or more than one element matches {node}");
            }
            catch (Exception ex)
            {
                return (false, $@"Hit exception when processing {contentToRun} on {node} : {ex.Message}");
            }
        }

        public void UpdateServiceCred(string node)
        {
            this.logger.LogDebug($@"[{this.GetHashCode()}]Reloading the profile...");
            //reload profile
            profile.LoadProfile();
            this.logger.LogDebug($@"[{this.GetHashCode()}]Reinstalling the service on {node}...");
            serviceDeployment.DeployService(node, profile, reInstall: true);
        }

        public void UpdateServiceBinary(string node)
        {
            //reload profile
            profile.LoadProfile();
            var localImagePath = profile.SvcInstallMediaLoc;
            var remoteImagePath = this.oshelper.GetServiceImagePath(profile.ServiceBinFullName, node);
            if (string.IsNullOrEmpty(remoteImagePath))
            {
                this.logger.LogDebug($@"[{this.GetHashCode()}]{node} does not have {remoteImagePath}, let me fix it now ...");
                //if remoteImagePath being empty due to no net use and return fail, the net use will be called inside here
                var result = serviceDeployment.DeployService(node, profile, reInstall: false);
                if (result)
                {
                    this.logger.LogDebug($@"[{this.GetHashCode()}]{node} service fixed now.");
                }
                else
                {
                    this.logger.LogDebug($@"[{this.GetHashCode()}]{node} service oops");
                }
            }
            else
            {
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(localImagePath);
                FileVersionInfo fileVersionRemote = FileVersionInfo.GetVersionInfo(remoteImagePath);
                if (fileVersionInfo.IsNewer(fileVersionRemote))
                {
                    this.logger.LogDebug($@"[{this.GetHashCode()}]Newer version of image file found {localImagePath}, updating the files on {node}");
                    var result = serviceDeployment.DeployService(node, profile, reInstall: false);
                    if (result)
                    {
                        this.logger.LogDebug($@"[{this.GetHashCode()}]{node} service fixed now.");
                    }
                    else
                    {
                        this.logger.LogDebug($@"[{this.GetHashCode()}]{node} service oops");
                    }
                }
            }
        }

        public bool IsNodeValid(string node)
        {
            return profile.IsUserValidOnNode(node);
        }

    }
}
