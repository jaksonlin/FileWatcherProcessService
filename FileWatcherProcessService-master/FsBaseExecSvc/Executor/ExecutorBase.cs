using DIFacility.SharedLib.Utils;
using FsBaseExecSvc.Abstract;
using FsBaseExecSvc.Interface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FsBaseExecSvc.Executor
{
    /// <summary>
    /// manage the life cycle of a record processor, and also a proxy to run the processor
    /// </summary>
    sealed class RecordProcessorManager : IRecordProcessorManager
    {
        public string ConfigFile { get; private set; }
        public string RunningConfigFile { get; private set; }
        public string OutputFile { get; private set; }
        public string EndConfigFile { get; private set; }
        public bool Finish { get; private set; } = false;
        public Task BgTask { get; private set; }
        public event EventHandler<string> FinishProcessEventHandler;
        public string TaskId { get; private set; }
        private readonly IRecordProcessorBase processor;
        private readonly IStringHelper stringHelper;
        private readonly ILogger<IRecordProcessorManager> logger;
        public RecordProcessorManager(
            IBoxLocations boxLocations,
            ILogger<IRecordProcessorManager> logger,
            IRecordProcessorBase processor,
            IStringHelper stringHelper)
        {
            this.stringHelper = stringHelper;
            this.logger = logger;
            this.processor = processor;
            this.TaskId = processor.GUID;
            var fileInfo = new FileInfo(processor.ConfigFile);
            //configs for revealing processing stage
            this.ConfigFile = fileInfo.FullName;
            this.RunningConfigFile = Path.Combine(boxLocations.RunningDir, fileInfo.Name);
            this.EndConfigFile = Path.Combine(boxLocations.EndDir, fileInfo.Name);
            this.OutputFile = Path.Combine(boxLocations.OutputDir, fileInfo.Name);
        }
        //these functions to make sure the event fire only once.
        void WriteByString(string file, string content)
        {
            using (FileStream sw = File.OpenWrite(file))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(content);
                sw.Write(bytes, 0, bytes.Length);
            }
        }
        void WriteByFile(string target, string source)
        {
            using (FileStream sw = File.OpenWrite(target))
            {
                var config = File.ReadAllBytes(source);
                sw.Write(config, 0, config.Length);
            }
        }
        public void RunJob()
        {
            
            this.BgTask = Task.Run(() =>
            {

                try
                {
                    this.logger.LogInformation($@"Processing {ConfigFile}");
                    WriteByFile(RunningConfigFile, ConfigFile);
                    string result = this.processor.ProcessingLogic();
                    WriteByString(OutputFile, result);
                    this.logger.LogInformation($@"Finish processing {ConfigFile}");
                }
                catch (Exception ex)
                {
                    string result = $@"Hit failure when processing {ConfigFile}, {this.stringHelper.GetExceptionDetails(ex)} {Environment.NewLine} this is caused by bug in the implenmetation of {this.GetType().ToString()}";
                    WriteByString(OutputFile, result);
                    this.logger.LogError($@"Hit failure when processing {ConfigFile} {Environment.NewLine} this is caused by bug in the implenmetation of {this.GetType().ToString()}", ex);
                }
                finally
                {
                    if (File.Exists(ConfigFile))
                    {
                        File.Delete(ConfigFile);
                    }
                    if (File.Exists(EndConfigFile))
                    {
                        File.Delete(EndConfigFile);
                    }
                    File.Move(RunningConfigFile, EndConfigFile);
                }
                this.OnRunToFinish();
            }).ContinueWith(t=> {
                if (t.IsFaulted)
                {
                    this.logger.LogError($@"Completed: {t.IsCompleted}, Faluted:{t.IsFaulted}. ", t.Exception);
                }
            });
        }
        private void OnRunToFinish()
        {
            this.Finish = true;
            this.FinishProcessEventHandler?.Invoke(null, TaskId);
        }
    }
}
