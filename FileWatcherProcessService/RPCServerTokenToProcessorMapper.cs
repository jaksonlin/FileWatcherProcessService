using FsBaseExecSvc.Executor;
using FsBaseExecSvc.Interface;
using FsBaseExecSvc.Registry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FsBaseExecCli.ServerRegistry
{
    public class ProcessorAttribute : Attribute
    {
        public string Token { get; }
        public ProcessorAttribute(string token)
        {
            this.Token = token;
        }
    }
    /// <summary>
    /// Server Registry Core
    /// </summary>
    public class RPCServerTokenToProcessorMapper : ITokenToProcessorMapper
    {
        public IDictionary<string, IExecuteLogic> UserProcessorDI()
        {
            IEnumerable<Type> candidates = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IExecuteLogic)));
            var dict = new Dictionary<string, IExecuteLogic>();
            foreach(var item in candidates)
            {
                if(item.GetCustomAttributes(typeof(ProcessorAttribute), false).FirstOrDefault() is ProcessorAttribute attr)
                {
                    var instance = Activator.CreateInstance(item) as IExecuteLogic;
                    if (instance != null)
                    {
                        dict.Add(attr.Token, instance);
                    }else
                    {
                        throw new Exception($"create instance of type {item.Name} failed");
                    }
                    
                }
            }
            if(dict.Count == 0)
            {
                throw new Exception("No user defined processing logic");
            }
            return dict;
        }
    }
}
