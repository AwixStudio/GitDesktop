using GitDesktop.Git;
using ImGuiNET;
using NativeFileDialogNET;
using System.Text;

namespace GitDesktop.UI
{
    internal class AddExistingRepositoryDialog : IRender
    {
        private readonly RepositoryManager repositoryManager;
        private bool isOpen = false;
        private string selectedPath = "";
        private string errorMessage = "";
        private const int TextInputBufferSize = 1024;
        private byte[] pathInputBuffer;

        public bool IsOpen => isOpen;

        public AddExistingRepositoryDialog(RepositoryManager repositoryManager)
        {
            this.repositoryManager = repositoryManager;
            this.pathInputBuffer = new byte[TextInputBufferSize];
        }

        public void Open()
        {
            isOpen = true;
            selectedPath = "";
            errorMessage = "";
            Array.Clear(pathInputBuffer, 0, pathInputBuffer.Length);

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

            ImGui.OpenPopup("Add Existing Repository");

            if (ImGui.BeginPopupModal("Add Existing Repository", ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.TextWrapped("Select a path to an existing Git repository:");
                ImGui.Spacing();

                // Path input field
                if (ImGui.InputText("Repository Path", pathInputBuffer, (uint)TextInputBufferSize))
                {
                    selectedPath = System.Text.Encoding.UTF8.GetString(pathInputBuffer).TrimEnd('\0');
                }

                if (ImGui.Button("Browse..."))
                {
                    using var dialog = new NativeFileDialog().SelectFolder();

                    var result = dialog.Open(out string? folder);

                    if (result == DialogResult.Okay && folder != null)
                    {
                        SetPath(folder);
                    }
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

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

                if (ImGui.Button("Add", new System.Numerics.Vector2(buttonWidth, 0)))
                {
                    TryAddRepository();
                }

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

        private void TryAddRepository()
        {
            errorMessage = "";

            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                errorMessage = "Please specify a path.";
                return;
            }

            if (!Directory.Exists(selectedPath))
            {
                errorMessage = "Directory does not exist.";
                return;
            }

            // Check if it's a git repository
            string gitPath = Path.Combine(selectedPath, ".git");
            if (!Directory.Exists(gitPath))
            {
                errorMessage = "Selected directory is not a Git repository (.git folder not found).";
                return;
            }

            try
            {
                repositoryManager.AddRepository(selectedPath);
                Close();
            }
            catch (InvalidOperationException ex)
            {
                errorMessage = $"Repository already exists. {ex.Message}";
            }
            catch (Exception ex)
            {
                errorMessage = $"Error: {ex.Message}";
            }
        }
    }
}
