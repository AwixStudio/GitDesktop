using ImGuiNET;

namespace GitDesktop.UI
{
    internal class MenuItem(string textKey, Action onClick) : IRender
    {
        private string textKey = textKey;
        private Action onClick = onClick;

        public void Render()
        {            
            if (onClick == null)
            {
                ImGui.Text(textKey);
                return;
            }

            if (ImGui.MenuItem(textKey))
            {
                onClick();
            }            
        }
    }
}