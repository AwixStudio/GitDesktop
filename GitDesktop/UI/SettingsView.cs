using GitDesktop.Git;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitDesktop.UI
{
    internal class SettingsView : IRender
    {
        private bool isOpen;
        private string gitUserName;
        private string gitUserEmail;

        public SettingsView()
        {
            isOpen = true;
            // Load current git config on initialization
            gitUserName = GitService.GetGitUserName();
            gitUserEmail = GitService.GetGitUserEmail();
            ViewManager.AddNewView?.Invoke(this);
        }

        public void Render()
        {
            ImGui.Begin("Settings", ref isOpen, ImGuiWindowFlags.NoDocking);

            ImGui.Text("Git Configuration");
            ImGui.Separator();

            ImGui.InputText("User Name", ref gitUserName, 100);
            ImGui.InputText("User Email", ref gitUserEmail, 100);

            if (ImGui.Button("Save", new System.Numerics.Vector2(100, 0)))
            {
                GitService.SetGitUserName(gitUserName);
                GitService.SetGitUserEmail(gitUserEmail);
            }

            //string password = CredentialManager.RetrieveCredentials().password;
            //ImGui.InputText("Personal access token", ref password, 100);
            //if(password != null && password != CredentialManager.RetrieveCredentials().password)
            //{
            //    CredentialManager.StoreCredentials(CredentialManager.RetrieveCredentials().username, password);
            //}


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
