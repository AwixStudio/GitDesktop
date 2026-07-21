using System;
using ImGuiNET;
using System.Numerics;
using GitDesktop.Git;

namespace GitDesktop.UI
{
    /// <summary>
    /// Helper class for managing context menu in CommitHistoryView
    /// </summary>
    public class CommitContextMenuHelper
    {
        private string selectedCommitHash = "";
        private string branchNameInput = "";
        private bool showCreateBranchDialog = false;
        private readonly RepositoryManager repositoryManager;

        public CommitContextMenuHelper(RepositoryManager repositoryManager)
        {
            this.repositoryManager = repositoryManager;
        }

        /// <summary>
        /// Show the branch creation dialog (called from context menu)
        /// </summary>
        public void ShowCreateBranchDialog(string commitHash)
        {
            selectedCommitHash = commitHash;
            showCreateBranchDialog = true;
        }

        /// <summary>
        /// Perform cherry-pick (called from context menu)
        /// </summary>
        public void PerformCherryPickPublic(string commitHash)
        {
            PerformCherryPick(commitHash);
        }

        /// <summary>
        /// Render the branch creation dialog
        /// </summary>
        public void RenderCreateBranchDialog()
        {
            if (showCreateBranchDialog)
            {
                ImGui.SetNextWindowSize(new Vector2(300, 120), ImGuiCond.FirstUseEver);

                if (ImGui.Begin("Create new branch", ref showCreateBranchDialog))
                {
                    ImGui.Text("Branch name:");
                    ImGui.SetItemDefaultFocus();

                    if (ImGui.InputText("##BranchName", ref branchNameInput, 256, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        CreateBranch();
                    }

                    ImGui.Spacing();

                    if (ImGui.Button("Create", new Vector2(120, 0)))
                    {
                        CreateBranch();
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Cancel", new Vector2(120, 0)))
                    {
                        showCreateBranchDialog = false;
                        branchNameInput = "";
                    }

                    ImGui.End();
                }
            }
        }

        private void CreateBranch()
        {
            if (!string.IsNullOrWhiteSpace(branchNameInput))
            {
                try
                {
                    GitService.CreateBranch(
                        repositoryManager.CurrentRepository.Path, 
                        branchNameInput, 
                        selectedCommitHash
                    );
                    Logger.Log($"Branch '{branchNameInput}' created based on {selectedCommitHash}");
                    showCreateBranchDialog = false;
                    branchNameInput = "";

                    // Refresh branches to show the newly created branch
                    repositoryManager.CurrentRepository?.RefreshBranches();
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error during creating branch: {ex.Message}");
                }
            }
        }

        private void PerformCherryPick(string commitHash)
        {
            try
            {
                GitService.CherryPick(repositoryManager.CurrentRepository.Path, commitHash);
                Logger.Log($"Cherry-pick completed for {commitHash}");
                repositoryManager.RefreshChanges();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error during cherry-pick: {ex.Message}");
            }
        }
    }
}
