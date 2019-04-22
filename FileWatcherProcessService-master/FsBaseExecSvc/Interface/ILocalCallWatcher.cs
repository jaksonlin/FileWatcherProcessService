using FsBaseExecSvc.Client;
using System;
using System.IO;

namespace FsBaseExecSvc.Interface
{
    interface ILocalCallWatcher
    {
        event EventHandler<ErrorEventArgs> OrchWatcherErrorEventHandler;

        event EventHandler<FileSystemEventArgs> OrchFileChangeEventHandler;
        string WatchingDir { get; }
        bool StartWatching();
        bool StopWatching();
    }

    interface IRPCWatcher : ILocalCallWatcher
    {
        IRPCHostInfo HostInfo { get; }
    }

}