using FsBaseExecSvc.Executor;
using FsBaseExecSvc.Watcher;
using FsBaseExecSvc.Interface;
using Lamar;
using System;
using System.Collections.Generic;
using System.Text;
using FsBaseExecSvc.Abstract;
using FsBaseExecSvc.Hosting;
using DIFacility.SharedRegistry;

namespace FsBaseExecSvc.Registry
{
    /// <summary>
    /// User should Provide ITokenToProcessorMapper to tell how to map the IExecuteLogic to the token.
    /// </summary>
    class RPCServerRegistry : ServiceRegistry
    {
        readonly IBoxLocations boxLocations;
        readonly ITokenToProcessorMapper mapper;
        public RPCServerRegistry(ITokenToProcessorMapper mapper) : base()
        {
            this.mapper = mapper;
            this.IncludeRegistry<CommonRegistry>();
            this.boxLocations = new BoxLocations();
            For<IBoxLocations>().Use(this.boxLocations).Singleton();

            this.ExecuteRecordBaseMappers();
            this.ExecuteRecordManagerMappers();
            //Manage the execution of a request
            For<IOrchExecutor>().Use<OrchExecutor>();
            //Listeners for inbox and reboot-continue box
            For<ILocalCallWatcher>().Use<OrchFileWatcher>()
                .Ctor<string>("watchDir").Is(boxLocations.MonitoringDir).Named("inbox");
            For<ILocalCallWatcher>().Use<OrchFileWatcher>()
                .Ctor<string>("watchDir").Is(boxLocations.RcDir).Named("rcbox");
            //the service facade for hosting the service
            For<IServiceFacade>().Use<ServiceFacade>()
                   .Ctor<ILocalCallWatcher>("inbox").IsNamedInstance("inbox")
                   .Ctor<ILocalCallWatcher>("rcbox").IsNamedInstance("rcbox");
        }

        private void ProcessUserLogics()
        {
            var diMapper = this.mapper.UserProcessorDI();
            foreach(var kv in diMapper)
            {
                For<IExecuteLogic>().Use(kv.Value).Transient().Named(kv.Key);
            }
        }

        private void ExecuteRecordBaseMappers()
        {
            //Assemble the IExecuteRecordInfo and IExecuteLogic to form the RecordProcessorBase into RecordProcessorManager, to be further maintained in upper layer
            this.Injectable<IExecuteRecordInfo>();
            this.ProcessUserLogics();
            this.Injectable<IExecuteLogic>();
            For<IRecordProcessorBase>().Use<RecordProcessorBase>().Named("forinject");
            this.Injectable<IRecordProcessorBase>();
            //record processor factory to Produce the RecordProcessorBase
            For<IRecordProcessorFactory>().Use<ExecuteRecordFactory>().Singleton();
        }

        private void ExecuteRecordManagerMappers()
        {
            For<IRecordProcessorManager>().Use<RecordProcessorManager>();
            //the factory in creating the IrecordProcessorManager, it receives a file, invoke ExecuteRecordFactory to create the IRecordProcessorBase, and inject
            For<IExecutorFactory>().Use<ExecutorFactory>();



        }
    }

}
