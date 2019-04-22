using System;
using System.Collections.Generic;
using System.Text;

namespace DIFacility.SharedLib.Utils
{
    class StringHelper : IStringHelper
    {
        public  string GetExceptionDetails(Exception exp)
        {
            if (exp is AggregateException aggex)
            {
                return HandleAggregateException(aggex);
            }
            else
            {
                return $@"{exp.Message} {Environment.NewLine} {exp.StackTrace}{Environment.NewLine}  {GetInnerExceptionMsg(exp)}";
            }
        }

        public  string GetInnerExceptionMsg(Exception exp)
        {
            var sb = new StringBuilder();
            var innerExp = exp.InnerException;
            var firstStack = exp.StackTrace;
            while (innerExp != null)
            {
                sb.AppendLine($@"{Environment.NewLine}[Expcetion Details] [{innerExp.GetType()}] {innerExp.Message} {Environment.NewLine}");
                innerExp = innerExp.InnerException;
            }
            sb.AppendLine(firstStack);
            return sb.ToString();
        }

        public  string HandleAggregateException(AggregateException aggEx)
        {
            var sb = new StringBuilder();
            foreach (Exception exInnerException in aggEx.Flatten().InnerExceptions)
            {
                Exception exNestedInnerException = exInnerException;
                do
                {
                    if (!string.IsNullOrEmpty(exNestedInnerException.Message))
                    {
                        sb.AppendLine(exNestedInnerException.Message);
                        sb.AppendLine(exNestedInnerException.StackTrace);
                        sb.AppendLine(new string('=', 60));
                    }
                    exNestedInnerException = exNestedInnerException.InnerException;
                }
                while (exNestedInnerException != null);
            }
            return sb.ToString();
        }

    }
}
