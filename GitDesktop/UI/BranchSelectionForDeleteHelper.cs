using System;
using ImGuiNET;
using System.Numerics;
using GitDesktop.Git;

namespace GitDesktop.UI
{
    /// <summary>
    /// Helper for selecting a branch to delete
    /// </summary>
    public class BranchSelectionForDeleteHelper : IRender
    {
        private int selectedBranchIndex = 0;
        private readonly RepositoryManager repositoryManager;
        private readonly DeleteBranchDialogHelper deleteBranchDialogHelper;
        private bool isOpen = false;

        public BranchSelectionForDeleteHelper(RepositoryManager repositoryManager, DeleteBranchDialogHelper deleteBranchDialogHelper)
        {
            this.repositoryManager = repositoryManager;
            this.deleteBranchDialogHelper = deleteBranchDialogHelper;
            isOpen = true;
        }

        public void Close()
        {
            isOpen = false;
            ViewManager.RemoveView?.Invoke(this);
        }

        public void Render()
        {
            if (!isOpen || repositoryManager.CurrentRepository == null)
            {
                return;
            }

            var branches = repositoryManager.CurrentRepository.Branches;
            if (branches.Count == 0)
            {
                Logger.Log("No branches available");
                Close();
                return;
            }

            string[] branchNames = branches.Select(b => b.Name).ToArray();

            ImGui.SetNextWindowSize(new Vector2(400, 250), ImGuiCond.FirstUseEver);

            if (ImGui.Begin("Select Branch to Delete"))
            {
                ImGui.Text("Select a branch to delete:");
                ImGui.SetNextItemWidth(-1);
                ImGui.ListBox("##BranchList", ref selectedBranchIndex, branchNames, branchNames.Length, 8);

                ImGui.Spacing();
                ImGui.Separator();

                float buttonWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2;

                if (ImGui.Button("Delete", new Vector2(buttonWidth, 0)))
                {
                    if (selectedBranchIndex >= 0 && selectedBranchIndex < branches.Count)
                    {
                        string selectedBranchName = branches[selectedBranchIndex].Name;
                        Close();
                        deleteBranchDialogHelper.Open(selectedBranchName);
                    }
                }

                ImGui.SameLine();
                if (ImGui.Button("Cancel", new Vector2(buttonWidth, 0)))
                {
                    Close();
                }

                ImGui.End();
            }
        }
    }
}
