using DIFacility;
using DIFacility.SharedLib.Utils;
using FsBaseExecSvc.Registry;
using Lamar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FsBaseExecSvc.Hosting
{
    /// <summary>
    /// the HostBuilder for building the Generic Host, ConfigureHostConfiguration has been called in here to init necessary settings, including logging level and working directory.
    /// </summary>
    class FileProcessingHost : IFileProcessingHost
    {
        readonly HostBuilder builder;
        IHost host;
        public FileProcessingHost(RPCServerRegistry serviceDescriptors)
        {
            builder = new HostBuilder();
            this.builder.UseServiceProviderFactory<IServiceCollection>(new LamarServiceProviderFactory())
            .ConfigureHostConfiguration((IConfigurationBuilder configHost) =>
            {
                configHost.SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                configHost.AddJsonFile("HostConfig.json", false, true);//set the HostingEnvironment using JsonFile will read any value provided in json
            })
            .ConfigureContainer((HostBuilderContext hostContext, ServiceRegistry services) =>
            {
                OrchConfig config = hostContext.Configuration.GetSection("OrchConfig").Get<OrchConfig>();
                if (!string.IsNullOrEmpty(config.LogLevel) && config.LogLevel.Equals("debug"))
                {
                    LogLevelService.SetVerboseOn();
                }
                services.AddRange(serviceDescriptors);
            })
            .ConfigureServices((HostBuilderContext hostContext, IServiceCollection configSvc) =>
            {
                //if (hostContext.HostingEnvironment.IsDevelopment())
                //{
                //    Console.WriteLine("Development mode");
                //}
                //else
                //{
                //    Console.WriteLine("Production mode");
                //}
                configSvc.AddLogging();
                configSvc.AddHostedService<FsBaseGenericHost>();
            });
            //.ConfigureAppConfiguration((HostBuilderContext hostContext, IConfigurationBuilder configApp) =>
            //{
            //    configApp.SetBasePath(Path.Combine(hostBaseDir, "config"));
            //    configApp.AddJsonFile($@"fs.{hostContext.HostingEnvironment.EnvironmentName}.json".ToLower(), false, true);
            //})
        }
        
        public void RunConsole()
        {
            this.host = this.builder.Build();
            host.Start();
        }

        public Task StopConsoleAsync()
        {
            return host.StopAsync();
        }
       
        public void RunAsService()
        {
            this.host = this.builder.Build();
            var hostService = new GenericServiceHost(this.host);
            ServiceBase.Run(hostService);
        }


    }



}
