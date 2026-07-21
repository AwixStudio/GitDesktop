using System;
using ImGuiNET;
using System.Numerics;
using GitDesktop.Git;

namespace GitDesktop.UI
{
    /// <summary>
    /// Helper for creating a new branch with base branch selection
    /// </summary>
    public class CreateBranchDialogHelper : IRender
    {
        private string branchNameInput = "";
        private int selectedBaseIndex = 0;
        private readonly RepositoryManager repositoryManager;

        public CreateBranchDialogHelper(RepositoryManager repositoryManager)
        {
            this.repositoryManager = repositoryManager;
        }

        /// <summary>
        /// Open the dialog
        /// </summary>
        public void Open()
        {
            if (repositoryManager.CurrentRepository == null)
            {
                Logger.Log("Not selected repository");
                return;
            }

            branchNameInput = "";
            selectedBaseIndex = 0;

            ViewManager.AddNewView?.Invoke(this);
        }

        public void Close()
        {
            ViewManager.RemoveView?.Invoke(this);
        }

        /// <summary>
        /// Render the dialog
        /// </summary>
        public void Render()
        {
            if (repositoryManager.CurrentRepository == null)
            {
                return;
            }

            var branches = repositoryManager.CurrentRepository.Branches;
            string[] branchNames = branches.Select(b => b.Name).ToArray();

            ImGui.SetNextWindowSize(new Vector2(400, 200), ImGuiCond.FirstUseEver);

            if (ImGui.Begin("Create new branch"))
            {
                ImGui.Text("Branch name:");
                ImGui.SetItemDefaultFocus();
                ImGui.InputText("##BranchName", ref branchNameInput, 256);

                ImGui.Spacing();
                ImGui.Text("Based on:");
                ImGui.SetNextItemWidth(-1);
                ImGui.Combo("##BaseBranch", ref selectedBaseIndex, branchNames, branchNames.Length);

                ImGui.Spacing();
                ImGui.Separator();

                float buttonWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2;

                if (ImGui.Button("Create", new Vector2(buttonWidth, 0)))
                {
                    CreateBranch();
                }

                ImGui.SameLine();
                if (ImGui.Button("Cancel", new Vector2(buttonWidth, 0)))
                {                    
                    branchNameInput = "";
                    Close();
                }

                ImGui.End();
            }
        }

        private void CreateBranch()
        {
            if (string.IsNullOrWhiteSpace(branchNameInput))
            {
                Logger.Log("Name cannot be empty");
                return;
            }

            try
            {
                var selectedBranch = repositoryManager.CurrentRepository?.Branches.ElementAt(selectedBaseIndex);
                if (selectedBranch == null)
                {
                    Logger.Log("Not selected base branch");
                    return;
                }

                // Get the commit hash of the selected base branch
                GitService.CreateBranch(
                    repositoryManager.CurrentRepository.Path,
                    branchNameInput,
                    selectedBranch.Name
                );

                Logger.Log($"Branch '{branchNameInput}' created based on '{selectedBranch.Name}'");                
                branchNameInput = "";
                Close();

                // Refresh changes to update the UI (which includes branches)
                repositoryManager.RefreshChanges();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error during branch creation: {ex.Message}");
            }
        }
    }
}
