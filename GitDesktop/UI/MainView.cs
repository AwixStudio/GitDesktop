using GitDesktop.Git;
using ImGuiNET;
using System.Numerics;

namespace GitDesktop.UI
{
    internal class MainView : IRender
    {
        private readonly RepositoryManager repositoryManager;
        private readonly AddExistingRepositoryDialog addExistingRepositoryDialog;
        private readonly CloneRepositoryDialog cloneRepositoryDialog;
        private readonly CreateRepositoryDialog createRepositoryDialog;
        private readonly CreateBranchDialogHelper createBranchDialogHelper;
        private readonly SearchableComboHelper branchComboHelper;

        private readonly MenuItem repositoryMenu_addExisting;
        private readonly MenuItem repositoryMenu_clone;
        private readonly MenuItem repositoryMenu_createNew;
        private readonly MenuItem repositoryMenu_gitCmd;
        private readonly MenuItem repositoryMenu_openInExplorer;
        private readonly MenuItem repositoryMenu_removeFromList;
        private readonly Menu repositoryMenu;

        private readonly MenuItem branchMenu_createNew;
        private readonly MenuItem branchMenu_resetHard;
        private readonly Menu branchMenu;

        private readonly MenuItem settingsMenu_credits;
        private readonly MenuItem settingsMenu_about;
        private readonly Menu settingsMenu;

        public MainView(RepositoryManager _repositoryManager)
        {
            repositoryManager = _repositoryManager;

            addExistingRepositoryDialog = new AddExistingRepositoryDialog(repositoryManager);
            cloneRepositoryDialog = new CloneRepositoryDialog(repositoryManager);
            createRepositoryDialog = new CreateRepositoryDialog(repositoryManager);
            createBranchDialogHelper = new CreateBranchDialogHelper(repositoryManager);
            branchComboHelper = new SearchableComboHelper();

            repositoryMenu_addExisting = new("Add existing repository", () => addExistingRepositoryDialog.Open());
            repositoryMenu_clone = new("Clone repository", () => cloneRepositoryDialog.Open());
            repositoryMenu_createNew = new("Create new repository", () => createRepositoryDialog.Open());
            repositoryMenu_gitCmd = new("Open in cmd", () => repositoryManager.CurrentRepository?.OpenInGitCmd());
            repositoryMenu_openInExplorer = new("Open in explorer", () => repositoryManager.CurrentRepository?.OpenInExplorer());
            repositoryMenu_removeFromList = new("Remove from list", () => RemoveCurrentRepositoryFromList());

            repositoryMenu = new("Repository",
            [
                repositoryMenu_addExisting,
                repositoryMenu_clone,
                repositoryMenu_createNew,
                repositoryMenu_gitCmd,
                repositoryMenu_openInExplorer,
                repositoryMenu_removeFromList
            ]);

            branchMenu_createNew = new("Create new branch", () => createBranchDialogHelper.Open());
            branchMenu_resetHard = new("Hard reset", () => repositoryManager.CurrentRepository?.HardReset());

            branchMenu = new("Branch",
            [
                branchMenu_createNew,
                branchMenu_resetHard
            ]);

            settingsMenu_credits = new("Options", () => new SettingsView());
            settingsMenu_about = new("About", () => new AboutView());
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

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);

            Repository? selectedRepository = repositoryManager.CurrentRepository;
            int selectedRepositoryIndex = Array.IndexOf(repositoryManager.GetRepositoryPaths(), selectedRepository?.Path);
            string[] repositoriesNames = repositoryManager.GetRepositoryNames();

            const float repositoryWidth = 300;
            const float branchWidth = 220;
            const float updateButtonWidth = 170;
            const float prButtonWidth = 180;
            const float explorerButtonWidth = 160;

            // Repository
            ImGui.BeginGroup();
            ImGui.TextUnformatted("Repository");
            ImGui.SetNextItemWidth(repositoryWidth);
            int previousSelectedRepository = selectedRepositoryIndex;
            ImGui.Combo("##Repository", ref selectedRepositoryIndex, repositoriesNames, repositoriesNames.Length);
            ImGui.EndGroup();


