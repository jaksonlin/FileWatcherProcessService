using Lamar;
using System;
using System.Collections.Generic;
using System.Text;

namespace DIFacility.SharedRegistry
{
    /// <summary>
    /// A registry that Provides StringHelper, OSHelper, and Logger service
    /// </summary>
    class CommonRegistry : ServiceRegistry
    {
        public CommonRegistry()
        {
            this.IncludeRegistry<LoggerServiceRegistry>();
            this.IncludeRegistry<HelperLibRegistry>();
        }
    }
}
