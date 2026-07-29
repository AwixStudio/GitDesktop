using GitDesktop.Git;
using ImGuiNET;
using NativeFileDialogNET;
using System.Text;

namespace GitDesktop.UI
{
    internal class CreateRepositoryDialog : IRender
    {
        private readonly RepositoryManager repositoryManager;
        private bool isOpen = false;
        private string repositoryName = "";
        private string selectedPath = "";
        private string branchName = "main";
        private string description = "";
        private string errorMessage = "";
        private bool isCreating = false;
        private int repositoryType = 0; // 0 = Local, 1 = GitHub
        private bool isPrivate = false;
        private const int TextInputBufferSize = 1024;
        private byte[] nameInputBuffer;
        private byte[] pathInputBuffer;
        private byte[] branchInputBuffer;
        private byte[] descriptionInputBuffer;

        public bool IsOpen => isOpen;

        public CreateRepositoryDialog(RepositoryManager repositoryManager)
        {
            this.repositoryManager = repositoryManager;
            this.nameInputBuffer = new byte[TextInputBufferSize];
            this.pathInputBuffer = new byte[TextInputBufferSize];
            this.branchInputBuffer = new byte[TextInputBufferSize];
            this.descriptionInputBuffer = new byte[TextInputBufferSize];
        }

        public void Open()
        {
            isOpen = true;
            repositoryName = "";
            selectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            branchName = "main";
            description = "";
            errorMessage = "";
            isCreating = false;
            repositoryType = 0; // Default to Local
            isPrivate = false;
            Array.Clear(nameInputBuffer, 0, nameInputBuffer.Length);
            Array.Clear(pathInputBuffer, 0, pathInputBuffer.Length);
            Array.Clear(branchInputBuffer, 0, branchInputBuffer.Length);
            Array.Clear(descriptionInputBuffer, 0, descriptionInputBuffer.Length);

            // Set default path (only for local)
            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                SetPath(selectedPath);
            }

            var branchBytes = Encoding.UTF8.GetBytes(branchName);
            Array.Copy(branchBytes, branchInputBuffer, Math.Min(branchBytes.Length, branchInputBuffer.Length - 1));

            ViewManager.AddNewView?.Invoke(this);
        }

        public void Close()
        {
            isOpen = false;
            ViewManager.RemoveView?.Invoke(this);
        }

        public void Render()
        {
            if (!isOpen)
                return;

            ImGui.OpenPopup("Create New Repository");

            if (ImGui.BeginPopupModal("Create New Repository", ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.TextWrapped("Create a new Git repository:");
                ImGui.Spacing();

                // Repository type selector
                ImGui.Text("Repository Type:");
                ImGui.RadioButton("Local Repository##Type", ref repositoryType, 0);
                ImGui.SameLine();
                ImGui.RadioButton("GitHub Repository##Type", ref repositoryType, 1);
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // Repository name input
                ImGui.Text("Repository Name:");
                if (ImGui.InputText("##RepositoryName", nameInputBuffer, (uint)TextInputBufferSize))
                {
                    repositoryName = Encoding.UTF8.GetString(nameInputBuffer).TrimEnd('\0');
                }

                ImGui.Spacing();

                // Location (only for local)
                if (repositoryType == 0)
                {
                    ImGui.Text("Location:");
                    if (ImGui.InputText("##RepositoryPath", pathInputBuffer, (uint)TextInputBufferSize))
                    {
                        selectedPath = Encoding.UTF8.GetString(pathInputBuffer).TrimEnd('\0');
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("Browse...##PathBrowse"))
                    {
                        using var dialog = new NativeFileDialog().SelectFolder();

                        var result = dialog.Open(out string? folder);

                        if (result == DialogResult.Okay && folder != null)
                        {
                            SetPath(folder);
                        }
                    }

                    ImGui.Spacing();
                }

                // Description (for GitHub)
                if (repositoryType == 1)
                {
                    ImGui.Text("Description (optional):");
                    if (ImGui.InputText("##Description", descriptionInputBuffer, (uint)TextInputBufferSize))
                    {
                        description = Encoding.UTF8.GetString(descriptionInputBuffer).TrimEnd('\0');
                    }

                    ImGui.Spacing();

                    ImGui.Checkbox("Private Repository", ref isPrivate);
                    ImGui.Spacing();
                }

                // Default branch name input
                ImGui.Text("Default Branch:");
                if (ImGui.InputText("##BranchName", branchInputBuffer, (uint)TextInputBufferSize))
                {
                    branchName = Encoding.UTF8.GetString(branchInputBuffer).TrimEnd('\0');
                }

                if (string.IsNullOrWhiteSpace(branchName))
                    branchName = "main";

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // Preview of full path (for local)
                if (repositoryType == 0 && !string.IsNullOrWhiteSpace(selectedPath) && !string.IsNullOrWhiteSpace(repositoryName))
                {
                    string fullPath = Path.Combine(selectedPath, repositoryName);
                    ImGui.TextWrapped($"Full path: {fullPath}");
                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();
                }

                // Error message display
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), errorMessage);
                    ImGui.Spacing();
                }

