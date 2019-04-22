using DIFacility.SharedRegistry;
using FsBaseExecSvc.Abstract;
using FsBaseExecSvc.Client;
using FsBaseExecSvc.Executor;
using FsBaseExecSvc.Interface;
using FsBaseExecSvc.Registry;
using Lamar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ClientExample
{
    /// <summary>
    /// it provides the record with typical file format to write onto the server's SMB path 
    /// </summary>
    public class RPCClientTokenProvider : ITokenProvider
    {
        public IEnumerable<string> GetClientTokenNames()
        {
            return new string[] { "itest" };
        }
    }
}
