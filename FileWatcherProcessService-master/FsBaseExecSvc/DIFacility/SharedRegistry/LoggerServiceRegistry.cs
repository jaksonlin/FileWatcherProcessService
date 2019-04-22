using Lamar;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace DIFacility.SharedRegistry
{

    class LoggerServiceRegistry : ServiceRegistry
    {
        internal static readonly LoggingLevelSwitch LoggingLevel = new LoggingLevelSwitch();
        readonly LoggerFactory loggerFactory = new LoggerFactory();
        readonly string dirLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public LoggerServiceRegistry()
        {
            LoggingLevel.MinimumLevel = Serilog.Events.LogEventLevel.Information;
            if (!Environment.UserInteractive)
            {
                Log.Logger = Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(LoggingLevel)
                .WriteTo.File(
                path: Path.Combine(dirLocation, $@"L1_alert.trc"),
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                rollOnFileSizeLimit: true
                )
                .CreateLogger();
            }
            else
            {
                Log.Logger = Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(LoggingLevel)
                .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                path: Path.Combine(dirLocation, $@"L1_alert.trc"),
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                rollOnFileSizeLimit: true
                )
                .CreateLogger();
            }
            
            loggerFactory.AddSerilog();
            For<ILoggerFactory>().Use(loggerFactory).Singleton();
            For(typeof(Microsoft.Extensions.Logging.ILogger<>)).Use(typeof(LoggerWrapper<>));
        }

        public bool HasMainWindow()
        {
            return (Process.GetCurrentProcess().MainWindowHandle != IntPtr.Zero);
        }
    }

    //lamar cannot match a open gernic type with a method call, using wrapper service instead 
    class LoggerWrapper<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        readonly Microsoft.Extensions.Logging.ILogger<T> instance;

        public LoggerWrapper(ILoggerFactory loggerFactory)
        {
            instance = loggerFactory.CreateLogger<T>();
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return instance.BeginScope<TState>(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return instance.IsEnabled(logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            instance.Log<TState>(logLevel:logLevel, eventId:eventId, state:state, exception:exception, formatter:formatter);
        }
    }
}
