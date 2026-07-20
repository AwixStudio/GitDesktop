using GitDesktop.Repository;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitDesktop.UI
{
    internal class MainView(RepositoryManager repositoryManager) : IRender
    {
        private readonly RepositoryManager repositoryManager = repositoryManager;

        public void Render()
        {
            ImGui.BeginMainMenuBar();



            ImGui.EndMainMenuBar();
        }
    }
}
