using System;
using System.Collections.Generic;
using System.Text;
using Serilog;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Linq;
using Lamar;
using FsBaseExecSvc.Interface;
using FsBaseExecSvc.Abstract;

namespace FsBaseExecSvc.Executor
{
    /// <summary>
    /// based on user defined format, tell the server how to process the file
    /// </summary>
    class ExecuteRecordFactory : IRecordProcessorFactory
    {
        readonly string orchFileNameFormat = @"orch_(.*?)_(.*?)\.txt"; // orch_recordtype_guid.txt
        private readonly IContainer container;

        public ExecuteRecordFactory(IContainer container)
        {
            this.container = container;
        }

        public IRecordProcessorBase GetRecordProcessor(string fileFullPath)
        {
            FileInfo fileInfo = new FileInfo(fileFullPath);
            Match match = Regex.Match(fileInfo.Name, orchFileNameFormat);
            if (match.Success)
            {
                if (match.Groups.Count == 3)
                {
                    var record = GetExecuteRecord(file: fileInfo.FullName, token: match.Groups[1].Value, guid: match.Groups[2].Value);
                    if (record != null)
                    {
                        return record;
                    }
                    else
                    {
                        throw new Exception($@"Current service not support running with Execute Record of type {match.Groups[0].Value}");
                    }
                }
                else
                {
                    throw new Exception($@"{fileInfo.FullName} config file match but group info retrieve fails. Number of Groups: {match.Groups.Count}");
                }
            }
            else
            {
                throw new Exception($@"{fileInfo.FullName} config file not of format orch_recordtype_guid_validcasename.txt");
            }
        }
        protected IRecordProcessorBase GetExecuteRecord(string file, string token, string guid)
        {
            //IExecuteRecordInfo is not to revealed to user
            IExecuteRecordInfo item = new ExecuteRecordInfo(file, guid);
            //IExecuteLogic is user defined
            IExecuteLogic executeLogic = container.GetInstance<IExecuteLogic>(token);
            using (var subContainer = container.GetNestedContainer())
            {
                subContainer.Inject<IExecuteRecordInfo>(item);
                subContainer.Inject<IExecuteLogic>(executeLogic);
                return subContainer.GetInstance<IRecordProcessorBase>("forinject");
            }
        }
    }
}
