using ImGuiNET;

namespace GitDesktop.UI
{
    internal class AboutView : IRender
    {
        private bool isOpen;

        public AboutView()
        {
            isOpen = true;
            ViewManager.AddNewView?.Invoke(this);
        }

        public void Render()
        {
            ImGui.Begin("About", ref isOpen);

            ImGui.Text("GitDesktop v1.0.0");
            ImGui.Spacing();
            ImGui.Text("Created by Dawid Plowiec");

            ImGui.End();

            if(!isOpen)
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
