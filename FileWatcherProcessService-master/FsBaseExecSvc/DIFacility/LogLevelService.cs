using DIFacility.SharedRegistry;
using System;
using System.Collections.Generic;
using System.Text;

namespace DIFacility
{
    /// <summary>
    /// this should only be used in your MAIN
    /// </summary>
    public static class LogLevelService
    {
        public static void SetVerboseOn()
        {
            LoggerServiceRegistry.LoggingLevel.MinimumLevel = Serilog.Events.LogEventLevel.Verbose;
        }

        public static void SetVerboseOff()
        {
            LoggerServiceRegistry.LoggingLevel.MinimumLevel = Serilog.Events.LogEventLevel.Information;
        }
    }
}
