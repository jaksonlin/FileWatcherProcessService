using System;

namespace DIFacility.SharedLib.Utils
{
    interface IStringHelper
    {
        string GetExceptionDetails(Exception exp);
        string GetInnerExceptionMsg(Exception exp);
        string HandleAggregateException(AggregateException aggEx);
    }
}