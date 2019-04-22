using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;
using System.Text;

namespace DIFacility.SharedLib.Utils
{
    class GenericServiceHost : ServiceBase
    {
        private IHost _host;
        private bool _stopRequestedByWindows;
        private int _startAdditionalTimeMs = 2000;

        public GenericServiceHost(IHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public GenericServiceHost(IHost host, int startAdditionalTimeMs) : this(host)
        {
            _startAdditionalTimeMs = startAdditionalTimeMs;
        }

        protected sealed override void OnStart(string[] args)
        {
            this.RequestAdditionalTime(_startAdditionalTimeMs);
            OnStarting(args);

            _host
                .Services
                .GetRequiredService<IApplicationLifetime>()
                .ApplicationStopped
                .Register(() =>
                {
                    if (!_stopRequestedByWindows)
                    {
                        Stop();
                    }
                });

            _host.Start();

            OnStarted();
        }

        protected sealed override void OnStop()
        {
            _stopRequestedByWindows = true;
            OnStopping();
            try
            {
                _host.StopAsync().GetAwaiter().GetResult();
            }
            finally
            {
                _host.Dispose();
                OnStopped();
            }
        }

        protected virtual void OnStarting(string[] args) { }

        protected virtual void OnStarted() { }

        protected virtual void OnStopping() { }

        protected virtual void OnStopped() { }
    }

    public static class GenericHostWindowsServiceExtensions
    {
        public static void RunAsService(this IHost host)
        {
            var hostService = new GenericServiceHost(host);
            ServiceBase.Run(hostService);
        }

        public static void RunAsService(this IHost host, int startAdditionalTimeMs)
        {
            var hostService = new GenericServiceHost(host, startAdditionalTimeMs);
            ServiceBase.Run(hostService);
        }
    }
}
