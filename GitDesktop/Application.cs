using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.Text;
using ImGuiNET;

namespace GitDesktop
{
    internal class Application
    {
        private readonly IWindow _window;
        private GL _gl = null!;
        private ImGuiController _imGui = null!;

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
            
            _imGui = new ImGuiController(_window, _gl);
        }

        private void OnUpdate(double deltaTime)
        {

        }

        private void OnRender(double deltaTime)
        {
            _gl.Clear(ClearBufferMask.ColorBufferBit);

            _imGui.Update(deltaTime);
            DrawUI();
            _imGui.Render();
        }

        private void DrawUI()
        {
            ImGui.Begin("Hello");
            ImGui.Text("Hello GitDesktop!");
            ImGui.Button("Click me");
            ImGui.End();
        }

        private void OnClosing()
        {

        }
    }
}
