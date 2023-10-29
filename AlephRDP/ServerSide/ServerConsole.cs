using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace AlephRDP.ServerSide
{
    internal class ServerConsole
    {
        public string workingDirectory = Environment.CurrentDirectory;
        public FileInfo toSend;

        public string RunCommand(string command)
        {
            string output = string.Empty;
            try
            {
                if (command == "reset")
                    workingDirectory = Environment.CurrentDirectory;
                else if (command == "exit")
                    return null;
                else if (command.StartsWith("download"))
                {
                    toSend = new FileInfo(workingDirectory + "/" + command.Substring(8).Trim());
                    if (!toSend.Exists)
                        output = $"Invalid file specified" + Environment.NewLine;
                    else
                    {
                        File.AppendAllText("console.log", command + Environment.NewLine + $"{workingDirectory}> ");
                        return null;
                    }
                }
                else if (command.StartsWith("type"))
                    output = File.ReadAllText(workingDirectory + "/" + command.Substring(4).Trim()) + Environment.NewLine;
                else
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = (command.StartsWith("echo") ? "/U" : "") + "/C " + command,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = workingDirectory
                    };
                    using (Process process = new Process())
                    {
                        process.StartInfo = startInfo;
                        process.Start();
                        output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();
                        if (!string.IsNullOrEmpty(error))
                            output += error;
                        else
                            UpdateWorkingDirectory(command);
                    }
                }
            }
            catch { }
            string finalOutput = $"{output}{workingDirectory}> ";
            File.AppendAllText("console.log", command + Environment.NewLine + finalOutput);
            return finalOutput;
        }

        private void UpdateWorkingDirectory(string command)
        {
            if (Regex.IsMatch(command, @"^[a-zA-Z]:$"))
                workingDirectory = command;
            else if (command.StartsWith("cd ", StringComparison.OrdinalIgnoreCase))
            {
                string newDirectory = command.Substring(3).Trim();
                string rootChecker = newDirectory == "/" || newDirectory.StartsWith("/") ? newDirectory : workingDirectory + "/" + newDirectory;
                try
                {
                    newDirectory = Path.GetFullPath(rootChecker);
                    if (Directory.Exists(newDirectory))
                        workingDirectory = newDirectory;
                    else
                        Console.WriteLine("Directory not found: " + newDirectory);
                }
                catch { }
            }
        }
        public void Main()
        {
            Console.Write(workingDirectory + "> ");
            while (true)
                Console.Write(RunCommand(Console.ReadLine()));
        }
    }
}
