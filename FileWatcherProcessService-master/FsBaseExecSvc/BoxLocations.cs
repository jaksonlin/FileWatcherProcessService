using FsBaseExecSvc.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace FsBaseExecSvc
{
    class BoxLocations : IBoxLocations
    {
        public BoxLocations()
        {
            this.ProcessRunningDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            SetupDir();
            SetupRestartSettings();
        }
        void SetupRestartSettings()
        {
            RPCRestartDelayMs = 15000;
            SetupDir();
            ServerRestartProcessDelayMs = RPCRestartDelayMs * 2 + 5000;
        }

        void SetupDir()
        {
            this.BaseDirectory = Path.Combine(this.ProcessRunningDir, "boxes");
            CreateDirIfNotThere(this.BaseDirectory);
            this.MonitoringDir = Path.Combine(this.BaseDirectory, "inbox");
            CreateDirIfNotThere(this.MonitoringDir);
            this.RunningDir = Path.Combine(this.BaseDirectory, "runningbox");
            CreateDirIfNotThere(this.RunningDir);
            this.RcDir = Path.Combine(this.BaseDirectory, "rcbox");
            CreateDirIfNotThere(this.RcDir);
            this.OutputDir = Path.Combine(this.BaseDirectory, "outbox");
            CreateDirIfNotThere(this.OutputDir);
            this.EndDir = Path.Combine(this.BaseDirectory, "endbox");
            CreateDirIfNotThere(this.EndDir);
        }
        void CreateDirIfNotThere(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
        public string ProcessRunningDir { get; private set; }
        public string MonitoringDir { get; private set; }
        public string RunningDir { get; private set; }
        public string RcDir { get; private set; }
        public string OutputDir { get; private set; }
        public string EndDir { get; private set; }
        public int RPCRestartDelayMs { get; private set; }
        public int ServerRestartProcessDelayMs { get; private set; }
        public string BaseDirectory { get; private set; }
    }
}
