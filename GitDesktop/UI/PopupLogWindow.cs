using ImGuiNET;

namespace GitDesktop.UI
{
    internal class PopupLogWindow : IRender
    {
        private readonly string message;
        private bool isPopupOpened = false;

        internal PopupLogWindow(string message)
        {
            this.message = message;

            ViewManager.AddNewView?.Invoke(this);
        }

        public void Render()
        {
            if (!isPopupOpened)
            {
                ImGui.OpenPopup("Warning");
                isPopupOpened = true;
            }

            ImGui.SetNextWindowSizeConstraints(new System.Numerics.Vector2(400, 150), new System.Numerics.Vector2(800, 600));

            if (ImGui.BeginPopupModal("Warning", ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.TextWrapped(message);
                ImGui.Spacing();

                if (ImGui.Button("Ok"))
                {
                    Close();
                }

                ImGui.EndPopup();
            }
        }

        public void Close()
        {
            ViewManager.RemoveView?.Invoke(this);
        }
    }
}