                // Buttons
                float buttonWidth = 100;
                float spacing = ImGui.GetStyle().ItemSpacing.X;
                float buttonsWidth = (buttonWidth * 2) + spacing;
                float availableWidth = ImGui.GetContentRegionAvail().X;

                ImGui.SetCursorPosX((availableWidth - buttonsWidth) / 2 + ImGui.GetCursorPosX());

                bool canCreate = !string.IsNullOrWhiteSpace(repositoryName) &&
                                (repositoryType == 1 || !string.IsNullOrWhiteSpace(selectedPath));

                ImGui.BeginDisabled(isCreating || !canCreate);

                if (ImGui.Button("Create", new System.Numerics.Vector2(buttonWidth, 0)))
                {
                    TryCreateRepository();
                }

                ImGui.EndDisabled();

                ImGui.SetItemDefaultFocus();
                ImGui.SameLine();

                if (ImGui.Button("Cancel", new System.Numerics.Vector2(buttonWidth, 0)))
                {
                    Close();
                }

                ImGui.EndPopup();
            }
        }

        private void SetPath(string path)
        {
            selectedPath = path;

            Array.Clear(pathInputBuffer, 0, pathInputBuffer.Length);

            var bytes = Encoding.UTF8.GetBytes(path);

            Array.Copy(
                bytes,
                pathInputBuffer,
                Math.Min(bytes.Length, pathInputBuffer.Length - 1));
        }

        private void TryCreateRepository()
        {
            errorMessage = "";

            if (string.IsNullOrWhiteSpace(repositoryName))
            {
                errorMessage = "Please specify a repository name.";
                return;
            }

            // Validate repository name (no invalid path characters)
            var invalidChars = Path.GetInvalidFileNameChars();
            if (repositoryName.Any(c => invalidChars.Contains(c)))
            {
                errorMessage = "Repository name contains invalid characters.";
                return;
            }

            isCreating = true;

            try
            {
                if (repositoryType == 0)
                {
                    // Local repository
                    CreateLocalRepository();
                }
                else if (repositoryType == 1)
                {
                    // GitHub repository
                    CreateGitHubRepository();
                }

                isCreating = false;
                Close();
            }
            catch (InvalidOperationException ex)
            {
                errorMessage = $"Repository error: {ex.Message}";
                isCreating = false;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error: {ex.Message}";
                isCreating = false;
            }
        }

        private void CreateLocalRepository()
        {
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                errorMessage = "Please specify a location.";
                return;
            }

            if (!Directory.Exists(selectedPath))
            {
                errorMessage = "Selected directory does not exist.";
                return;
            }

            // Create the repository directory
            string repositoryPath = Path.Combine(selectedPath, repositoryName);

            if (Directory.Exists(repositoryPath))
            {
                errorMessage = "A directory with this name already exists.";
                return;
            }

            // Initialize the git repository
            GitService.InitRepository(repositoryPath, string.IsNullOrWhiteSpace(branchName) ? "main" : branchName);

            // Add to repository manager
            repositoryManager.AddRepository(repositoryPath);
        }

        private void CreateGitHubRepository()
        {
            // Create repository on GitHub using API
            var (htmlUrl, cloneUrl) = GitService.CreateRepositoryOnGitHub(
                repositoryName,
                description,
                isPrivate
            );

            // Clone the repository locally
            string localPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                repositoryName
            );

            // Ensure directory doesn't exist
            if (Directory.Exists(localPath))
            {
                // Try to find an alternative name
                int counter = 2;
                string basePath = localPath;
                while (Directory.Exists(localPath))
                {
                    localPath = basePath + counter;
                    counter++;
                }
            }

            // Clone the repository
            GitService.CloneAsync(cloneUrl, localPath, null).Wait();

            // Add to repository manager
            repositoryManager.AddRepository(localPath);

            // Log as single message
            string successMessage = $"Repository created on GitHub: {htmlUrl}\n\nRepository cloned to: {localPath}";
            Logger.Log(successMessage);
        }
    }
}
