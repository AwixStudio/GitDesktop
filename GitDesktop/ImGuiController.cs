using ImGuiNET;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.IO;

namespace GitDesktop
{
    internal class ImGuiController
    {
        private readonly IWindow _window;
        private readonly GL _gl;
        private readonly ImGuiIOPtr _io;
        private readonly Shader _shader;

        private uint _fontTexture;

        private uint _vertexArray; // VAO (Vertex Array Object) - description of the vertex data layout
        private uint _vertexBuffer; // VBO (Vertex Buffer Object) - buffer for vertex data
        private uint _indexBuffer; // EBO (Element Buffer Object) - buffer for index data (which vertices should connect to form triangles)

        public ImGuiController(IWindow window, GL gl)
        {
            _window = window;
            _gl = gl;

            ImGui.CreateContext();

            _io = ImGui.GetIO();

            _io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            //_io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable; // Enable multi-viewport later
            
            _shader = CreateShaders();

            CreateDeviceResources();
        }

        private Shader CreateShaders()
        {           
            string vertexSource = File.ReadAllText("Shaders/ImGuiVert.glsl");
            string fragmentSource = File.ReadAllText("Shaders/ImGuiFrag.glsl");
            return new Shader(_gl, vertexSource, fragmentSource);
        }

        private void CreateDeviceResources()
        {
            CreateBuffers();
            CreateFontTexture();            
        }

        private unsafe void CreateBuffers()
        {
            _vertexArray = _gl.GenVertexArray(); // generate a new empty OpenGL vertex array and store its ID in _vertexArray
            _vertexBuffer = _gl.GenBuffer();
            _indexBuffer = _gl.GenBuffer();

            _gl.BindVertexArray(_vertexArray); // start working on _vertexArray
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBuffer); // start working on _vertexBuffer before vertex attributes to tell VBA to use this VBO for vertex attributes
            // define the layout of the vertex data in the vertex array
            // position attribute
            _gl.VertexAttribPointer(
                0, // atribute index
                2, // values count in the attribute
                GLEnum.Float, // type of the values
                false, // should the values be normalized to 0-1 (use for colors to change from 0-255 to 0-1)
                20, // size in bytes of a single vertex (ImDrawVert has 2 floats for position, 2 floats for UV, and 1 uint for color: 2*4 + 2*4 + 4 = 20)
                (void*)0); // offset in bytes of the first value of the attribute in the vertex (position is the first attribute in ImDrawVert)
            _gl.EnableVertexAttribArray(0); // by default, all attributes are disabled
            _gl.VertexAttribPointer(1, 2, GLEnum.Float, false, 20, (void*)8); // UV attribute (offset is 8 bytes: 2 floats for position)
            _gl.EnableVertexAttribArray(1);
            // we interpret the color (one uint (4 bytes)) as 4 unsigned bytes to store each color separately (RGBA)
            _gl.VertexAttribPointer(2, 4, GLEnum.UnsignedByte, true, 20, (void*)16); // color attribute
            _gl.EnableVertexAttribArray(2);            

