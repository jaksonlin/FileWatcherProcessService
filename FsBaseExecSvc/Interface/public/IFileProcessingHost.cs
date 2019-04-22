using System.Threading;
using System.Threading.Tasks;
using FsBaseExecSvc.Registry;

namespace FsBaseExecSvc.Hosting
{
    public interface IFileProcessingHost
    {
        void RunAsService();
        void RunConsole();
        Task StopConsoleAsync();
    }
}