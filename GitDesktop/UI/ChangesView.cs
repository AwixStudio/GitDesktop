using GitDesktop.Git;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitDesktop.UI
{
    internal class ChangesView : IRender
    {
        private readonly RepositoryManager repositoryManager;
        private string commitMessage = string.Empty;
        private int lastSelectedChangeIndex = -1;
        private string lastHoveredFilePath = string.Empty;

        public ChangesView(RepositoryManager repositoryManager)
        {
            this.repositoryManager = repositoryManager;
        }

        public void Render()
        {
            ImGuiWindowFlags flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoCollapse;
            ImGui.SetNextWindowDockID(ImGui.GetID("LeftPanel"), ImGuiCond.Always);

            float yOffset = ImGui.GetFrameHeightWithSpacing() + 55f;
            float windowHeight = ImGui.GetIO().DisplaySize.Y - yOffset - 5f;

            ImGui.SetNextWindowPos(new System.Numerics.Vector2(5, yOffset), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(550, windowHeight), ImGuiCond.Always);

            ImGui.Begin("Changes", flags);

            Repository? currentRepository = repositoryManager.CurrentRepository;
            if (currentRepository == null)
            {
                ImGui.TextDisabled("No repository selected");
                ImGui.End();
                return;
            }

            var changes = currentRepository.Changes;

            if (changes.Count == 0)
            {
                ImGui.TextDisabled("No changes to commit");
                ImGui.End();
                return;
            }

            ImGui.Text($"Changes: {changes.Count}");
            ImGui.Separator();

            // Container for the changes list with fixed height
            if (ImGui.BeginChild("ChangesListContainer", new System.Numerics.Vector2(-1, -110f)))
            {
                if (ImGui.BeginTable("###ChangesList", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
                {
                    ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 20);
                    ImGui.TableSetupColumn("File", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableHeadersRow();

                    int changeIndex = 0;
                    foreach (var change in changes)
                    {
                        ImGui.TableNextRow();

                        // Checkbox column
                        ImGui.TableSetColumnIndex(0);
                        bool marked = change.MarkedForCommit;
                        if (ImGui.Checkbox($"##checkbox_{change.Path}", ref marked))
                        {
                            // Handle Shift+Click for range selection
                            if (ImGui.IsKeyDown(ImGuiKey.LeftShift) && lastSelectedChangeIndex >= 0)
                            {
                                int startIndex = Math.Min(lastSelectedChangeIndex, changeIndex);
                                int endIndex = Math.Max(lastSelectedChangeIndex, changeIndex);

                                for (int i = startIndex; i <= endIndex; i++)
                                {
                                    changes[i].MarkedForCommit = marked;
                                }
                            }
                            else
                            {
                                change.MarkedForCommit = marked;
                            }

                            lastSelectedChangeIndex = changeIndex;
                        }

                        // File path column
                        ImGui.TableSetColumnIndex(1);

                        // Determine color based on state
                        uint color = GetColorForState(change.IndexState, change.WorkingTreeState);
                        ImGui.PushStyleColor(ImGuiCol.Text, color);

                        ImGui.Text(change.Path);
                        ImGui.PopStyleColor();

                        if (ImGui.IsItemHovered())
                        {
                            lastHoveredFilePath = change.Path;

                            // Lazy load diff on hover
                            if (!change.IsDiffLoaded)
                            {
                                try
                                {
                                    string diff = GitService.GetFileDiff(currentRepository.Path, change.Path);
                                    change.SetCachedDiff(diff);
                                }
                                catch
                                {
                                    change.SetCachedDiff("Error loading diff");
                                }
                            }

                            ImGui.BeginTooltip();
                            ImGui.Text(change.GetCachedDiff());
                            ImGui.EndTooltip();
                        }

                        changeIndex++;
                    }

                    ImGui.EndTable();
                }
                ImGui.EndChild();
            }

            // Commit message area
            ImGui.TextUnformatted("Commit Message:");

            ImGui.InputTextMultiline(
                "##CommitMessage",
                ref commitMessage,
                1024,
                new System.Numerics.Vector2(-1, 60f),
                ImGuiInputTextFlags.None
            );

            // Commit button
            if (ImGui.Button("Commit", new System.Numerics.Vector2(-1, 0)))
            {
                if (!string.IsNullOrWhiteSpace(commitMessage))
                {
                    currentRepository?.CommitChanges(commitMessage);
                    commitMessage = string.Empty;
                }
            }

            ImGui.End();
        }

        private uint GetColorForState(GitFileState indexState, GitFileState workingTreeState)
        {
            // Yellow for modified
            if (workingTreeState == GitFileState.Modified)
                return ImGui.GetColorU32(System.Numerics.Vector4.One with { X = 1.0f, Y = 1.0f, Z = 0.0f, W = 1.0f });

            // Green for added
            if (indexState == GitFileState.Added || workingTreeState == GitFileState.Added || workingTreeState == GitFileState.Untracked)
                return ImGui.GetColorU32(System.Numerics.Vector4.One with { X = 0.0f, Y = 1.0f, Z = 0.0f, W = 1.0f });

            // Red for deleted
            if (indexState == GitFileState.Deleted || workingTreeState == GitFileState.Deleted)
                return ImGui.GetColorU32(System.Numerics.Vector4.One with { X = 1.0f, Y = 0.0f, Z = 0.0f, W = 1.0f });

            // Magenta for conflicted
            if (workingTreeState == GitFileState.Conflicted)
                return ImGui.GetColorU32(System.Numerics.Vector4.One with { X = 1.0f, Y = 0.0f, Z = 1.0f, W = 1.0f });

            // Default white
            return ImGui.GetColorU32(System.Numerics.Vector4.One);
        }
    }
}
