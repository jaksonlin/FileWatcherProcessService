using DIFacility.SharedLib.Utils;
using FsBaseExecSvc.Executor;
using FsBaseExecSvc.Interface;
using FsBaseExecSvc.Watcher;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FsBaseExecSvc.Hosting
{
    class ServiceFacade : IServiceFacade
    {
        public ILocalCallWatcher InBoxFileWatcher { get; private set; }

        public ILocalCallWatcher RebootWatcher { get; private set; }

        public IOrchExecutor OrchExecutor { get; private set; }

        private volatile bool _stopping = false;
        private readonly IOSHelper oshelper;
        private readonly ILogger<ServiceFacade> _logger;
        private readonly IBoxLocations boxLocations;
        public ServiceFacade(IBoxLocations boxLocations, IOSHelper oshelper, ILogger<ServiceFacade> logger, ILocalCallWatcher inbox, ILocalCallWatcher rcbox, IOrchExecutor orchExecutor)
        {
            this.boxLocations = boxLocations;
            this.oshelper = oshelper;
            this._logger = logger;
            this.InBoxFileWatcher = inbox ?? throw new ArgumentNullException("inbox");
            this.RebootWatcher = rcbox ?? throw new ArgumentNullException("rcbox");
            this.OrchExecutor = orchExecutor ?? throw new ArgumentNullException("orchExecutor");
        }

        public void StartService()
        {
            MoveInBoxJobToRcJob();
            this.OrchExecutor.ProcessRcRequest();
            this.InBoxFileWatcher.OrchFileChangeEventHandler += this.OrchExecutor.OnOrchFileCreated;
            this.InBoxFileWatcher.StartWatching();
            this.RebootWatcher.OrchFileChangeEventHandler += this.OnRestartRequestHappen;
            this.RebootWatcher.StartWatching();
        }
        private void MoveInBoxJobToRcJob()
        {
            DirectoryInfo dirInfo = new DirectoryInfo(boxLocations.MonitoringDir);
            foreach (var file in dirInfo.GetFiles())
            {
                File.Move(file.FullName, Path.Combine(boxLocations.RcDir, file.Name));
            }
        }
        private void OnRestartRequestHappen(object sender, FileSystemEventArgs e)
        {
            if (!_stopping)
            {
                _stopping = true;
                this._logger.LogInformation("Restart server request...");
                this.InBoxFileWatcher.StopWatching();
                this._logger.LogInformation("Watcher stopped.");
                //restart request will need to wait till all things are ok, this is a sync call
                this.OrchExecutor.Shutdown();
                this._logger.LogInformation("Executor stopped. Rebooting server.");
                this.oshelper.Reboot(3);
            }
        }

        public void StopService()
        {
            this._stopping = true;
            this.InBoxFileWatcher.StopWatching();
            //this.RebootWatcher.StopWatching();
        }
    }
}
