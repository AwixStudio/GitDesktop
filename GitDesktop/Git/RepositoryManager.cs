using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitDesktop.Git
{
    public class RepositoryManager
    {
        private const string APP_CONFIG_FILE_NAME = "app-config.json";

        private readonly string appConfigPath;
        private AppConfig appConfig;

        private List<Repository> repositories = [];
        public IReadOnlyList<Repository> Repositories => repositories;
        public Repository? CurrentRepository { get; private set; }
        public GitFile? SelectedFile { get; set; }

        public RepositoryManager()
        {
            appConfigPath = Path.Combine(AppContext.BaseDirectory, APP_CONFIG_FILE_NAME);
            appConfig = new();
            
            LoadAppConfig();
        }

        private void LoadAppConfig()
        {
            if (!File.Exists(appConfigPath))            
                return;            

            try
            {
                var json = File.ReadAllText(appConfigPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                appConfig = JsonSerializer.Deserialize<AppConfig>(json, options) ?? new AppConfig();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during loading app config: {ex.Message}");
                appConfig = new AppConfig();
            }

            repositories.Clear();
            foreach (var path in appConfig.RepositoryPaths)
            {
                if (Directory.Exists(path))
                {
                    try
                    {
                        var repo = new Repository(new DirectoryInfo(path).Name, path);
                        repositories.Add(repo);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading repository at {path}: {ex.Message}");
                    }
                }
            }
            CurrentRepository = Repositories.FirstOrDefault(r => r.Path == appConfig.LastUsedRepository);

            GitService.ExecuteWithoutRepository(AppContext.BaseDirectory, "lfs install");
        }

        private void SaveAppConfig()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var json = JsonSerializer.Serialize(appConfig, options);
                File.WriteAllText(appConfigPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during saving app config: {ex.Message}");
            }
        }

        public void AddRepository(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"Directory does not exist: {path}");
            }

            if (Repositories.Any(r => r.Path == path))
            {
                throw new InvalidOperationException($"Repository already exists: {path}");
            }

            var repo = new Repository(new DirectoryInfo(path).Name, path);

            repositories.Add(repo);
            appConfig.RepositoryPaths.Add(path);
            SetCurrentRepository(repo);
        }

        public void RemoveRepository(string path)
        {
            var repo = Repositories.FirstOrDefault(r => r.Path == path);
            if (repo != null)
            {
                repositories.Remove(repo);
                appConfig.RepositoryPaths.Remove(path);

                // Clear last used repository if it was the one being removed
                if (appConfig.LastUsedRepository == path)                
                    appConfig.LastUsedRepository = null;

                // If the removed repository was the currently selected one, clear it
                if (CurrentRepository?.Path == path)
                {
                    CurrentRepository = null;
                }

                SaveAppConfig();
            }
        }

        public Repository? GetRepository(string path) => Repositories.FirstOrDefault(r => r.Path == path);
        public Repository GetRepository(int index) => Repositories[index];

        public void SetCurrentRepository(Repository repository)
        {
            if (!Directory.Exists(repository.Path))
            {
                throw new DirectoryNotFoundException($"Directory does not exist: {repository.Path}");
            }

            CurrentRepository = repository;
            appConfig.LastUsedRepository = repository.Path;
            SaveAppConfig();
        }        

        public string[] GetRepositoryPaths() => Repositories.Select(r => r.Path).ToArray();
        public string[] GetRepositoryNames() => Repositories.Select(r => r.Name).ToArray();

        public void RefreshChanges()
        {
            if(CurrentRepository != null)
            {
                GitStatus status = GitService.GetStatus(CurrentRepository.Path);
                CurrentRepository.RefreshChanges(status.Files);
            }
        }
    }

    internal class AppConfig
    {
        [JsonPropertyName("lastUsedRepository")]
        public string? LastUsedRepository { get; set; }

        [JsonPropertyName("repositoryPaths")]
        public List<string> RepositoryPaths { get; set; } = [];
    }

    public class Repository
    {
        private const int MAX_COMMITS_PER_PAGE = 100;

        public string Name { get; private set; }
        public string Path { get; private set; }
        public string DefaultBranchName { get; private set; }
        public GitBranch CurrentBranch { get; private set; }
        internal IRepositoryProvider Provider { get; private set; }

        private List<GitBranch> branches = [];
        public IReadOnlyList<GitBranch> Branches => branches;

        private List<GitFile> changes = [];
        public IReadOnlyList<GitFile> Changes => changes;

        private List<GitCommit> commits = [];
        public IReadOnlyList<GitCommit> Commits => commits;

        private bool hasMoreCommits = false;
        public bool HasMoreCommits => hasMoreCommits;

        public Repository(string name, string path)
        {
            Name = name;
            Path = path;
            DefaultBranchName = GitService.GetDefaultBranchName(path);

            (var branches, var currentBranch) = GitService.GetBranches(path);
            CurrentBranch = currentBranch;
            this.branches = branches;

            GitStatus status = GitService.GetStatus(Path);
            changes = status.Files;

            try
            {
                commits = GitService.GetCommitLog(Path, CurrentBranch.Name);
                CheckAndSetHasMoreCommits();
            }
            catch
            {
                commits = [];
                hasMoreCommits = false;
            }

            Provider = GitService.GetProvider(path);            
        }

        public void ChangeBranch(GitBranch newBranch)
        {
            if (!Branches.Any(b => b.Name == newBranch.Name))
            {
                throw new InvalidOperationException($"Branch {newBranch.Name} does not exist in repository {Name}");
            }

            GitStatus status = GitService.GetStatus(Path);
            if (status.IsClean)
            {
                try
                {
                    GitService.CheckoutBranch(Path, newBranch.Name);
                    CurrentBranch = newBranch;

                    status = GitService.GetStatus(Path);
                    changes = status.Files;

                    try
                    {
                        commits = GitService.GetCommitLog(Path, CurrentBranch.Name);
                        CheckAndSetHasMoreCommits();
                    }
                    catch
                    {
                        commits = [];
                        hasMoreCommits = false;
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to checkout branch {newBranch.Name}: {ex.Message}");
                }
            }
            else
            {
                Logger.Log("To switch branch you need first to clear all your current changes by committing, stashing or discarding them.");
            }
        }

        public void RefreshChanges(List<GitFile> newChanges)
        {
            // Remove files that are no longer in the status
            for (int i = changes.Count - 1; i >= 0; i--)
            {
                var existingFile = changes[i];
                if (!newChanges.Any(f => f.Path == existingFile.Path))
                {
                    changes.RemoveAt(i);
                }
            }

            // Update existing files and add new ones
            foreach (var newFile in newChanges)
            {
                var existingFile = changes.FirstOrDefault(f => f.Path == newFile.Path);
                if (existingFile != null)
                {
                    // Update existing file's state but preserve MarkedForCommit flag
                    var index = changes.IndexOf(existingFile);
                    var updatedFile = new GitFile
                    {
                        Path = newFile.Path,
                        IndexState = newFile.IndexState,
                        WorkingTreeState = newFile.WorkingTreeState,
                        MarkedForCommit = existingFile.MarkedForCommit
                    };
                    changes[index] = updatedFile;
                }
                else
                {
                    // Add new file
                    changes.Add(newFile);
                }
            }
        }

        public void CommitChanges(string commitMessage)
        {            
            try
            {
                GitService.CommitChanges(Path, commitMessage, changes);                                
                GitStatus status = GitService.GetStatus(Path);
                RefreshChanges(status.Files);

                try
                {
                    commits = GitService.GetCommitLog(Path, CurrentBranch.Name);
                    CheckAndSetHasMoreCommits();
                }
                catch
                {
                    commits = [];
                    hasMoreCommits = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to commit changes: {ex.Message}");
            }
        }

        public void DiscardChanges()
        {
            try
            {
                GitService.DiscardChanges(Path, changes);
                GitStatus status = GitService.GetStatus(Path);
                RefreshChanges(status.Files);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to discard changes: {ex.Message}");
            }
        }

        public void HardReset()
        {
            try
            {
                GitService.HardReset(Path);
                GitStatus status = GitService.GetStatus(Path);
                RefreshChanges(status.Files);
                try
                {
                    commits = GitService.GetCommitLog(Path, CurrentBranch.Name);
                    CheckAndSetHasMoreCommits();
                }
                catch
                {
                    commits = [];
                    hasMoreCommits = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to reset hard: {ex.Message}");
            }
        }

        public async Task<MergeConflictResult> UpdateFromMain(Action<string, float> onProgress)
        {
            try
            {
                var result = await GitService.UpdateFromMain(Path, onProgress);

                // If no conflicts, proceed with refresh
                if (!result.HasConflicts)
                {
                    // Refresh status after update
                    onProgress("Refreshing file status...", 97);
                    GitStatus status = GitService.GetStatus(Path);
                    RefreshChanges(status.Files);

                    // Refresh commits
                    onProgress("Refreshing commit history...", 99);
                    try
                    {
                        commits = GitService.GetCommitLog(Path, CurrentBranch.Name);
                        CheckAndSetHasMoreCommits();
                    }
                    catch
                    {
                        commits = [];
                        hasMoreCommits = false;
                    }

                    onProgress("Update completed!", 100);

                    // Refresh branches after update (new branches might have been fetched)
                    RefreshBranches();
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update from main: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh the list of branches from the repository
        /// </summary>
        public void RefreshBranches()
        {
            try
            {
                (var updatedBranches, var updatedCurrentBranch) = GitService.GetBranches(Path);
                branches = updatedBranches;
                CurrentBranch = updatedCurrentBranch;
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to refresh branches: {ex.Message}");
            }
        }

        public void OpenInGitCmd()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                WorkingDirectory = Path,
                UseShellExecute = true
            });
        }

        public void OpenInExplorer()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Path,
                UseShellExecute = true
            });
        }

        public void CreatePullRequest()
        {
            Provider.CreatePullRequest(Path, "Title", CurrentBranch.Name, "master");            
        }

        public void LoadMoreCommits()
        {
            try
            {
                // Get next batch of commits with offset
                var nextCommits = GitService.GetCommitLog(Path, CurrentBranch.Name, MAX_COMMITS_PER_PAGE, commits.Count);

                // If we got fewer commits than the limit, there are no more commits available
                hasMoreCommits = nextCommits.Count >= MAX_COMMITS_PER_PAGE;

                // Add new commits to the list
                commits.AddRange(nextCommits);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to load more commits: {ex.Message}");
            }
        }

        private void CheckAndSetHasMoreCommits()
        {
            try
            {
                // Check if there are more commits after the current batch
                var testCommits = GitService.GetCommitLog(Path, CurrentBranch.Name, 1, commits.Count);
                hasMoreCommits = testCommits.Count > 0;
            }
            catch
            {
                hasMoreCommits = false;
            }
        }
    }
}
