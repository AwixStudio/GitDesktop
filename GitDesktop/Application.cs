using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitDesktop
{
    internal class Application
    {
        private readonly IWindow _window;
        private GL _gl = null!;
        private ImGuiController _imGui = null!;
        private IInputContext _input = null!;

        public Application()
        {
            var options = WindowOptions.Default;

            options.Title = "GitDesktop";
            options.Size = new Vector2D<int>(1600, 900);

            _window = Window.Create(options);

            _window.Load += OnLoad;
            _window.Update += OnUpdate;
            _window.Render += OnRender;
            _window.Closing += OnClosing;
        }

        public void Run()
        {
            _window.Run();
        }

        private void OnLoad()
        {
            _gl = _window.CreateOpenGL();
            _gl.ClearColor(0.1f, 0.15f, 0.3f, 1.0f);

            _gl.Enable(GLEnum.Blend);

            _gl.BlendEquation(GLEnum.FuncAdd);

            _gl.BlendFunc(
                BlendingFactor.SrcAlpha,
                BlendingFactor.OneMinusSrcAlpha);

            _gl.Disable(GLEnum.CullFace);
            _gl.Disable(GLEnum.DepthTest);
            _gl.Disable(GLEnum.StencilTest);

            _input = _window.CreateInput();
            _imGui = new ImGuiController(_window, _gl, _input);
        }

        private void OnUpdate(double deltaTime)
        {

        }

        private void OnRender(double deltaTime)
        {
            _gl.Viewport(
                0,
                0,
                (uint)_window.FramebufferSize.X,
                (uint)_window.FramebufferSize.Y);
            _gl.Clear(ClearBufferMask.ColorBufferBit);

            _imGui.Update(deltaTime);
            DrawUI();
            _imGui.Render();
        }

        private void DrawUI()
        {
            ImGui.Begin("Hello");
            ImGui.Text("Hello GitDesktop!");
            if (ImGui.Button("Click me"))
            {
                Console.WriteLine("Button clicked!");
            }
            ImGui.End();
        }

        private void OnClosing()
        {
            _imGui.Dispose();
        }
    }
}
