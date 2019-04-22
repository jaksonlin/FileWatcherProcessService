namespace FsBaseExecSvc.Interface
{
    interface IFileNameProvider
    {
        /// <summary>
        /// return a file name that is of format orch_userdefinetyestr_guid.txt
        /// </summary>
        /// <returns></returns>
        string GetFileName();
    }
}