using GitDesktop.Git;
using GitDesktop.UI;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace GitDesktop
{
    internal class Application
    {
        private readonly IWindow window;
        private GL gl = null!;
        private ImGuiController imGui = null!;
        private IInputContext input = null!;

        private RepositoryManager repositoryManager;

        private List<IRender> views;

        // Auto-pull on focus mechanism
        private DateTime lastAutoPullTime = DateTime.MinValue;
        private const int AUTO_PULL_DEBOUNCE_MS = 5000; // Don't pull more often than every 5 seconds
        private UpdateProgressPopup? autoPullPopup;

        public Application()
        {
            var options = WindowOptions.Default;

            options.Title = "GitDesktop";
            options.Size = new Vector2D<int>(1600, 900);

            window = Window.Create(options);

            window.Load += OnLoad;
            window.Update += OnUpdate;
            window.Render += OnRender;
            window.Closing += OnClosing;
            window.FocusChanged += OnFocusChanged;

            repositoryManager = new RepositoryManager();

            Logger.Initialize(ViewManager.CreateLogPopupWindow);
            views = new List<IRender>();

            // Configure ViewManager callbacks
            ViewManager.AddNewView = (view) => views.Add(view);
            ViewManager.RemoveView = (view) => views.Remove(view);

            views.Add(new MainView(repositoryManager));
            views.Add(new ChangesView(repositoryManager));
            views.Add(new RightPanelTabbedView(repositoryManager));
        }

        public void Run()
        {
            window.Run();
        }

        private void OnLoad()
        {
            gl = window.CreateOpenGL();
            gl.ClearColor(0.1f, 0.15f, 0.3f, 1.0f);

            gl.Enable(GLEnum.Blend); 

            gl.BlendEquation(GLEnum.FuncAdd);

            gl.BlendFunc(
                BlendingFactor.SrcAlpha,
                BlendingFactor.OneMinusSrcAlpha);

            gl.Disable(GLEnum.CullFace);
            gl.Disable(GLEnum.DepthTest);
            gl.Disable(GLEnum.StencilTest);

            input = window.CreateInput();
            imGui = new ImGuiController(window, gl, input);
        }

        private void OnUpdate(double deltaTime)
        {
            imGui.PrepareForRender(deltaTime);
            ProcessUI();
        }

        private void OnRender(double deltaTime)
        {
            gl.Viewport(
                0,
                0,
                (uint)window.FramebufferSize.X,
                (uint)window.FramebufferSize.Y);
            gl.Clear(ClearBufferMask.ColorBufferBit);

            imGui.Render();
        }

        private void ProcessUI()
        {
            // Don't render UI if window is minimized (size is 0)
            if (window.FramebufferSize.X == 0 || window.FramebufferSize.Y == 0)
                return;

            // iterate over a copy of the views list to avoid modification during iteration
            var viewsCopy = new List<IRender>(views);
            foreach (var view in viewsCopy)
            {
                view.Render();
            }
        }

        private void OnFocusChanged(bool focused)
        {
            if (focused)
            {
                repositoryManager.RefreshChanges();

                // Auto-pull with debounce to prevent frequent pulls
                var now = DateTime.UtcNow;
                if ((now - lastAutoPullTime).TotalMilliseconds > AUTO_PULL_DEBOUNCE_MS 
                    && repositoryManager.CurrentRepository != null
                    && autoPullPopup == null)
                {
                    lastAutoPullTime = now;
                    PerformAutoPull();
                }
            }
        }

        private async void PerformAutoPull()
        {
            try
            {
                var repository = repositoryManager.CurrentRepository;
                if (repository == null)
                    return;

                // First check if there are any updates without showing popup
                bool hasUpdates = await GitService.HasRemoteUpdates(repository.Path);

                // Only show popup if there are actually updates to pull
                if (!hasUpdates)
                    return;

                // Show popup only when we know there are updates
                autoPullPopup = new UpdateProgressPopup("Auto-Pull Progress", () =>
                {
                    autoPullPopup = null;
                });

                // Perform the auto-pull with progress updates
                bool pullSuccess = await repository.AutoPullFromRemote((status, progress) =>
                {
                    if (autoPullPopup != null)
                    {
                        autoPullPopup.UpdateStatus(status, progress);
                    }
                });

                if (pullSuccess && autoPullPopup != null)
                {
                    autoPullPopup.Complete();
                }
            }
            catch (Exception ex)
            {
                if (autoPullPopup != null)
                {
                    autoPullPopup.Error($"Auto-pull error: {ex.Message}");
                }
            }
        }

        private void OnClosing()
        {
            imGui.Dispose();
        }
    }
}
