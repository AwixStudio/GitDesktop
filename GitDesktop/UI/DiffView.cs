using GitDesktop.Git;
using ImGuiNET;
using System;
using System.Numerics;

namespace GitDesktop.UI
{
    internal class DiffView : IRender
    {
        private readonly RepositoryManager repositoryManager;
        private string currentDiffContent = string.Empty;
        private GitFile? lastDisplayedFile = null;

        public DiffView(RepositoryManager repositoryManager)
        {
            this.repositoryManager = repositoryManager;
        }

        public void Render()
        {
            ImGuiWindowFlags flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoCollapse;
            ImGui.SetNextWindowDockID(ImGui.GetID("RightPanel"), ImGuiCond.FirstUseEver);

            float yOffset = ImGui.GetFrameHeightWithSpacing() + 55f;
            float windowHeight = ImGui.GetIO().DisplaySize.Y - yOffset - 5f;
            float windowWidth = ImGui.GetIO().DisplaySize.X - 560f - 10f;

            ImGui.SetNextWindowPos(new Vector2(560, yOffset), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight), ImGuiCond.FirstUseEver);

            ImGui.Begin("File Diff", flags);
            RenderContent();
            ImGui.End();
        }

        public void RenderContent()
        {
            Repository? currentRepository = repositoryManager.CurrentRepository;
            GitFile? selectedFile = repositoryManager.SelectedFile;

            if (selectedFile != null && selectedFile != lastDisplayedFile)
            {
                // Load diff for newly selected file
                lastDisplayedFile = selectedFile;
                RefreshDiff(currentRepository, selectedFile);
            }

            if (selectedFile == null)
            {
                ImGui.TextDisabled("Select a file from the changes list to view its diff");
                return;
            }

            ImGui.Text($"File: {selectedFile.Path}");
            ImGui.TextDisabled($"State: {selectedFile.WorkingTreeState}");
            ImGui.Separator();

            // Display diff content in a scrollable area
            if (ImGui.BeginChild("DiffContent", new Vector2(-1, -1)))
            {
                if (string.IsNullOrEmpty(currentDiffContent))
                {
                    ImGui.TextDisabled("Loading diff...");
                }
                else
                {
                    // Use monospace font for diff (if available)
                    ImGui.TextUnformatted(currentDiffContent);
                }
                ImGui.EndChild();
            }
        }

        private void RefreshDiff(Repository? repository, GitFile file)
        {
            if (repository == null)
            {
                currentDiffContent = "No repository selected";
                return;
            }

            try
            {
                if (!file.IsDiffLoaded)
                {
                    string diff = GitService.GetFileDiff(repository.Path, file.Path);
                    file.SetCachedDiff(diff);
                }

                currentDiffContent = file.GetCachedDiff();
            }
            catch (Exception ex)
            {
                currentDiffContent = $"Error loading diff: {ex.Message}";
            }
        }
    }
}
