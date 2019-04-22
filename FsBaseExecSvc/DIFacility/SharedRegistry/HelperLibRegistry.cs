using DIFacility.SharedLib.Utils;
using Lamar;
using System;
using System.Collections.Generic;
using System.Text;

namespace DIFacility.SharedRegistry
{
    class HelperLibRegistry : ServiceRegistry
    {
        public HelperLibRegistry()
        {
            For<IOSHelper>().Use<OSHelper>().Singleton();
            For<IStringHelper>().Use<StringHelper>().Singleton();
        }
    }
}
