using GitDesktop.Git;
using ImGuiNET;
using NativeFileDialogNET;
using System.Text;

namespace GitDesktop.UI
{
    internal class CloneRepositoryDialog : IRender
    {
        private readonly RepositoryManager repositoryManager;
        private bool isOpen = false;
        private string destinationPath = "";
        private string repositoryUrl = "";
        private string statusMessage = "";
        private string errorMessage = "";
        private bool isCloning = false;
        private const int TextInputBufferSize = 1024;
        private byte[] destinationPathBuffer;
        private byte[] repositoryUrlBuffer;

        public bool IsOpen => isOpen;

        public CloneRepositoryDialog(RepositoryManager repositoryManager)
        {
            this.repositoryManager = repositoryManager;
            this.destinationPathBuffer = new byte[TextInputBufferSize];
            this.repositoryUrlBuffer = new byte[TextInputBufferSize];
        }

        public void Open()
        {
            isOpen = true;
            destinationPath = "";
            repositoryUrl = "";
            statusMessage = "";
            errorMessage = "";
            isCloning = false;
            Array.Clear(destinationPathBuffer, 0, destinationPathBuffer.Length);
            Array.Clear(repositoryUrlBuffer, 0, repositoryUrlBuffer.Length);

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

            ImGui.OpenPopup("Clone Repository");

            if (ImGui.BeginPopupModal("Clone Repository", ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.TextWrapped("Enter the repository URL and destination folder:");
                ImGui.Spacing();

                // Repository URL input field
                ImGui.TextUnformatted("Repository URL:");
                if (ImGui.InputText("##repositoryUrl", repositoryUrlBuffer, (uint)TextInputBufferSize))
                {
                    repositoryUrl = Encoding.UTF8.GetString(repositoryUrlBuffer).TrimEnd('\0');
                }

                ImGui.Spacing();

                // Destination path input field
                ImGui.TextUnformatted("Destination Folder:");
                if (ImGui.InputText("##destinationPath", destinationPathBuffer, (uint)TextInputBufferSize))
                {
                    destinationPath = Encoding.UTF8.GetString(destinationPathBuffer).TrimEnd('\0');
                }

                if (ImGui.Button("Browse..."))
                {
                    using var dialog = new NativeFileDialog().SelectFolder();

                    var result = dialog.Open(out string? folder);

                    if (result == DialogResult.Okay && folder != null)
                    {
                        SetDestinationPath(folder);
                    }
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // Status message display
                if (!string.IsNullOrEmpty(statusMessage))
                {
                    ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0, 1), statusMessage);
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

                ImGui.BeginDisabled(isCloning || string.IsNullOrWhiteSpace(repositoryUrl) || string.IsNullOrWhiteSpace(destinationPath));
                if (ImGui.Button("Clone", new System.Numerics.Vector2(buttonWidth, 0)))
                {
                    TryCloneRepository();
                }
                ImGui.EndDisabled();

                ImGui.SetItemDefaultFocus();
                ImGui.SameLine();

                ImGui.BeginDisabled(isCloning);
                if (ImGui.Button("Cancel", new System.Numerics.Vector2(buttonWidth, 0)))
                {
                    Close();
                }
                ImGui.EndDisabled();

                ImGui.EndPopup();
            }
        }

        private void SetDestinationPath(string path)
        {
            destinationPath = path;
            Array.Clear(destinationPathBuffer, 0, destinationPathBuffer.Length);
            byte[] pathBytes = Encoding.UTF8.GetBytes(destinationPath);
            Array.Copy(pathBytes, destinationPathBuffer, Math.Min(pathBytes.Length, TextInputBufferSize - 1));
        }

        private void TryCloneRepository()
        {
            if (string.IsNullOrWhiteSpace(repositoryUrl) || string.IsNullOrWhiteSpace(destinationPath))
            {
                errorMessage = "Both repository URL and destination folder are required.";
                return;
            }

            // Validate URL format
            if (!IsValidRepositoryUrl(repositoryUrl))
            {
                errorMessage = "Invalid repository URL format.";
                return;
            }

            // Validate destination path
            try
            {
                string parentDir = Path.GetDirectoryName(destinationPath);
                if (string.IsNullOrEmpty(parentDir))
                {
                    errorMessage = "Invalid destination path.";
                    return;
                }

                // Check if parent directory exists
                if (!Directory.Exists(parentDir))
                {
                    errorMessage = "Parent directory does not exist.";
                    return;
                }

                // Check if destination already exists
                if (Directory.Exists(destinationPath))
                {
                    errorMessage = "Destination folder already exists.";
                    return;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Invalid path: {ex.Message}";
                return;
            }

            errorMessage = "";
            statusMessage = "Cloning repository...";
            isCloning = true;

            CloneRepositoryAsync();
        }

        private bool IsValidRepositoryUrl(string url)
        {
            // Basic validation: Should contain .git or be a valid GitHub/GitLab URL
            return (url.Contains(".git", StringComparison.OrdinalIgnoreCase) ||
                    url.Contains("github.com", StringComparison.OrdinalIgnoreCase) ||
                    url.Contains("gitlab.com", StringComparison.OrdinalIgnoreCase) ||
                    Uri.TryCreate(url, UriKind.Absolute, out _));
        }

        private async void CloneRepositoryAsync()
        {
            try
            {
                await GitService.CloneAsync(repositoryUrl, destinationPath, (progress) =>
                {
                    statusMessage = $"Cloning: {progress}";
                });

                statusMessage = "Repository cloned successfully!";
                isCloning = false;

                // Add repository to the manager
                repositoryManager.AddRepository(destinationPath);

                // Close dialog after a short delay to show success message
                await Task.Delay(2000);
                if (isOpen)
                {
                    Close();
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Clone failed: {ex.Message}";
                statusMessage = "";
                isCloning = false;
            }
        }
    }
}
