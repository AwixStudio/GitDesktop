using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

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

        public static void CommitChanges(string repositoryPath, string commitMessage, List<GitFile> files)
        {
            var selectedFiles = files
                .Where(f => f.MarkedForCommit)
                .Select(f => $"\"{f.Path}\"");

            string arguments = "add " + string.Join(' ', selectedFiles);

            Execute(repositoryPath, arguments);
            Execute(repositoryPath, $"commit -m \"{commitMessage}\"");
            Execute(repositoryPath, "push");
        }

        public static List<GitCommit> GetCommitLog(string repositoryPath, string branchName, int limit = 100)
        {
            string gitLogCmd = $"log {branchName} --pretty=format:\"%H|%an|%ai|%s|%P\" --max-count={limit}";
            string gitLogCmdResult = Execute(repositoryPath, gitLogCmd);

            List<GitCommit> commits = [];
            foreach (string line in gitLogCmdResult.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('|');
                if (parts.Length >= 5)
                {
                    string hash = parts[0].Trim();
                    string author = parts[1].Trim();
                    if (DateTime.TryParse(parts[2].Trim(), out DateTime date))
                    {
                        string message = parts[3].Trim();
                        string parentHash = parts[4].Trim().Split(' ').FirstOrDefault() ?? "";

                        commits.Add(new GitCommit(hash, author, date, message, parentHash));
                    }
                }
            }

            return commits;
        }

        public static string GetFileDiff(string repositoryPath, string filePath)
        {
            try
            {
                // For untracked files, show the content instead of diff
                string gitDiffCmd = $"diff HEAD -- \"{filePath}\"";
                string gitDiffResult = Execute(repositoryPath, gitDiffCmd);

                // Limit diff to first 30 lines
                var lines = gitDiffResult.Split('\n');
                int maxLines = Math.Min(30, lines.Length);
                return string.Join("\n", lines.Take(maxLines));
            }
            catch
            {
                // If diff fails, try to show file content for untracked files
                try
                {
                    string showCmd = $"show :{filePath}";
                    return Execute(repositoryPath, showCmd);
                }
                catch
                {
                    return "Unable to load diff";
                }
            }
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
