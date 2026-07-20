using GitDesktop.Git;
using ImGuiNET;
using System.Numerics;

namespace GitDesktop.UI
{
    internal class MainView : IRender
    {
        private readonly RepositoryManager repositoryManager;
        private readonly AddExistingRepositoryDialog addExistingRepositoryDialog;

        private readonly MenuItem repositoryMenu_addExisting;
        private readonly MenuItem repositoryMenu_clone;
        private readonly MenuItem repositoryMenu_createNew;
        private readonly Menu repositoryMenu;

        private readonly MenuItem branchMenu_createNew;
        private readonly Menu branchMenu;

        private readonly MenuItem settingsMenu_credits;
        private readonly MenuItem settingsMenu_about;
        private readonly Menu settingsMenu;

        public MainView(RepositoryManager _repositoryManager)
        {
            repositoryManager = _repositoryManager;

            addExistingRepositoryDialog = new AddExistingRepositoryDialog(repositoryManager);

            repositoryMenu_addExisting = new("Add existing repository", () => addExistingRepositoryDialog.Open());
            repositoryMenu_clone = new("Clone repository", () => { });
            repositoryMenu_createNew = new("Create new repository", () => { });

            repositoryMenu = new("Repository",
            [
                repositoryMenu_addExisting,
                repositoryMenu_clone,
                repositoryMenu_createNew
            ]);

            branchMenu_createNew = new("Create new branch", () => { });

            branchMenu = new("Branch",
            [
                branchMenu_createNew
            ]);

            settingsMenu_credits = new("Credits", () => { });
            settingsMenu_about = new("About", () => { });
            settingsMenu = new("Settings",
            [
                settingsMenu_credits,
                settingsMenu_about
            ]);
        }

        public void Render()
        {
            ImGui.BeginMainMenuBar();
            repositoryMenu.Render();
            branchMenu.Render();
            settingsMenu.Render();
            ImGui.EndMainMenuBar();

            MainBar();
        }

        private void MainBar()
        {
            ImGuiViewportPtr viewport = ImGui.GetMainViewport();

            ImGui.SetNextWindowPos(viewport.WorkPos);
            ImGui.SetNextWindowSize(viewport.WorkSize);
            ImGui.SetNextWindowViewport(viewport.ID);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);

            ImGuiWindowFlags flags =
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoBringToFrontOnFocus |
                ImGuiWindowFlags.NoNavFocus;

            ImGui.Begin("MainWindow", flags);

            ImGui.PopStyleVar(2);

            Repository? selectedRepository = repositoryManager.CurrentRepository;
            int selectedRepositoryIndex = Array.IndexOf(repositoryManager.GetRepositoryPaths(), selectedRepository?.Path);
            string[] repositoriesNames = repositoryManager.GetRepositoryNames();

            const float repositoryWidth = 300;
            const float branchWidth = 220;
            const float updateButtonWidth = 170;
            const float prButtonWidth = 180;

            // Repository
            ImGui.BeginGroup();
            ImGui.TextUnformatted("Repository");
            ImGui.SetNextItemWidth(repositoryWidth);
            int previousSelectedRepository = selectedRepositoryIndex;
            ImGui.Combo("##Repository", ref selectedRepositoryIndex, repositoriesNames, repositoriesNames.Length);
            ImGui.EndGroup();

            if(selectedRepositoryIndex != previousSelectedRepository)
            {
                Repository newSelectedRepository = repositoryManager.GetRepository(selectedRepositoryIndex);
                repositoryManager.SetCurrentRepository(newSelectedRepository);
                selectedRepository = newSelectedRepository;
            }

            // Branch
            GitBranch? selectedBranch = selectedRepository?.CurrentBranch;
            int selectedBranchIndex = Array.IndexOf(selectedRepository?.Branches.Select(b => b.Name).ToArray() ?? [], selectedBranch?.Name);
            string[] branchesNames = selectedRepository?.Branches.Select(b => b.Name).ToArray() ?? [];

            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.TextUnformatted("Branch");
            ImGui.SetNextItemWidth(branchWidth);
            int previousSelectedBranch = selectedBranchIndex;
            ImGui.Combo("##Branch", ref selectedBranchIndex, branchesNames, branchesNames.Length);
            ImGui.EndGroup();

            if(previousSelectedBranch != selectedBranchIndex)
            {
                GitBranch newSelectedBranch = selectedRepository?.Branches.ElementAt(selectedBranchIndex) ?? throw new InvalidOperationException("Selected branch not found");
                selectedRepository.ChangeBranch(newSelectedBranch);
                selectedBranch = newSelectedBranch;
            }

            ImGui.SameLine(0, 20);

            // Update button
            ImGui.BeginGroup();
            ImGui.Dummy(new Vector2(0, ImGui.GetTextLineHeightWithSpacing())); // miejsce na etykietę
            if (ImGui.Button("Update from main", new Vector2(updateButtonWidth, 0)))
            {
                if (selectedRepository != null)
                {
                    // Create and show progress popup
                    UpdateProgressPopup progressPopup = new();

                    // Start update on a separate thread
                    Task.Run(() =>
                    {
                        try
                        {
                            selectedRepository.UpdateFromMain((status, progress) =>
                            {
                                progressPopup.UpdateStatus(status, progress);
                            });
                            progressPopup.Complete();
                        }
                        catch (Exception ex)
                        {
                            progressPopup.Error(ex.Message);
                        }
                    });
                }
            }
            ImGui.EndGroup();

            ImGui.SameLine();

            // Pull Request button
            ImGui.BeginGroup();
            ImGui.Dummy(new Vector2(0, ImGui.GetTextLineHeightWithSpacing())); // miejsce na etykietę
            if (ImGui.Button("Create Pull Request", new Vector2(prButtonWidth, 0)))
            {
            }
            ImGui.EndGroup();

            ImGui.Separator();

            ImGui.End();
        }
    }
}
