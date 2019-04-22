using FsBaseExecSvc.Abstract;
using FsBaseExecSvc.Interface;
using Lamar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace FsBaseExecSvc.Executor
{
    class ExecutorFactory : IExecutorFactory
    {
        protected readonly IContainer container;
        protected IRecordProcessorFactory recordProcessorFactory;
        public ExecutorFactory(IContainer container, IRecordProcessorFactory factory)
        {
            this.container = container;
            this.recordProcessorFactory = factory;
        }

        public IRecordProcessorManager GetExecuteRecord(string fileFullPath)
        {
            IRecordProcessorBase processor = recordProcessorFactory.GetRecordProcessor(fileFullPath);
            using (var subContainer = container.GetNestedContainer())
            {
                subContainer.Inject<IRecordProcessorBase>(processor);
                return subContainer.GetInstance<IRecordProcessorManager>();
            }
        }
    }

}
