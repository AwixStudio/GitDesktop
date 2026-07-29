using GitDesktop.Git;
using ImGuiNET;
using System.Numerics;

namespace GitDesktop.UI
{
    internal class RightPanelTabbedView : IRender
    {
        private readonly RepositoryManager repositoryManager;
        private readonly CommitHistoryView commitHistoryView;
        private readonly DiffView diffView;
        private int activeTab = 0; // 0 = History, 1 = Diff

        public RightPanelTabbedView(RepositoryManager repositoryManager)
        {
            this.repositoryManager = repositoryManager;
            this.commitHistoryView = new CommitHistoryView(repositoryManager);
            this.diffView = new DiffView(repositoryManager);
        }

        public void Render()
        {
            ImGuiWindowFlags flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoCollapse;
            ImGui.SetNextWindowDockID(ImGui.GetID("RightPanel"), ImGuiCond.Always);

            float yOffset = ImGui.GetFrameHeightWithSpacing() + 55f;
            float windowHeight = ImGui.GetIO().DisplaySize.Y - yOffset - 5f;
            float windowWidth = ImGui.GetIO().DisplaySize.X - 560f - 10f;

            ImGui.SetNextWindowPos(new Vector2(560, yOffset), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight), ImGuiCond.Always);

            ImGui.Begin("Repository", flags);

            // Begin tab bar
            if (ImGui.BeginTabBar("##RightPanelTabs", ImGuiTabBarFlags.None))
            {
                // History tab
                bool historyOpen = true;
                if (ImGui.BeginTabItem("History", ref historyOpen, ImGuiTabItemFlags.None))
                {
                    activeTab = 0;
                    commitHistoryView.RenderContent();
                    ImGui.EndTabItem();
                }

                // Diff tab
                bool diffOpen = true;
                if (ImGui.BeginTabItem("Diff description", ref diffOpen, ImGuiTabItemFlags.None))
                {
                    activeTab = 1;
                    diffView.RenderContent();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.End();
        }
    }
}
