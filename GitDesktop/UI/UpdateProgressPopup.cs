using ImGuiNET;
using System.Numerics;

namespace GitDesktop.UI
{
    internal class UpdateProgressPopup : IRender
    {
        private bool isPopupOpened = false;
        private string currentStatus = "Starting update...";
        private float progress = 0f;
        private bool isComplete = false;
        private bool hasError = false;
        private string? errorMessage;
        private string titleText = "Update Progress";

        public UpdateProgressPopup(string? customTitle = null)
        {
            if (customTitle != null)
                titleText = customTitle;
            ViewManager.AddNewView?.Invoke(this);
        }

        public void UpdateStatus(string status, float progressPercent = -1)
        {
            currentStatus = status;
            if (progressPercent >= 0)
                progress = Math.Min(progressPercent, 100f);
        }

        public void Complete()
        {
            isComplete = true;
            progress = 100f;
            currentStatus = "Update completed successfully!";
        }

        public void Error(string message)
        {
            hasError = true;
            errorMessage = message;
            currentStatus = "Update failed!";
            progress = 0f;
        }

        public void Render()
        {
            if (!isPopupOpened)
            {
                ImGui.OpenPopup(titleText);
                isPopupOpened = true;
            }

            ImGui.SetNextWindowSizeConstraints(new Vector2(500, 200), new Vector2(700, 350));

            if (ImGui.BeginPopupModal(titleText, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("Updating from main branch...");
                ImGui.Spacing();

                ImGui.TextWrapped(currentStatus);
                ImGui.Spacing();

                ImGui.ProgressBar(progress / 100f, new Vector2(-1, 25), $"{progress:F0}%");
                ImGui.Spacing();

                if (hasError && errorMessage != null)
                {
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "Error:");
                    ImGui.TextWrapped(errorMessage);
                    ImGui.Spacing();
                }

                ImGui.Separator();

                if (isComplete)
                {
                    if (ImGui.Button("Close", new Vector2(150, 0)))
                    {
                        Close();
                    }
                }
                else if (hasError)
                {
                    if (ImGui.Button("Close", new Vector2(150, 0)))
                    {
                        Close();
                    }
                }
                else
                {
                    ImGui.BeginDisabled();
                    ImGui.Button("Updating...", new Vector2(150, 0));
                    ImGui.EndDisabled();
                }

                ImGui.EndPopup();
            }
        }

        private void Close()
        {
            ViewManager.RemoveView?.Invoke(this);
        }
    }
}
