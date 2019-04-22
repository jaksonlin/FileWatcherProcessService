using System;
using System.Collections.Generic;
using System.Text;

namespace FsBaseExecSvc.Interface
{
    public interface ITokenProvider
    {
        /// <summary>
        /// these are the token names that can be mapped on the ITokenToProcessorMapper
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetClientTokenNames();
    }
}
