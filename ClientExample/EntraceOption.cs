using CommandLine;
using DIFacility;
using FsBaseExecSvc.Client;
using FsBaseExecSvc.Interface;
using Lamar;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace ClientExample
{
    class EntraceOption
    {
        [Option("config", Required = true, HelpText = "Config file name")]
        public string ConfigFile { get; set; } = "";
        [Option("update", Required = false, HelpText = "Update the tool")]
        public bool Update { get; set; }

        [Option("node", Required = false, HelpText = "Node to run, note the config file is still current node local")]
        public string Node { get; set; } = System.Net.Dns.GetHostName();

        private readonly static string ServiceName = "FileWatcherProcessService";
        public bool VerifyInput()
        {
            if (!Directory.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ServiceName)))
            {
                Console.WriteLine($@"Incomplete version of file. Please copy again");
                return false;
            }
            if (!this.Update && !File.Exists(this.ConfigFile))
            {
                Console.WriteLine($@"Config file {this.ConfigFile} not exist");
                return false;
            }
            return true;
        }

        public string Run()
        {
            var factory = ClientEntraceFactory.GetClientEntrance(new RPCClientTokenProvider());
            LogLevelService.SetVerboseOn();

            IFsRPCBase fsDemoRPC = factory.GetRPCClient("itest");
            var result = fsDemoRPC.RunOnNode("rwsam16", "time=11111", 3);

            if (this.Update)
            {
                fsDemoRPC.UpdateServiceCred(this.Node);
                fsDemoRPC.UpdateServiceBinary(this.Node);
                return "Update Completes";
            }
            else
            {
                //example for reboot continue, just an example, you should build your own criteria for reboot-continue
                if (this.ConfigFile.Contains("12345"))
                {
                    result = fsDemoRPC.RunAfterRebootOnNode(this.Node, File.ReadAllText(this.ConfigFile));
                    return result.output;
                }
                else
                {
                    result = fsDemoRPC.RunOnNode(this.Node, File.ReadAllText(this.ConfigFile));
                    return result.output;
                }
            }
        }
    }
}