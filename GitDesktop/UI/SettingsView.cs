using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitDesktop.UI
{
    internal class SettingsView : IRender
    {
        private bool isOpen;

        public SettingsView()
        {
            isOpen = true;
            ViewManager.AddNewView?.Invoke(this);
        }

        public void Render()
        {
            ImGui.Begin("Settings", ref isOpen);

            ImGui.End();

            if (!isOpen)
            {
                Close();
            }
        }

        private void Close()
        {
            isOpen = false;
            ViewManager.RemoveView?.Invoke(this);
        }
    }
}
