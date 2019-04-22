using FsBaseExecSvc.Interface;
using FsBaseExecSvc.Registry;

namespace FsBaseExecSvc.Hosting
{
    public class FileProcessingHostEntrance
    {
        public static IFileProcessingHost GetFileProcessingHost(ITokenToProcessorMapper mapper)
        {
            return new FileProcessingHost(new RPCServerRegistry(mapper));
        }
    }



}
