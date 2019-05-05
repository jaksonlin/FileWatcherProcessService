using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace DIFacility.SharedLib.Utils
{
    class OSHelper : IOSHelper
    {
        public OSHelper(ILogger<IOSHelper> logger)
        {
            this.logger = logger;
        }
         readonly string scExe = Path.Combine(Environment.SystemDirectory, "sc.exe");
         readonly string netExe = Path.Combine(Environment.SystemDirectory, "net.exe");
        private readonly ILogger<IOSHelper> logger;

        public  string GetService(string serviceName, string server)
        {
            return PureCmdExector(scExe, $@"\\{server} query {serviceName}").output;
        }
        public  string GetService(string serviceName)
        {
            return PureCmdExector(scExe, $@"query {serviceName}").output;
        }
        public  string CreateService(string serviceName, string server, string svcBin, string svcUser, string svcPwd, string depend)
        {
            string args = $@"\\{server} create {serviceName} start= auto binPath= {svcBin} depend= {depend} obj= ""{svcUser}"" password= ""{svcPwd}""";
            PureCmdExector(scExe, $@"\\{server} stop {serviceName}");
            PureCmdExector(scExe, $@"\\{server} delete {serviceName}");
            //for service clean by os.
            Thread.Sleep(TimeSpan.FromSeconds(2));
            PureCmdExector(scExe, args);
            PureCmdExector(scExe, $@"\\{server} start {serviceName}");
            Thread.Sleep(TimeSpan.FromSeconds(2));
            return PureCmdExector(scExe, $@"\\{server} query {serviceName}").output;
        }
        public  string CreateService(string serviceName, string svcBin, string svcUser, string svcPwd, string depend)
        {
            string args = $@"create {serviceName} start= auto binPath= {svcBin} depend= {depend} obj= ""{svcUser}"" password= ""{svcPwd}""";
            PureCmdExector(scExe, $@"stop {serviceName}");
            PureCmdExector(scExe, $@"delete {serviceName}");
            //for service clean by os.
            Thread.Sleep(TimeSpan.FromSeconds(2));
            PureCmdExector(scExe, args);
            PureCmdExector(scExe, $@"start {serviceName}");
            Thread.Sleep(TimeSpan.FromSeconds(2));
            return PureCmdExector(scExe, $@"query {serviceName}").output;
        }
        public  bool StartService(string serviceName, string server, int timeout = 2)
        {
            PureCmdExector(scExe, $@"\\{server} start {serviceName}");
            Thread.Sleep(TimeSpan.FromSeconds(timeout));
            var rs = PureCmdExector(scExe, $@"\\{server} query {serviceName}");
            if (rs.output.Contains("RUNNING"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public  (bool result, string details) RemoteCopyFiles(string hostname, string source, string target, string domain, string user, string pwd)
        {
            string source_fixed;
            string target_fixed;
            //XCopy directory does not allow the path ends with slash.
            if (source.EndsWith(@"\"))
            {
                source_fixed = source.Substring(0, source.Length - 1).ToLower();
            }
            else
            {
                source_fixed = source.ToLower();
            }
            if (target.EndsWith(@"\"))
            {
                target_fixed = target.Substring(0, target.Length - 1).ToLower();
            }
            else
            {
                target_fixed = target.ToLower();
            }
            var hostFromDns = System.Net.Dns.GetHostName().ToLower();
            if (hostFromDns.Contains(hostname.ToLower()) || 
                hostname.ToLower().Contains(hostFromDns)) {
                //local host
                if (target_fixed.Equals(source_fixed))
                {
                    return (Directory.Exists(target_fixed), "this is a local host copying into same dir, no copy happen");
                }
            }
            if (!target_fixed.StartsWith($@"\\{hostname}\"))
            {
                target_fixed = $@"\\{hostname}\{target_fixed.Replace(":", "$")}";
            }
            var rs = PureCmdExector(
                $@"{Path.Combine(Environment.SystemDirectory, "xcopy.exe")}",
                $@"/E /I /Y /V ""{source_fixed}"" ""{target_fixed}""",
                usr: user, domain: domain, pwd: this.StringToSecureString(pwd));
            //Console.WriteLine(rs);
            return (Directory.Exists(target_fixed), rs.output);
        }
        public  bool StartService(string serviceName, int timeout = 2)
        {
            PureCmdExector(scExe, $@"start {serviceName}");
            Thread.Sleep(TimeSpan.FromSeconds(timeout));
            var rs = PureCmdExector(scExe, $@"query {serviceName}");
            if (rs.output.Contains("RUNNING"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public  bool StopService(string serviceName, string server, int timeout = 2)
        {
            string scExe = Path.Combine(Environment.SystemDirectory, "sc.exe");
            PureCmdExector(scExe, $@"\\{server} stop {serviceName}");
            Thread.Sleep(TimeSpan.FromSeconds(timeout));
            var rs = PureCmdExector(scExe, $@"\\{server} query {serviceName}");
            if (rs.output.Contains("STOPPED"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public  bool StopService(string serviceName, int timeout = 2)
        {
            string scExe = Path.Combine(Environment.SystemDirectory, "sc.exe");
            PureCmdExector(scExe, $@"stop {serviceName}");
            Thread.Sleep(TimeSpan.FromSeconds(timeout));
            var rs = PureCmdExector(scExe, $@"query {serviceName}");
            if (rs.output.Contains("STOPPED"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public  string SecureStringToString(SecureString value)
        {
            IntPtr bstr = Marshal.SecureStringToBSTR(value);

            try
            {
                return Marshal.PtrToStringBSTR(bstr);
            }
            finally
            {
                Marshal.FreeBSTR(bstr);
            }
        }

        public  SecureString StringToSecureString(string value)
        {
            if (value == null || value == string.Empty)
            {
                return null;
            }
            var secure = new SecureString();
            foreach (char c in value.ToCharArray())
            {
                secure.AppendChar(c);
            }
            return secure;
        }
        public  bool NetUseSMBDrive(string driveName, string node, string domain, string user, string pwd)
        {
            var result = PureCmdExector(netExe, $@"use \\{node}\{driveName}$", redirect_input: true, content_in: $@"{domain}\{user}{Environment.NewLine}{pwd}");
            if (result.result && result.output.Contains("The command completed successfully."))
            {
                return true;
            }
            else
            {
                this.logger.LogError($@"Net use failed info {result.output}");
                return false;
            }
        }
        public  string GetServiceImagePath(string serviceName)
        {
            using (RegistryKey rgs = Microsoft.Win32.Registry.LocalMachine.OpenSubKey($@"System\CurrentControlSet\Services\{serviceName}"))
            {
                if (rgs != null)
                {
                    return rgs.GetValue("ImagePath").ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
        }
        public  string GetServiceImagePath(string serviceName, string node)
        {
            var result = PureCmdExector(scExe, $@"\\{node} qc {serviceName}");
            var match = Regex.Match(result.output, $@"^\s+BINARY_PATH_NAME\s+?:\s*(.*)$", RegexOptions.Multiline);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
            else
            {
                return string.Empty;
            }
        }
        public  bool IsUserValid(string domain, string user, SecureString pwd, string node)
        {
            try
            {
                var tmpFolder = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid().ToString()}");
                string drive = tmpFolder.Trim().Substring(0, 1);
                if (!this.NetUseSMBDrive(drive, node, domain: domain, user: user, pwd: this.SecureStringToString(pwd)))
                {
                    this.logger.LogError($@"Net use {node} on drive {drive} for validate user failed");
                    return false;
                }
                tmpFolder = tmpFolder.Replace(":", "$");
                tmpFolder = $@"\\{node}\{tmpFolder}";
                var details = this.PureCmdExector("cmd.exe", $@"/c mkdir {tmpFolder}", usr: user, domain: domain, pwd: pwd);
                if (!details.result)
                {
                    this.logger.LogError($@"Cmd execute for validate user hit some issue {details.output}");
                }
                if (Directory.Exists(tmpFolder))
                {
                    this.logger.LogInformation($"User Validation success.");
                    Directory.Delete(tmpFolder);
                    return true;
                }
                else
                {
                    this.logger.LogError($"User Validation failed. ");
                    return false;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"User Validation failed.");
                return false;
            }

        }
        public  void Reboot(int timeleft)
        {
            PureCmdExector("shutdown.exe", $"-r -t {timeleft}");
        }
        #region cmd
        private  ProcessStartInfo ProcessStartInfoFactory(string filename, string arguments, IDictionary<string, string> env = null, string working_dir = "", string usr = "", SecureString pwd = null, string domain = "")
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(filename, arguments)
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                ErrorDialog = true,
                UseShellExecute = false,
                CreateNoWindow = true,

                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            if (!string.IsNullOrEmpty(working_dir))
            {
                startInfo.WorkingDirectory = working_dir;
            }
            if (!string.IsNullOrEmpty(usr))
            {
                startInfo.UserName = usr;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (pwd != null)
                {
                    startInfo.Password = pwd;
                }
                if (!string.IsNullOrEmpty(domain))
                {
                    startInfo.Domain = domain;
                }
            }

            if (env != null)
            {
                foreach (KeyValuePair<string, string> kvp in env)
                {
                    startInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                }
            }
            return startInfo;
        }
        public  (bool result, string output) PureCmdExector(string filename, string arguments, IDictionary<string, string> env = null, string working_dir = "", string usr = "", SecureString pwd = null, string domain = "", bool redirect_input = false, string content_in = "")
        {
            ProcessStartInfo startInfo = ProcessStartInfoFactory(filename, arguments, env, working_dir, usr, pwd, domain);
            
            return PureCmdExector(startInfo, redirect_input, content_in);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="startInfo"></param>
        /// <exception cref="Exception"></exception>
        /// <returns></returns>
        private  (bool ok, string output) PureCmdExector(ProcessStartInfo startInfo, bool redirect_input, string content_in)
        {
            Process process = new Process();

            StringBuilder stdOutput = new StringBuilder();
            StringBuilder stdErrorOutput = new StringBuilder();
            if (redirect_input)
            {
                startInfo.RedirectStandardInput = true;
            }
            process.StartInfo = startInfo;
            process.OutputDataReceived += (sender, args) => stdOutput.Append(args.Data + Environment.NewLine);
            process.ErrorDataReceived += (sender, args) => stdErrorOutput.Append(args.Data + Environment.NewLine);
            int pid = -1;
            string result = "";
            try
            {
                process.EnableRaisingEvents = true;
                process.Start();
                pid = process.Id;
                this.logger.LogInformation($@"Run cmd [PID : {pid}] start: {startInfo.FileName}, with args {startInfo.Arguments}");
                if (redirect_input)
                {
                    process.StandardInput.WriteLine(content_in);
                }
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                result = $"{stdErrorOutput.ToString().Trim()} {Environment.NewLine} {stdOutput.ToString().Trim()}";
                this.logger.LogInformation($@"Run cmd [PID : {pid}] success: {startInfo.FileName}, with args {startInfo.Arguments}");
                this.logger.LogDebug($@"Cmd output: {result}");
                return (true, result.Trim());
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $@"Run cmd [PID : {pid}] failed: {startInfo.FileName} {startInfo.Arguments}");
                result = $@"Run cmd [PID : {pid}] failed: {startInfo.FileName} {startInfo.Arguments} {ex.Message} {ex.StackTrace}";
            }
            this.logger.LogDebug($@"the result of cmd [PID : {pid}] is failed, cmd output {result}");
            return (false, result);
        }
        #endregion
    }

    public static class FileVersionCompareExtension
    {
        public static bool IsNewer(this FileVersionInfo fileVersionInfo, FileVersionInfo anotherFileVersionInfo)
        {
            Version myVersion = new Version(fileVersionInfo.FileVersion);
            Version anotherVersion = new Version(anotherFileVersionInfo.FileVersion);
            return myVersion > anotherVersion;
        }
    }
}
