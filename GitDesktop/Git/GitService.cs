using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GitDesktop.Git
{
    internal class GitService
    {
        public static GitStatus GetStatus(string repositoryPath)
        {
            string gitStatusCmdResult = Execute(repositoryPath, "status --porcelain");

            GitStatus status = new();
            foreach (string line in gitStatusCmdResult.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                char index = line[0];
                char workingTree = line[1];
                string path = line.Substring(3);

                GitFile file = new()
                {
                    Path = path,
                    IndexState = GitFileStatus.ParseState(index),
                    WorkingTreeState = GitFileStatus.ParseState(workingTree)
                };

                status.Files.Add(file);
            }

            return status;
        }

        public static (List<GitBranch>, GitBranch) GetBranches(string repositoryPath)
        {
            string gitBranchCmdResult = Execute(repositoryPath, "branch --list");
            List<GitBranch> branches = [];
            GitBranch currentBranch = new("main");            

            foreach (string line in gitBranchCmdResult.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                string branchName = line.TrimStart('*', ' ').Trim();
                bool isCurrent = line.StartsWith("*");

                GitBranch branch = new(branchName);

                if (isCurrent)                
                    currentBranch = branch;
                branches.Add(branch);                
            }

            return (branches, currentBranch);
        }

        public static void CheckoutBranch(string repositoryPath, string branchName)
        {
            Execute(repositoryPath, $"checkout \"{branchName}\"");
        }

        private static string Execute(string repositoryPath, string arguments)
        {
            Process process = new();
            process.StartInfo.FileName = "git";
            process.StartInfo.Arguments = arguments;
            process.StartInfo.WorkingDirectory = repositoryPath;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Git command failed with exit code {process.ExitCode}");

            return output;
        }
    }
}
