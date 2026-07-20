using ImGuiNET;

namespace GitDesktop.UI
{
    internal class Menu(string textKey, List<IRender> children) : IRender
    {
        private string textKey = textKey;
        private List<IRender> children = children;

        public void Render()
        {
            if (ImGui.BeginMenu(textKey))
            {
                foreach (var item in children)
                {
                    item.Render();
                }

                ImGui.EndMenu();
            }
        }

        public void ClearChildren() => children.Clear();
        public void AddChild(IRender child) => children.Add(child);
    }
}