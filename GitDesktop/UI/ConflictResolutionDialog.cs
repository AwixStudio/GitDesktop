using ImGuiNET;
using System.Numerics;
using GitDesktop.Git;

namespace GitDesktop.UI
{
    internal class ConflictResolutionDialog : IRender
    {
        private bool isPopupOpened = false;
        private List<ConflictedFile> conflictedFiles = [];
        private bool isResolving = false;
        private bool isResolved = false;
        private string? errorMessage;
        private Repository? repository;
        private bool userCancelled = false;
        private Action? onResolutionComplete;

        public ConflictResolutionDialog(Repository repository, List<ConflictedFile> conflicts, Action? onComplete = null)
        {
            this.repository = repository;
            this.conflictedFiles = conflicts;
            this.onResolutionComplete = onComplete;
            ViewManager.AddNewView?.Invoke(this);
        }

        public bool IsResolved => isResolved;
        public bool WasCancelled => userCancelled;

        public void Render()
        {
            if (!isPopupOpened)
            {
                ImGui.OpenPopup("Resolve Merge Conflicts");
                isPopupOpened = true;
            }

            ImGui.SetNextWindowSizeConstraints(new Vector2(600, 400), new Vector2(900, 700));

            if (ImGui.BeginPopupModal("Resolve Merge Conflicts", ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("The merge resulted in conflicts. Please resolve them:");
                ImGui.Spacing();

                if (ImGui.BeginChild("ConflictsList", new Vector2(-1, 250)))
                {
                    for (int i = 0; i < conflictedFiles.Count; i++)
                    {
                        var conflict = conflictedFiles[i];
                        ImGui.Separator();
                        ImGui.Text($"File: {conflict.Path}");
                        ImGui.Spacing();

                        // Button width for resolution options
                        float buttonWidth = 120;
                        float spacing = 10f;

                        if (conflict.Resolution == null)
                        {
                            if (ImGui.Button($"Use Ours###ours_{i}", new Vector2(buttonWidth, 0)))
                            {
                                conflict.Resolution = "ours";
                            }

                            ImGui.SameLine(0, spacing);

                            if (ImGui.Button($"Use Theirs###theirs_{i}", new Vector2(buttonWidth, 0)))
                            {
                                conflict.Resolution = "theirs";
                            }
                        }
                        else
                        {
                            string resolutionText = conflict.Resolution == "ours" ? "Using local version" : "Using incoming version";
                            ImGui.TextColored(new Vector4(0, 1, 0, 1), resolutionText);

                            ImGui.SameLine(0, spacing);

                            if (ImGui.Button($"Change###change_{i}", new Vector2(60, 0)))
                            {
                                conflict.Resolution = null;
                            }
                        }
                    }
                    ImGui.EndChild();
                }

                ImGui.Spacing();

                // Status and error display
                int resolvedCount = conflictedFiles.Count(c => c.Resolution != null);
                ImGui.Text($"Resolved: {resolvedCount}/{conflictedFiles.Count}");

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Error: {errorMessage}");
                }

                ImGui.Spacing();
                ImGui.Separator();

                // Buttons
                bool allResolved = conflictedFiles.All(c => c.Resolution != null);

                if (isResolving)
                {
                    ImGui.BeginDisabled();
                    ImGui.Button("Applying resolutions...", new Vector2(150, 0));
                    ImGui.EndDisabled();
                }
                else if (allResolved)
                {
                    if (ImGui.Button("Apply Resolutions", new Vector2(150, 0)))
                    {
                        ApplyResolutions();
                    }
                }
                else
                {
                    ImGui.BeginDisabled();
                    ImGui.Button("Apply Resolutions", new Vector2(150, 0));
                    ImGui.EndDisabled();
                    ImGui.SetItemTooltip("Please resolve all conflicts before applying");
                }

                ImGui.SameLine(0, 10);

                if (ImGui.Button("Cancel Merge", new Vector2(150, 0)))
                {
                    CancelMerge();
                }

                if (isResolved)
                {
                    ImGui.SameLine(0, 10);

                    if (ImGui.Button("Close", new Vector2(100, 0)))
                    {
                        Close();
                    }
                }

                ImGui.EndPopup();
            }
        }

        private void ApplyResolutions()
        {
            if (repository == null)
                return;

            isResolving = true;
            errorMessage = null;

            try
            {
                foreach (var conflict in conflictedFiles)
                {
                    if (conflict.Resolution != null)
                    {
                        GitService.ResolveConflict(repository.Path, conflict.Path, conflict.Resolution);
                    }
                }

                // Complete the merge
                GitService.CompleteMerge(repository.Path, "Merge from main branch - conflicts resolved");

                // Refresh the repository after successful merge
                try
                {
                    var status = GitService.GetStatus(repository.Path);
                    repository.RefreshChanges(status.Files);
                    repository.RefreshBranches();

                    // Also refresh commits
                    try
                    {
                        var commits = GitService.GetCommitLog(repository.Path, repository.CurrentBranch.Name);
                        // Note: Repository class doesn't expose a public setter for commits, 
                        // but the commit refresh happens through ChangeBranch or similar methods
                        // The important part is that changes are refreshed
                    }
                    catch { }
                }
                catch { }

                // Call the completion callback to refresh the UI
                onResolutionComplete?.Invoke();

                isResolved = true;
                isResolving = false;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                isResolving = false;
            }
        }

        private void CancelMerge()
        {
            if (repository == null)
                return;

            try
            {
                GitService.AbortMerge(repository.Path);
                userCancelled = true;
                Close();
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to abort merge: {ex.Message}";
            }
        }

        private void Close()
        {
            ViewManager.RemoveView?.Invoke(this);
        }
    }
}
