using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;
using FsBaseExecSvc.Interface;

namespace FsBaseExecSvc.Watcher
{
    class OrchFileWatcher : ILocalCallWatcher
    {
        protected FileSystemWatcher Watcher { get; set; }
        public string WatchingDir { get; protected set; }
        private event EventHandler<FileSystemEventArgs> orchFileChangeEventHandler;
        private event EventHandler<ErrorEventArgs> orchWatcherErrorEventHandler;

        public event EventHandler<FileSystemEventArgs> OrchFileChangeEventHandler
        {
            add
            {
                if (orchFileChangeEventHandler == null || !orchFileChangeEventHandler.GetInvocationList().Contains(value))
                {
                    orchFileChangeEventHandler += value;
                }
            }
            remove
            {
                orchFileChangeEventHandler -= value;
            }
        }
        public event EventHandler<ErrorEventArgs> OrchWatcherErrorEventHandler
        {
            add
            {
                if (orchFileChangeEventHandler == null || !orchFileChangeEventHandler.GetInvocationList().Contains(value))
                {
                    orchWatcherErrorEventHandler += value;
                }
            }
            remove
            {
                orchWatcherErrorEventHandler -= value;
            }
        }
        protected void OnBackFileChanged(FileSystemEventArgs e)
        {
            this.orchFileChangeEventHandler?.Invoke(this, e);
        }
        readonly ILogger<ILocalCallWatcher> logger;
        public OrchFileWatcher(ILogger<ILocalCallWatcher> logger, string watchDir)
        {
            this.logger = logger;
            this.WatchingDir = watchDir;
            if (!Directory.Exists(watchDir))
            {
                this.logger.LogDebug($@"Creating watchDir {watchDir} ...");
                Directory.CreateDirectory(watchDir);
            }
        }

        public bool StartWatching()
        {
            try
            {
                if (!Directory.Exists(this.WatchingDir))
                {
                    Directory.CreateDirectory(this.WatchingDir);
                }
                this.Watcher = new FileSystemWatcher();
                this.Watcher.Path = this.WatchingDir;
                this.Watcher.NotifyFilter = NotifyFilters.LastWrite;
                this.Watcher.Filter = "orch_*.txt";
                this.Watcher.Changed += new FileSystemEventHandler(OnChanged);
                this.Watcher.Error += new ErrorEventHandler(OnError);
                this.Watcher.EnableRaisingEvents = true;
                this.logger.LogDebug($@"Starting Watching {WatchingDir} ...");
                return true;
            }
            catch (Exception ex)
            {
                this.logger.LogError($@"Start watching on {WatchingDir} failed", ex);
            }
            return false;
        }

        public bool StopWatching()
        {
            try
            {
                this.Watcher.EnableRaisingEvents = false;
                return true;
            }
            catch (Exception ex)
            {
                this.logger.LogError($@"Stop watching on {WatchingDir} failed", ex);
            }
            return false;
        }

        protected virtual void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                this.logger.LogDebug($@"Captured matching file changed {e.FullPath} ...");
                this.OnBackFileChanged(e);
            }
        }
        protected virtual void OnError(object sender, ErrorEventArgs e)
        {
            this.logger.LogError($@"Hit error on watching dir {this.WatchingDir}", e.GetException());
            this.orchWatcherErrorEventHandler?.Invoke(this, e);
        }
    }
}
