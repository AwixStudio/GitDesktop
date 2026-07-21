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
            // Get all branches (local and remote with -a flag)
            string gitBranchCmdResult = Execute(repositoryPath, "branch -a");
            List<GitBranch> branches = [];
            GitBranch currentBranch = new("main");            

            foreach (string line in gitBranchCmdResult.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                string branchName = line.TrimStart('*', ' ').Trim();
                bool isCurrent = line.StartsWith("*");

                // Skip remote HEAD pointers (e.g., "origin/HEAD -> origin/main")
                if (branchName.Contains("->"))
                    continue;

                // Remove "remotes/" prefix from remote branches for easier display
                if (branchName.StartsWith("remotes/"))
                {
                    branchName = branchName.Substring("remotes/".Length);
                }

                GitBranch branch = new(branchName);

                if (isCurrent)                
                    currentBranch = branch;

                // Avoid duplicates (local branch + remote branch with same name)
                if (!branches.Any(b => b.Name == branchName))
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

        public static void HardReset(string repositoryPath)
        {
            Execute(repositoryPath, "reset --hard HEAD");
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

        public static async Task UpdateFromMain(string repositoryPath, Action<string, float> onProgress)
        {
            onProgress("Fetching...", 5);

            int exit = await ExecuteAsync(
                repositoryPath,
                "fetch origin",
                onOutput: (message) => onProgress(message, 5));

            if (exit != 0)
                throw new Exception("Fetch failed.");

            onProgress("Merging...", 20);

            exit = await ExecuteAsync(
                repositoryPath,
                "merge origin/main",
                onOutput: (message) => 
                {
                    if (message.Contains('%'))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(
                            message,
                            @"^(.*?)(\d+)%(.+)?$");

                        if (match.Success && float.TryParse(match.Groups[2].Value, out float percent))
                        {
                            string before = match.Groups[1].Value.TrimEnd();
                            string after = match.Groups[3].Value.Trim();

                            onProgress(
                                $"{before} {percent}%% {after}",
                                20 + percent * 0.74f);
                        }
                    }
                });

            if (exit != 0)
                throw new Exception("Merge failed.");

            onProgress("Merging done.", 95);
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

        public static void DiscardChanges(string repositoryPath, List<GitFile> files)
        {
            var selectedFiles = files
                .Where(f => f.MarkedForCommit)
                .Select(f => $"\"{f.Path}\"");
            string arguments = "checkout -- " + string.Join(' ', selectedFiles);
            Execute(repositoryPath, arguments);
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

        private static async Task<int> ExecuteAsync(string repositoryPath, string arguments, Action<string>? onOutput = null, Action<string>? onError = null)
        {
            using Process process = new();

            process.StartInfo.FileName = "git";
            process.StartInfo.Arguments = arguments;
            process.StartInfo.Environment["GIT_TRACE2_EVENT"] = "-";
            process.StartInfo.WorkingDirectory = repositoryPath;

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.EnableRaisingEvents = true;

            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data) && !e.Data.Contains("warning", StringComparison.OrdinalIgnoreCase))
                {
                    onOutput?.Invoke(e.Data);
                    Console.WriteLine(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data) && !e.Data.Contains("warning", StringComparison.OrdinalIgnoreCase))
                {
                    onOutput?.Invoke(e.Data);
                    Console.WriteLine(e.Data);
                }
            };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            return process.ExitCode;
        }
            /// <summary>
            /// Create a new branch from a specific commit
            /// </summary>
            public static void CreateBranch(string repositoryPath, string branchName, string commitHash)
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"branch {branchName} {commitHash}",
                        WorkingDirectory = repositoryPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    string error = process.StandardError.ReadToEnd();
                    throw new Exception($"Git branch failed: {error}");
                }
            }

            /// <summary>
            /// Perform cherry-pick operation
            /// </summary>
            public static void CherryPick(string repositoryPath, string commitHash)
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"cherry-pick {commitHash}",
                        WorkingDirectory = repositoryPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    string error = process.StandardError.ReadToEnd();
                    throw new Exception($"Git cherry-pick failed: {error}");
                }
            }
        }
    }
