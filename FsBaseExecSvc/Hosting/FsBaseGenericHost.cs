using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using FsBaseExecSvc.Interface;

namespace FsBaseExecSvc.Hosting
{
    class FsBaseGenericHost : IHostedService
    {
        private readonly IApplicationLifetime _appLifetime;
        private readonly IConfiguration _configuration;
        private readonly IHostingEnvironment _environment;
        private readonly IServiceFacade _serviceface;
        private readonly ILogger<FsBaseGenericHost> _logger;
        private volatile bool _stopping = false;
        public FsBaseGenericHost(
            IServiceFacade serviceFace,
            ILogger<FsBaseGenericHost> logger,
            IConfiguration configuration,
            IHostingEnvironment environment,
            IApplicationLifetime appLifetime
            )
        {
            _logger = logger ?? throw new Exception("No logger found");
            _serviceface = serviceFace ?? throw new Exception("no service facade found");
            _configuration = configuration;
            _environment = environment;
            _appLifetime = appLifetime;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _appLifetime.ApplicationStarted.Register(OnStarted);
            _appLifetime.ApplicationStopping.Register(OnStopping);
            _appLifetime.ApplicationStopped.Register(OnStopped);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void OnStarted()
        {
            this._logger.LogInformation("Service Starting");
            this._serviceface.StartService();
            this._logger.LogInformation("Service started.");
        }

        private void OnStopping()
        {
            if (!_stopping)
            {
                _stopping = true;
                //service shutdown will abort the running test to prevent close timeout
                this._logger.LogInformation("Service Stopping...");
                this._serviceface.StopService();
                this._logger.LogInformation("Watcher stopped.");
            }

        }

        private void OnStopped()
        {
            this._logger.LogInformation("Service Stopped");

            // Perform post-stopped activities here
        }
    }
}