            if (selectedRepositoryIndex != previousSelectedRepository)
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
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 3);

            ImGui.BeginGroup();
            ImGui.TextUnformatted("Branch");
            ImGui.SetNextItemWidth(branchWidth);
            int previousSelectedBranch = selectedBranchIndex;

            // Use searchable combo instead of standard combo
            if (branchComboHelper.BeginCombo("##Branch", selectedBranch?.Name ?? "No branch", "##BranchCombo"))
            {
                branchComboHelper.SearchInput();
                ImGui.Separator();

                int newSelectedIndex = branchComboHelper.SelectableList(branchesNames, selectedBranchIndex);
                if (newSelectedIndex >= 0)
                {
                    selectedBranchIndex = newSelectedIndex;
                    branchComboHelper.EndCombo();
                    branchComboHelper.Reset();
                }
                else
                {
                    branchComboHelper.EndCombo();
                }
            }

            ImGui.EndGroup();

            if(previousSelectedBranch != selectedBranchIndex)
            {
                GitBranch newSelectedBranch = selectedRepository?.Branches.ElementAt(selectedBranchIndex) ?? throw new InvalidOperationException("Selected branch not found");
                selectedRepository.ChangeBranch(newSelectedBranch);
                selectedBranch = newSelectedBranch;
            }

            ImGui.SameLine(0, 20);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 4);

            if (repositoryManager.CurrentRepository != null)
            {
                // Update button
                ImGui.BeginGroup();
                ImGui.Dummy(new Vector2(0, ImGui.GetTextLineHeightWithSpacing())); // miejsce na etykietę
                if (ImGui.Button("Update from " + repositoryManager.CurrentRepository?.DefaultBranchName, new Vector2(updateButtonWidth, 0)))
                {
                    if (selectedRepository != null)
                    {
                        // Create and show progress popup
                        UpdateProgressPopup progressPopup = new();

                        // Start update on a separate thread
                        Task.Run(async () =>
                        {
                            try
                            {
                                var result = await selectedRepository.UpdateFromMain((status, progress) =>
                                {
                                    progressPopup.UpdateStatus(status, progress);
                                });

                                if (result.HasConflicts)
                                {
                                    // Show conflict resolution dialog
                                    progressPopup.UpdateStatus("Showing conflict resolution dialog...", 95);

                                    // Create callback for after conflicts are resolved
                                    Action onConflictsResolved = () =>
                                    {
                                        // Refresh the main view to show updated file list
                                        // The repository has already refreshed its internal state in the dialog
                                    };

                                    ConflictResolutionDialog conflictDialog = new(selectedRepository, result.ConflictedFiles, onConflictsResolved);

                                    // Wait for dialog to complete
                                    while (!conflictDialog.IsResolved && !conflictDialog.WasCancelled)
                                    {
                                        await Task.Delay(100);
                                    }

                                    if (conflictDialog.WasCancelled)
                                    {
                                        progressPopup.Error("Merge cancelled by user.");
                                    }
                                    else
                                    {
                                        progressPopup.Complete();
                                    }
                                }
                                else
                                {
                                    progressPopup.Complete();
                                }
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
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 4);

                // Pull Request button
                ImGui.BeginGroup();
                ImGui.Dummy(new Vector2(0, ImGui.GetTextLineHeightWithSpacing())); // miejsce na etykietę
                if (ImGui.Button("Create Pull Request", new Vector2(prButtonWidth, 0)))
                {
                    repositoryManager.CurrentRepository?.CreatePullRequest();
                }
                ImGui.EndGroup();
            }

            ImGui.End();
        }

        private void RemoveCurrentRepositoryFromList()
        {
            if (repositoryManager.CurrentRepository != null)
            {
                repositoryManager.RemoveRepository(repositoryManager.CurrentRepository.Path);
            }
        }
    }
}
