using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FsBaseExecSvc.Interface;
using System.Linq;
using System.Threading;
using FsBaseExecSvc.Abstract;

namespace FsBaseExecSvc.Executor
{
    class OrchExecutor : IOrchExecutor
    {
        private readonly Dictionary<string, IRecordProcessorManager> runningOrch = new Dictionary<string, IRecordProcessorManager>();
        private readonly object locker = new object();
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private Task rcRequestDelayTask;
        readonly IExecutorFactory recordFactory;
        private readonly IBoxLocations boxLocations;
        readonly ILogger<IOrchExecutor> logger;
        public OrchExecutor(ILogger<IOrchExecutor> logger, IExecutorFactory factory, IBoxLocations boxLocations)
        {
            this.boxLocations = boxLocations;
            this.logger = logger;
            this.recordFactory = factory;
        }
        public void ProcessRequest(string fileFullPath)
        {
            string recordId = String.Empty;
            IRecordProcessorManager record = null;
            try
            {
                this.logger.LogDebug($@"Baking request in file {fileFullPath} ...");
                record = recordFactory.GetExecuteRecord(fileFullPath);
                this.logger.LogDebug($@"Request GUID for {fileFullPath} is {record.TaskId} ...");
                recordId = record.TaskId;
                record.FinishProcessEventHandler += this.OnRecordRunFinishes;
                bool addOk = false;
                lock (locker)
                {
                    if (addOk = !this.runningOrch.ContainsKey(record.TaskId) == true)
                    {
                        this.runningOrch.Add(record.TaskId, record);
                    }else
                    {
                        this.logger.LogDebug($@"Request {record.TaskId} for {fileFullPath} conflicting detected  ...");
                        try
                        {
                            var conflictingJob = File.ReadAllText(this.runningOrch[record.TaskId].ConfigFile);
                            this.logger.LogDebug($@"Request {record.TaskId} will not start because of conflicting running task: task config is {conflictingJob}");
                        }
                        catch(Exception ex)
                        {
                            this.logger.LogDebug($@"Request {record.TaskId} conflicting task extraction failed. {ex.Message}");
                        }
                        
                    }
                }
                if (addOk)
                {
                    this.logger.LogDebug($@"Start Request {record.TaskId}");
                    record.RunJob();
                }
                else
                {
                    if (runningOrch.TryGetValue(record.TaskId, out IRecordProcessorManager executeRecord))
                    {
                        this.logger.LogWarning($@"GUID from {record.ConfigFile} conflict with current running record using config file: {executeRecord.ConfigFile}");
                    }
                    else
                    {
                        this.logger.LogWarning($@"Find GUID conflict, but getting the running record fails, GUID {record.TaskId}");
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $@"Fails in processing request {fileFullPath}");
                if(record == null)
                {
                    //place the error into the outbox
                    ErrorOutForNoRecordManager(fileFullPath, ex.Message);
                }
            }
        }
        void WriteByString(string file, string content)
        {
            using (FileStream sw = File.OpenWrite(file))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(content);
                sw.Write(bytes, 0, bytes.Length);
            }
        }
        private void ErrorOutForNoRecordManager(string fullPath, string info)
        {
            FileInfo fileInfo = new FileInfo(fullPath);
            var errorOutFile = Path.Combine(this.boxLocations.OutputDir, fileInfo.Name);
            WriteByString(errorOutFile, info);
        }

        public void OnOrchFileCreated(object sender, FileSystemEventArgs args)
        {
            ProcessRequest(args.FullPath);
        }

        public void ProcessRcRequest()
        {
            rcRequestDelayTask = Task.Run(async () =>
            {
                //add delay for client rpc to rebuild the watcher and events.
                await Task.Delay(boxLocations.ServerRestartProcessDelayMs);
                if (cts.Token.IsCancellationRequested)
                {
                    return;
                }
                DirectoryInfo dirInfo = new DirectoryInfo(boxLocations.RcDir);
                foreach (var file in dirInfo.GetFiles())
                {
                    //the rest of the item will be processed next time
                    if (cts.Token.IsCancellationRequested)
                    {
                        break;
                    }
                    this.ProcessRequest(file.FullName);
                }
            }, cts.Token);
        }

        public void Shutdown()
        {
            this.logger.LogDebug($@"a reboot request occurs, cancelling stoppable job, other running task can still proceed till they are done. Node may reboot later ");
            //when a reboot come before all tasks are consumed. cancel the task for run in next reboot. running task will run till completion.
            cts.Cancel();
            Task.WhenAll(runningOrch.Values.Select(v => v.BgTask)).Wait();
            this.logger.LogDebug($@"Server reboot grancefully, bye.");
        }

        protected void OnRecordRunFinishes(object sender, string result)
        {
            if (runningOrch.TryGetValue(result, out IRecordProcessorManager executeRecord))
            {
                executeRecord.FinishProcessEventHandler -= this.OnRecordRunFinishes;
                runningOrch.Remove(result);
                this.logger.LogDebug($@"Record with guid {result} is removed from repository");
                this.logger.LogInformation($@"Run request from {executeRecord.EndConfigFile} completes. Result : {Environment.NewLine} {File.ReadAllText(executeRecord.OutputFile)} {Environment.NewLine}");
            }
            else
            {
                this.logger.LogInformation($@"Remove record with guid {result} fails, record may have already been removed");
            }
        }
    }
}
