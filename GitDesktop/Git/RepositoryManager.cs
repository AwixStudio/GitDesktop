using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitDesktop.Git
{
    internal class RepositoryManager
    {
        private const string CONFIG_FILE_NAME = "repositories.json";
        private const string APP_CONFIG_FILE_NAME = "app-config.json";

        private readonly string configPath;
        private readonly string appConfigPath;
        private AppConfig appConfig;

        public List<Repository> Repositories { get; private set; } = [];
        public Repository? CurrentRepository { get; private set; }

        public RepositoryManager()
        {
            configPath = Path.Combine(AppContext.BaseDirectory, CONFIG_FILE_NAME);
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

            Repositories.Clear();
            foreach (var path in appConfig.RepositoryPaths)
            {
                if (Directory.Exists(path))
                {
                    try
                    {
                        var repo = new Repository(new DirectoryInfo(path).Name, path);
                        Repositories.Add(repo);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading repository at {path}: {ex.Message}");
                    }
                }
            }
            CurrentRepository = Repositories.FirstOrDefault(r => r.Path == appConfig.LastUsedRepository);
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

            Repositories.Add(repo);
            appConfig.RepositoryPaths.Add(path);
            SetCurrentRepository(repo);
        }

        public void RemoveRepository(string path)
        {
            var repo = Repositories.FirstOrDefault(r => r.Path == path);
            if (repo != null)
            {
                Repositories.Remove(repo);
                appConfig.RepositoryPaths.Remove(path);

                // Clear last used repository if it was the one being removed
                if (appConfig.LastUsedRepository == path)                
                    appConfig.LastUsedRepository = null;
                                
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
    }

    internal class AppConfig
    {
        [JsonPropertyName("lastUsedRepository")]
        public string? LastUsedRepository { get; set; }

        [JsonPropertyName("repositoryPaths")]
        public List<string> RepositoryPaths { get; set; } = [];
    }

    internal class Repository
    {
        public string Name { get; private set; }
        public string Path { get; private set; }
        public GitBranch CurrentBranch { get; private set; }        
        public List<GitBranch> Branches { get; private set; }

        public Repository(string name, string path)
        {
            Name = name;
            Path = path;

            (var branches, var currentBranch) = GitService.GetBranches(path);
            CurrentBranch = currentBranch;
            Branches = branches;
        }

        public void ChangeBranch(GitBranch newBranch)
        {
            if (!Branches.Any(b => b.Name == newBranch.Name))
            {
                throw new InvalidOperationException($"Branch {newBranch.Name} does not exist in repository {Name}");
            }
            CurrentBranch = newBranch;
        }
    }
}