            // instead of specifying size and offset in bytes, we could use sizeof(ImDrawVert) for the size of a single vertex
            // and Marshal.OffsetOf<ImDrawVert>("pos") to get the size and offset of the attributes in the ImDrawVert struct
            // or better Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.pos))          
            
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer); // start working on EBO to tell VAO to use this EBO for index data
            _gl.BindVertexArray(0);
        }

        private unsafe void CreateFontTexture()
        {
            // Generate the font texture from default ImGui's font atlas
            _io.Fonts.GetTexDataAsRGBA32(
                        out IntPtr pixels,
                        out int width,
                        out int height,
                        out int bytesPerPixel);

            _fontTexture = _gl.GenTexture(); // generate a new empty OpenGL texture and store its ID in _fontTexture

            _gl.BindTexture(TextureTarget.Texture2D, _fontTexture); // now start working on _fontTexture
            // insert pixels into the texture, with the specified width, height and format
            _gl.TexImage2D(
                        TextureTarget.Texture2D,
                        0, // mipmap level
                        InternalFormat.Rgba,
                        (uint)width,
                        (uint)height,
                        0,
                        PixelFormat.Rgba,
                        PixelType.UnsignedByte,
                        (void*)pixels);
            // set texture parameters for minification and magnification filters
            // minification filter is used when the target is smaller than ogiginal texture, magnification filter is used when the target is larger than original texture
            _gl.TexParameter(
                        TextureTarget.Texture2D,
                        TextureParameterName.TextureMinFilter,
                        (int)GLEnum.Linear);
            _gl.TexParameter(
                        TextureTarget.Texture2D,
                        TextureParameterName.TextureMagFilter,
                        (int)GLEnum.Linear);
            _gl.BindTexture(TextureTarget.Texture2D, 0); // stop working on _fontTexture

            _io.Fonts.SetTexID((nint)_fontTexture); // tell ImGui to use the generated OpenGL texture ID for rendering text
            _io.Fonts.ClearTexData(); // delete the CPU texture data in ImGui after uploading to GPU by OpenGL
        }

        public void Update(double deltaTime)
        {
            _io.DeltaTime = (float)deltaTime;
            _io.DisplaySize = new Vector2(_window.Size.X, _window.Size.Y);

            ImGui.NewFrame();
        }

        public unsafe void Render()
        {
            ImGui.Render(); // missleading naming convention: ImGui.Render() generates the draw data for the current frame not the actual rendering of the frame
                        
            ImDrawDataPtr drawData = ImGui.GetDrawData(); // Contains VBO and EBO for the current frame

            UploadVertexBuffer(drawData);
            UploadIndexBuffer(drawData);
            RenderDrawData(drawData);
        }

        private unsafe void UploadVertexBuffer(ImDrawDataPtr drawData)
        {
            int vertexBufferSize = drawData.TotalVtxCount * sizeof(ImDrawVert);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBuffer);
            _gl.BufferData(
                        BufferTargetARB.ArrayBuffer,
                        (nuint)vertexBufferSize,
                        null,
                        BufferUsageARB.StreamDraw);

            int vertexOffset = 0; // in bytes
            for (int i = 0; i < drawData.CmdListsCount; i++)
            {
                ImDrawListPtr list = drawData.CmdLists[i];
                int size = list.VtxBuffer.Size * sizeof(ImDrawVert);

                _gl.BufferSubData(
                            BufferTargetARB.ArrayBuffer, 
                            (nint)vertexOffset,          
                            (nuint)size,
                            (void*)list.VtxBuffer.Data);

                vertexOffset += size;
            }
        }

        private unsafe void UploadIndexBuffer(ImDrawDataPtr drawData)
        {
            int indexBufferSize = drawData.TotalIdxCount * sizeof(ushort);

            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer);
            _gl.BufferData(
                BufferTargetARB.ElementArrayBuffer,
                (nuint)indexBufferSize,
                null,
                BufferUsageARB.StreamDraw);

            int indexOffset = 0;
            for (int i = 0; i < drawData.CmdListsCount; i++)
            {
                ImDrawListPtr list = drawData.CmdLists[i];

                int size = list.IdxBuffer.Size * sizeof(ushort);

                _gl.BufferSubData(
                            BufferTargetARB.ElementArrayBuffer,
                            (nint)indexOffset,
                            (nuint)size,
                            (void*)list.IdxBuffer.Data);

                indexOffset += size;
            }
        }

        private unsafe void RenderDrawData(ImDrawDataPtr drawData)
        {
            _shader.Use();
            _gl.Uniform1(_shader.TextureLocation, 0); // use TextureUnit.Texture0

            float left = drawData.DisplayPos.X;
            float right = drawData.DisplayPos.X + drawData.DisplaySize.X;

            float top = drawData.DisplayPos.Y;
            float bottom = drawData.DisplayPos.Y + drawData.DisplaySize.Y;

            float[] projection =
            {
                2.0f / (right - left), 0.0f,                    0.0f, 0.0f,
                0.0f,                  2.0f / (top - bottom),  0.0f, 0.0f,
                0.0f,                  0.0f,                  -1.0f, 0.0f,
                (right + left) / (left - right),
                (top + bottom) / (bottom - top),
                0.0f,
                1.0f
            };

            fixed (float* matrix = projection)
            {
                _gl.UniformMatrix4(
                    _shader.ProjectionMatrixLocation,
                    1,
                    false,
                    matrix);
            }            

            _gl.BindVertexArray(_vertexArray);

            int globalIdxOffset = 0;
            int globalVtxOffset = 0;
            for (int i = 0; i < drawData.CmdListsCount; i++)
            {
                ImDrawListPtr list = drawData.CmdLists[i];

                for (int j = 0; j < list.CmdBuffer.Size; j++)
                {
                    ImDrawCmdPtr cmd = list.CmdBuffer[j];
                    nint indexOffset = (nint)((cmd.IdxOffset + globalIdxOffset) * sizeof(ushort));
                    nint baseVertex = (nint)(cmd.VtxOffset + globalVtxOffset);

                    _gl.ActiveTexture(TextureUnit.Texture0);
                    _gl.BindTexture(TextureTarget.Texture2D, (uint)cmd.TextureId);
                    _gl.DrawElementsBaseVertex(
                                        PrimitiveType.Triangles,
                                        cmd.ElemCount,
                                        DrawElementsType.UnsignedShort,
                                        (void*)indexOffset,
                                        (int)baseVertex);
                }

                globalIdxOffset += list.IdxBuffer.Size;
                globalVtxOffset += list.VtxBuffer.Size;
            }

            _gl.BindVertexArray(0);
        }
    }
}
