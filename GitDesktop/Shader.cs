using Silk.NET.OpenGL;

namespace GitDesktop
{
    public sealed class Shader : IDisposable
    {
        private readonly GL _gl;

        public uint Handle { get; }
        public int ProjectionMatrixLocation { get; }
        public int TextureLocation { get; }

        public Shader(GL gl, string vertexSource, string fragmentSource)
        {
            _gl = gl;

            // Vertex Shader
            uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
            _gl.ShaderSource(vertexShader, vertexSource);
            _gl.CompileShader(vertexShader);

            _gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int success);
            if (success == 0)
            {
                throw new Exception(_gl.GetShaderInfoLog(vertexShader));
            }

            // Fragment Shader
            uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
            _gl.ShaderSource(fragmentShader, fragmentSource);
            _gl.CompileShader(fragmentShader);

            _gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out success);
            if (success == 0)
            {
                throw new Exception(_gl.GetShaderInfoLog(fragmentShader));
            }

            // Program
            Handle = _gl.CreateProgram();

            _gl.AttachShader(Handle, vertexShader);
            _gl.AttachShader(Handle, fragmentShader);

            _gl.LinkProgram(Handle);

            ProjectionMatrixLocation = _gl.GetUniformLocation(Handle, "projection_matrix");
            TextureLocation = _gl.GetUniformLocation(Handle, "Texture");

            _gl.GetProgram(Handle, ProgramPropertyARB.LinkStatus, out success);
            if (success == 0)
            {
                throw new Exception(_gl.GetProgramInfoLog(Handle));
            }

            // Shadery nie są już potrzebne po linkowaniu
            _gl.DetachShader(Handle, vertexShader);
            _gl.DetachShader(Handle, fragmentShader);

            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);
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