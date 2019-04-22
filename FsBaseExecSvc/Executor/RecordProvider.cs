using System;
using FsBaseExecSvc.Interface;

namespace FsBaseExecSvc.Executor
{
    class FileNameProvider : IFileNameProvider
    {
        private readonly string name;

        public FileNameProvider(string name)
        {
            this.name = name;
        }

        public string GetFileName()
        {
            return $@"orch_{this.name}_{Guid.NewGuid().ToString()}.txt";
        }
    }
}
