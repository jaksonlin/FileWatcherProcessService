using CommandLine;
using DIFacility;
using FsBaseExecSvc.Client;
using FsBaseExecSvc.Interface;
using System;
using System.ComponentModel;

namespace ClientExample
{
    //this is a demo of client, you should make your own change to keep credential safe.
    class Program
    {
        static void Main(string[] args)
        {

            //ParserResult<EntraceOption> item = Parser.Default.ParseArguments<EntraceOption>(args);
            //item.WithParsed<EntraceOption>(opts => ProcessOption(opts, args));
            var factory = ClientEntraceFactory.GetClientEntrance(new RPCClientTokenProvider());
            LogLevelService.SetVerboseOn();

            IFsRPCBase fsDemoRPC = factory.GetRPCClient("itest");
            var result = fsDemoRPC.RunOnNode("rwsam16", "time=11111",3);

            Console.WriteLine(result);
            Console.ReadLine();
        }
        private static void ProcessOption(EntraceOption opts, string[] args)
        {
            if (opts.VerifyInput())
            {
                string result = opts.Run();
                Console.WriteLine(result);
            }
            else
            {
                Console.WriteLine("Exit due to input not valid");
            }
        }
    }
}
