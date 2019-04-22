using DIFacility;
using DIFacility.SharedLib.Utils;
using FsBaseExecCli.ServerRegistry;
using FsBaseExecSvc;
using FsBaseExecSvc.Hosting;
using Lamar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace FileWatcherProcessService
{
    class Program
    {
        static async Task Main(string[] args)
        { 
            IFileProcessingHost host = FileProcessingHostEntrance.GetFileProcessingHost(new RPCServerTokenToProcessorMapper());
            if(args.Length>0 && args[0].Equals("debug"))
            {
                host.RunConsole();
                Console.WriteLine("Press Enter to exit");
                Console.ReadLine();
                await host.StopConsoleAsync();
            }
            else
            { 
                host.RunAsService();
            }
        }
    }
}
