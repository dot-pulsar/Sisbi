using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sisbi.Extensions
{
    public class ShellResult
    {
        public string Output { get; set; }
        public string Error { get; set; }
        public int Code { get; set; }
    }
    
    public static class ShellHelper
    {
        public static ShellResult Bash(this string cmd)
        {
            var result = new ShellResult();
            var escapedArgs = cmd.Replace("\"", "\\\"");
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };
            process.Exited += (sender, args) =>
            {
                result.Output = process.StandardOutput.ReadToEnd();
                result.Error = process.StandardError.ReadToEnd();
                result.Code = process.ExitCode;

                process.Dispose();
            };

            try
            {
                process.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return result;
        }
    }
}