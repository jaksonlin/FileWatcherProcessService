using System;
using System.Collections.Generic;
using System.Text;

namespace FsBaseExecSvc.Interface
{
    public interface ITokenToProcessorMapper
    {
        IDictionary<string, IExecuteLogic> UserProcessorDI();
    }
}
