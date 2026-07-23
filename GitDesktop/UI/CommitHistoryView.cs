using GitDesktop.Git;
using ImGuiNET;
using System;
using System.Collections.Generic;

namespace GitDesktop.UI
{
    internal class CommitHistoryView : IRender
    {
        private readonly RepositoryManager repositoryManager;
        private int selectedCommitIndex = -1;
        private string selectedCommitDetails = string.Empty;
        private CommitContextMenuHelper contextMenuHelper;

        public CommitHistoryView(RepositoryManager repositoryManager)
        {
            this.repositoryManager = repositoryManager;
            this.contextMenuHelper = new CommitContextMenuHelper(repositoryManager);
        }

        public void Render()
        {
            ImGuiWindowFlags flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoCollapse;
            ImGui.SetNextWindowDockID(ImGui.GetID("RightPanel"), ImGuiCond.Always);

            float yOffset = ImGui.GetFrameHeightWithSpacing() + 55f;
            float windowHeight = ImGui.GetIO().DisplaySize.Y - yOffset - 5f;
            float windowWidth = ImGui.GetIO().DisplaySize.X - 560f - 10f;

            ImGui.SetNextWindowPos(new System.Numerics.Vector2(560, yOffset), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(windowWidth, windowHeight), ImGuiCond.Always);

            ImGui.Begin("Commit History", flags);

            Repository? currentRepository = repositoryManager.CurrentRepository;
            if (currentRepository == null)
            {
                ImGui.TextDisabled("No repository selected");
                ImGui.End();
                return;
            }

            var commits = currentRepository.Commits;

            if (commits.Count == 0)
            {
                ImGui.TextDisabled("No commits to display");
                ImGui.End();
                return;
            }

            ImGui.Text($"Commits: {commits.Count}");
            ImGui.Separator();

            // Container for the commits list with fixed height
            if (ImGui.BeginChild("CommitsListContainer", new System.Numerics.Vector2(-1, -5f)))
            {
                if (ImGui.BeginTable("###CommitsList", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit))
                {
                    ImGui.TableSetupColumn("Hash", ImGuiTableColumnFlags.WidthFixed, 80);
                    ImGui.TableSetupColumn("Author", ImGuiTableColumnFlags.WidthFixed, 120);
                    ImGui.TableSetupColumn("Date", ImGuiTableColumnFlags.WidthFixed, 150);
                    ImGui.TableSetupColumn("Message", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableHeadersRow();

                    int commitIndex = 0;
                    foreach (var commit in commits)
                    {
                        ImGui.TableNextRow();

                        // Hash column
                        ImGui.TableSetColumnIndex(0);
                        string shortHash = commit.Hash.Length > 7 ? commit.Hash.Substring(0, 7) : commit.Hash;
                        ImGui.PushID($"commit_{commit.Hash}");
                        if (ImGui.Selectable(shortHash, selectedCommitIndex == commitIndex, ImGuiSelectableFlags.SpanAllColumns))
                        {
                            selectedCommitIndex = commitIndex;
                            UpdateSelectedCommitDetails(commit);
                        }

                        if (ImGui.BeginPopupContextItem("##CommitContextMenu"))
                        {
                            if (ImGui.MenuItem("Create branch from this commit"))
                            {
                                contextMenuHelper.ShowCreateBranchDialog(commit.Hash);
                            }

                            if (ImGui.MenuItem("Cherry-pick"))
                            {
                                contextMenuHelper.PerformCherryPickPublic(commit.Hash);
                            }

                            ImGui.EndPopup();
                        }

                        ImGui.PopID();

                        // Author column
                        ImGui.TableSetColumnIndex(1);
                        ImGui.TextUnformatted(commit.Author);

                        // Date column
                        ImGui.TableSetColumnIndex(2);
                        ImGui.TextUnformatted(commit.Date.ToString("yyyy-MM-dd HH:mm") + "łąść");

                        // Message column
                        ImGui.TableSetColumnIndex(3);
                        string message = commit.Message.Length > 50 ? commit.Message.Substring(0, 50) + "..." : commit.Message;
                        ImGui.TextUnformatted(message);

                        commitIndex++;
                    }

                    ImGui.EndTable();
                }
                ImGui.EndChild();
            }

            contextMenuHelper.RenderCreateBranchDialog();

            ImGui.End();
        }

        private void UpdateSelectedCommitDetails(GitCommit commit)
        {
            selectedCommitDetails = $"Hash: {commit.Hash}\n" +
                                   $"Author: {commit.Author}\n" +
                                   $"Date: {commit.Date:yyyy-MM-dd HH:mm:ss}\n" +
                                   $"Parent: {(string.IsNullOrEmpty(commit.ParentHash) ? "None" : commit.ParentHash)}\n" +
                                   $"Message: {commit.Message}";
        }
    }
}
