using System.Collections.Generic;

namespace FsBaseExecSvc.Interface
{
    public interface IExecuteLogic
    {
        string ProcessingLogic(IEnumerable<string> requestContent);
    }
}
