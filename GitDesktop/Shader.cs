using Silk.NET.OpenGL;

namespace GitDesktop
{
    public sealed class Shader : IDisposable
    {
        private readonly GL gl;

        public uint Handle { get; }
        public int ProjectionMatrixLocation { get; }
        public int TextureLocation { get; }

        public Shader(GL _gl, string vertexSource, string fragmentSource)
        {
            gl = _gl;

            // Vertex Shader
            uint vertexShader = gl.CreateShader(ShaderType.VertexShader);
            gl.ShaderSource(vertexShader, vertexSource);
            gl.CompileShader(vertexShader);

            gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int success);
            if (success == 0)
            {
                throw new Exception(gl.GetShaderInfoLog(vertexShader));
            }

            // Fragment Shader
            uint fragmentShader = gl.CreateShader(ShaderType.FragmentShader);
            gl.ShaderSource(fragmentShader, fragmentSource);
            gl.CompileShader(fragmentShader);

            gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out success);
            if (success == 0)
            {
                throw new Exception(gl.GetShaderInfoLog(fragmentShader));
            }

            // Program
            Handle = gl.CreateProgram();

            gl.AttachShader(Handle, vertexShader);
            gl.AttachShader(Handle, fragmentShader);

            gl.LinkProgram(Handle);

            ProjectionMatrixLocation = gl.GetUniformLocation(Handle, "projection_matrix");
            TextureLocation = gl.GetUniformLocation(Handle, "Texture");

            gl.GetProgram(Handle, ProgramPropertyARB.LinkStatus, out success);
            if (success == 0)
            {
                throw new Exception(gl.GetProgramInfoLog(Handle));
            }

            // Shadery nie są już potrzebne po linkowaniu
            gl.DetachShader(Handle, vertexShader);
            gl.DetachShader(Handle, fragmentShader);

            gl.DeleteShader(vertexShader);
            gl.DeleteShader(fragmentShader);
        }

        public void Use()
        {
            gl.UseProgram(Handle);
        }

        public void Dispose()
        {
            gl.DeleteProgram(Handle);
        }
    }
}