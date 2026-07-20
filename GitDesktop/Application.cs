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

            repositoryManager = new RepositoryManager();

            Logger.Initialize(ViewManager.CreateLogPopupWindow);
            views = new List<IRender>();

            // Configure ViewManager callbacks
            ViewManager.AddNewView = (view) => views.Add(view);
            ViewManager.RemoveView = (view) => views.Remove(view);

            views.Add(new MainView(repositoryManager));
            views.Add(new ChangesView(repositoryManager));
            views.Add(new CommitHistoryView(repositoryManager));
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
            // iterate over a copy of the views list to avoid modification during iteration
            var viewsCopy = new List<IRender>(views);
            foreach (var view in viewsCopy)
            {
                view.Render();
            }
        }

        private void OnClosing()
        {
            imGui.Dispose();
        }
    }
}
