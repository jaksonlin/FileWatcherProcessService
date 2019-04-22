using FsBaseExecCli.ServerRegistry;
using FsBaseExecSvc.Abstract;
using FsBaseExecSvc.Executor;
using FsBaseExecSvc.Interface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace FsBaseExecCli.UserProcessor
{
    [Processor("itest")]
    class InternalRecordProcessor : IExecuteLogic
    {

        public string ProcessingLogic(IEnumerable<string> requestContent)
        {
            var firstLine = requestContent.FirstOrDefault();
            if (firstLine != null)
            {
                if(firstLine.IndexOf("ECHO=", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return $@"{Environment.MachineName}-{firstLine.ToLower().Replace("echo=", "")}";
                }
                if (firstLine.IndexOf("TIME=", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Thread.Sleep(5000);
                    return $@"{Environment.MachineName}-{firstLine.ToLower().Replace("time=", "after sleep 5 seconds")}";
                }
                else
                {
                    return $@"Default test";
                }
            }
            else
            {
                return $@"[FAIL]The content contains nothing";
            }
        }
    }
}
