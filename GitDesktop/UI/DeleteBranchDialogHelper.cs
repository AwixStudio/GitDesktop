using System;
using ImGuiNET;
using System.Numerics;
using GitDesktop.Git;

namespace GitDesktop.UI
{
    /// <summary>
    /// Helper for deleting a branch with confirmation dialog and remote option
    /// </summary>
    public class DeleteBranchDialogHelper : IRender
    {
        private string branchNameToDelete = "";
        private bool deleteRemote = false;
        private readonly RepositoryManager repositoryManager;
        private bool isOpen = false;

        public DeleteBranchDialogHelper(RepositoryManager repositoryManager)
        {
            this.repositoryManager = repositoryManager;
        }

        /// <summary>
        /// Open the delete confirmation dialog
        /// </summary>
        public void Open(string branchName)
        {
            if (repositoryManager.CurrentRepository == null)
            {
                Logger.Log("Not selected repository");
                return;
            }

            // Check if the branch is the current branch
            if (repositoryManager.CurrentRepository.CurrentBranch.Name == branchName)
            {
                Logger.Log($"Cannot delete the currently active branch '{branchName}'");
                return;
            }

            branchNameToDelete = branchName;
            deleteRemote = false;
            isOpen = true;

            ViewManager.AddNewView?.Invoke(this);
        }

        public void Close()
        {
            isOpen = false;
            ViewManager.RemoveView?.Invoke(this);
        }

        /// <summary>
        /// Render the delete confirmation dialog
        /// </summary>
        public void Render()
        {
            if (!isOpen || repositoryManager.CurrentRepository == null)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(400, 200), ImGuiCond.FirstUseEver);

            if (ImGui.Begin("Delete Branch"))
            {
                ImGui.Text($"Are you sure you want to delete branch '{branchNameToDelete}'?");

                ImGui.Spacing();
                ImGui.Spacing();

                bool deleteRemoteChanged = ImGui.Checkbox("Also delete on remote", ref deleteRemote);

                ImGui.Spacing();
                ImGui.Separator();

                float buttonWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2;

                if (ImGui.Button("Delete", new Vector2(buttonWidth, 0)))
                {
                    DeleteBranch();
                }

                ImGui.SameLine();
                if (ImGui.Button("Cancel", new Vector2(buttonWidth, 0)))
                {
                    branchNameToDelete = "";
                    Close();
                }

                ImGui.End();
            }
        }

        private void DeleteBranch()
        {
            if (string.IsNullOrWhiteSpace(branchNameToDelete))
            {
                Logger.Log("Branch name cannot be empty");
                return;
            }

            try
            {
                GitService.DeleteBranch(
                    repositoryManager.CurrentRepository.Path,
                    branchNameToDelete,
                    deleteRemote
                );

                if (deleteRemote)
                {
                    Logger.Log($"Branch '{branchNameToDelete}' deleted locally and remotely");
                }
                else
                {
                    Logger.Log($"Branch '{branchNameToDelete}' deleted locally");
                }

                branchNameToDelete = "";
                Close();

                // Refresh branches list to remove the deleted branch
                repositoryManager.CurrentRepository?.RefreshBranches();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error during branch deletion: {ex.Message}");
            }
        }
    }
}
