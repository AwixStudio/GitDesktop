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

        public ChangesView(RepositoryManager repositoryManager)
        {
            this.repositoryManager = repositoryManager;
        }

        public void Render()
        {
            ImGui.Begin("Changes");

            Repository? currentRepository = repositoryManager.CurrentRepository;
            if (currentRepository == null)
            {
                ImGui.TextDisabled("Nie wybrano repozytorium");
                ImGui.End();
                return;
            }

            var changes = currentRepository.Changes;

            if (changes.Count == 0)
            {
                ImGui.TextDisabled("Brak zmian");
                ImGui.End();
                return;
            }

            ImGui.Text($"Zmian: {changes.Count}");
            ImGui.Separator();

            if (ImGui.BeginTable("###ChangesList", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
            {
                ImGui.TableSetupColumn("Zaznacz", ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("Plik", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();

                foreach (var change in changes)
                {
                    ImGui.TableNextRow();

                    // Checkbox column
                    ImGui.TableSetColumnIndex(0);
                    bool marked = change.MarkedForCommit;
                    if (ImGui.Checkbox($"##checkbox_{change.Path}", ref marked))
                    {
                        change.MarkedForCommit = marked;
                    }

                    // File path column
                    ImGui.TableSetColumnIndex(1);

                    // Determine color based on state
                    uint color = GetColorForState(change.IndexState, change.WorkingTreeState);
                    ImGui.PushStyleColor(ImGuiCol.Text, color);

                    ImGui.Text(change.Path);
                    ImGui.PopStyleColor();

                    // Show state tooltip
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text($"Index: {change.IndexState}");
                        ImGui.Text($"Working Tree: {change.WorkingTreeState}");
                        ImGui.EndTooltip();
                    }
                }

                ImGui.EndTable();
            }

            ImGui.End();
        }

        private uint GetColorForState(GitFileState indexState, GitFileState workingTreeState)
        {
            // Yellow for modified
            if (workingTreeState == GitFileState.Modified)
                return ImGui.GetColorU32(System.Numerics.Vector4.One with { X = 1.0f, Y = 1.0f, Z = 0.0f, W = 1.0f });

            // Green for added
            if (indexState == GitFileState.Added || workingTreeState == GitFileState.Added)
                return ImGui.GetColorU32(System.Numerics.Vector4.One with { X = 0.0f, Y = 1.0f, Z = 0.0f, W = 1.0f });

            // Red for deleted
            if (indexState == GitFileState.Deleted || workingTreeState == GitFileState.Deleted)
                return ImGui.GetColorU32(System.Numerics.Vector4.One with { X = 1.0f, Y = 0.0f, Z = 0.0f, W = 1.0f });

            // Cyan for untracked
            if (workingTreeState == GitFileState.Untracked)
                return ImGui.GetColorU32(System.Numerics.Vector4.One with { X = 0.0f, Y = 1.0f, Z = 1.0f, W = 1.0f });

            // Magenta for conflicted
            if (workingTreeState == GitFileState.Conflicted)
                return ImGui.GetColorU32(System.Numerics.Vector4.One with { X = 1.0f, Y = 0.0f, Z = 1.0f, W = 1.0f });

            // Default white
            return ImGui.GetColorU32(System.Numerics.Vector4.One);
        }
    }
}
