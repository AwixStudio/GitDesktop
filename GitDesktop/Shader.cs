using Silk.NET.OpenGL;

namespace GitDesktop
{
    public sealed class Shader : IDisposable
    {
        private readonly GL _gl;

        public uint Handle { get; }

        public Shader(GL gl, string vertexSource, string fragmentSource)
        {
            _gl = gl;

            // tutaj będzie kompilacja
        }

        public void Use()
        {
            _gl.UseProgram(Handle);
        }

        public void Dispose()
        {
            _gl.DeleteProgram(Handle);
        }
    }
}