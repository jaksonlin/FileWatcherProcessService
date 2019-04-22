# FileWatcherProcessService 
FileWatcher Processing Service.

This is a user defined file processing service. The service holds FileWatcher object to read the file in the directory (when new created) and run user define processing on the file.

Upon the service starts up, it will create a "boxes" folder in the service running dir, which contains 5 sub directories.

## Requirement 
All the server should use the same user account.

# Platform: 

Windows server 2012 R2 +

## Boxes for file watcher
inbox: any file created in the "<service_location>\boxes\inbox" will be read by the service and do some user define processing against the file.

runningbox: this directory is typically for user to know what are the things the service is currently working on. the service copy the file from inbox into here.

outbox: any output of the processing will be placed in this directory, using the same name as input file in inbox.

endbox: preserved the inbox's input file for audit/review.

rcbox: any file created in "<service_location>\boxes\rcbox" will trigger a server restart. And upon the service restart, it will consume the request in this box; if there are any running job on the server, the restart will be delayed till all running jobs completes. New in-coming job will be processed after service start from the node reboot.

## [Server-side] Define your own processing logic:

0. Create a Console Application.

1. Create a class and implement interface IExecuteLogic

    public interface IExecuteLogic
    {
        string ProcessingLogic(IEnumerable<string> requestContent);
    }
    
The requestContent is what client side provided for processing on server side.

2. Implement interface :


            public interface ITokenToProcessorMapper
            {
                IDictionary<string, IExecuteLogic> UserProcessorDI();
            }
            
        
In this method you should return the key-value pairs. of a user-defined-string to the user-defined-processing logic.

You can refer to exmaple:  FileWatcherProcessService/RPCServerTokenToProcessorMapper.cs, it uses Custom attribute and reflection to build up the key-value pairs.

3. Modify the main to async main (C# 7.3 lang support):

        static async Task Main(string[] args)
        { 
            IFileProcessingHost host = 
                    FileProcessingHostEntrance.GetFileProcessingHost(new RPCServerTokenToProcessorMapper());
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
        
And you are set for the server service.
 

## [Client-side] Register the user-defined-string to the client

1. implement interface:

            public interface ITokenProvider
            {
                IEnumerable<string> GetClientTokenNames();
            }
        
This is for client to know what are the user-defined-string that the server know, the string should match the those setup on the server side.
        
## RPC calls (will be enhance further to hide the backing container)

0. Create a ClientEntrace factory using:

        IClientEntrance factory = ClientEntraceFactory.GetClientEntrance(new RPCClientTokenProvider());
           
 
1. Resolve the IFsRPCBase using the user-defined-string:

        IFsRPCBase fsDemoRPC = factory.GetRPCObject(<user defined string>);
        

2. RunOnNode 

        (bool ok, string output) RunOnNode(string node, string contentToRun);
        
this function will create a file on the target server with path: 

        ServiceProfileInstance.GetNodeServiceNetLocation()/boxes/inbox/orch_<user-defined-string>_guid.txt

If the service is not configured on the target server, or it is of old version, it will be deployed/update automatically.
It will check the service location/boxes/ directory for doing RPC.

3. RunOnNode with client-side timeout

        (bool ok, string output) RunOnNode(string node, string contentToRun, int timeoutSeconds);

The call will return (false, $@"[ORCH-ERR]the operation time out ({timeoutSeconds})") if the call timeout. Note the request on the server will not be aborted, only client-side is timing out.

3.1 How the service looks like:
        
        a) The client will deploy it on user_home/filewatcherprocesservice2
        b) The client uses the profile.ini file (same location as your client.exe) to configure the service log on user credentials on targeting server
        c) It starts up automatically upon server reboot
        d) It has some dependencies on networking for SMB path to work
        e) Its name: FileWatcherProcessService2
        
        
3.2 Auto deployment of the server service will happen when:

        a) There's no FileWatcherPRocessService2 found on the remote server
        b) The profile.ini on the remote server not match with the one on client - this is designed to automacticall fix the service logon password expiration.
        c) The FileWatcherPRocessService2.dll found on the remote server is of older version than the client's
        
3.3 How can the client find the service binary to deploy:

You need to make sure the client binary is having below hierarchy:

        <my_client_folder>/myclient.exe
        <my_client_folder>/profile.ini
        <my_client_folder>/filewatcherprocesservice2/filewatcherprocesservice2.exe
        <my_client_folder>/filewatcherprocesservice2/profile.ini
        
        note both profile.ini should have valid credential. We will enhance the encryption on the password in later release

3.4 Auto deployment of the server service will **NOT** happen when:

        a) You have deploy the FileWatcherPRocessService2 yourself on the target nodes already. and the running service is of the same or higer version than the one in "<my_client_folder>/filewatcherprocesservice2/"
        
4. RunAfterRebootOnNode

        (bool ok, string output) RunAfterRebootOnNode(string node, string contentToRun);
 
When a service received this request, it will 

        a) shutdown the inbox watcher; 
        b) do a sync wait on all the current running jobs
        c) **REBOOT** Server 
        d) move all the files in "boxes/inbox" to "boxes/rcbox"
        e) start all the necessary watchers
        f) re-establish the watchers for RPC usage
        g) start the processing "boxes/rcbox" which contains request from local/RPC fire event upon finished
        
## Debug Logging
Before starting up the service, open up the HostConfig.json file, and change the LogLevel value to debug. 


## When to use:

1. When there's no SSH on your server
2. When there's no Powershell Remoting on your server
3. When IT said RestAPI is not allowed
4. When there is a Domain user that you can use to access the servers you would like to work on. and you can "net use" it to other server.

## Dependencies:
1. Lamar Container, https://github.com/JasperFx/lamar
2. Serilog, https://github.com/serilog/serilog


## TO-DO
0. The ServiceProfile is not easy to use, need a way to simplify its usage, and remove the PlainText password in SimpleServiceProfile implementation.
1. Hot-Modification of debug logging.
2. Code clean-up, the code may not be good, but must be easy to use for others.


