using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace GitDesktop
{
    internal class ImGuiController : IDisposable
    {
        private readonly IWindow _window;
        private readonly GL _gl;
        private readonly ImGuiIOPtr _io;
        private readonly Shader _shader;

        private uint _fontTexture;

        private uint _vertexArray; // VAO (Vertex Array Object) - description of the vertex data layout
        private uint _vertexBuffer; // VBO (Vertex Buffer Object) - buffer for vertex data
        private uint _indexBuffer; // EBO (Element Buffer Object) - buffer for index data (which vertices should connect to form triangles)

        private readonly IInputContext _input;
        private readonly IMouse _mouse;
        private readonly IKeyboard _keyboard;

        private static readonly Dictionary<Key, ImGuiKey> _keyMap = new()
        {
            { Key.Tab, ImGuiKey.Tab },

            { Key.Left, ImGuiKey.LeftArrow },
            { Key.Right, ImGuiKey.RightArrow },
            { Key.Up, ImGuiKey.UpArrow },
            { Key.Down, ImGuiKey.DownArrow },

            { Key.PageUp, ImGuiKey.PageUp },
            { Key.PageDown, ImGuiKey.PageDown },

            { Key.Home, ImGuiKey.Home },
            { Key.End, ImGuiKey.End },

            { Key.Insert, ImGuiKey.Insert },
            { Key.Delete, ImGuiKey.Delete },

            { Key.Backspace, ImGuiKey.Backspace },

            { Key.Space, ImGuiKey.Space },

            { Key.Enter, ImGuiKey.Enter },
            { Key.Escape, ImGuiKey.Escape },

            { Key.Apostrophe, ImGuiKey.Apostrophe },
            { Key.Comma, ImGuiKey.Comma },
            { Key.Minus, ImGuiKey.Minus },
            { Key.Period, ImGuiKey.Period },
            { Key.Slash, ImGuiKey.Slash },

            { Key.Semicolon, ImGuiKey.Semicolon },
            { Key.Equal, ImGuiKey.Equal },

            { Key.LeftBracket, ImGuiKey.LeftBracket },
            { Key.BackSlash, ImGuiKey.Backslash },
            { Key.RightBracket, ImGuiKey.RightBracket },
            { Key.GraveAccent, ImGuiKey.GraveAccent },

            { Key.CapsLock, ImGuiKey.CapsLock },
            { Key.ScrollLock, ImGuiKey.ScrollLock },
            { Key.NumLock, ImGuiKey.NumLock },

            { Key.PrintScreen, ImGuiKey.PrintScreen },
            { Key.Pause, ImGuiKey.Pause },

            { Key.Keypad0, ImGuiKey.Keypad0 },
            { Key.Keypad1, ImGuiKey.Keypad1 },
            { Key.Keypad2, ImGuiKey.Keypad2 },
            { Key.Keypad3, ImGuiKey.Keypad3 },
            { Key.Keypad4, ImGuiKey.Keypad4 },
            { Key.Keypad5, ImGuiKey.Keypad5 },
            { Key.Keypad6, ImGuiKey.Keypad6 },
            { Key.Keypad7, ImGuiKey.Keypad7 },
            { Key.Keypad8, ImGuiKey.Keypad8 },
            { Key.Keypad9, ImGuiKey.Keypad9 },

            { Key.KeypadDecimal, ImGuiKey.KeypadDecimal },
            { Key.KeypadDivide, ImGuiKey.KeypadDivide },
            { Key.KeypadMultiply, ImGuiKey.KeypadMultiply },
            { Key.KeypadSubtract, ImGuiKey.KeypadSubtract },
            { Key.KeypadAdd, ImGuiKey.KeypadAdd },
            { Key.KeypadEnter, ImGuiKey.KeypadEnter },
            { Key.KeypadEqual, ImGuiKey.KeypadEqual },

            { Key.ShiftLeft, ImGuiKey.LeftShift },
            { Key.ShiftRight, ImGuiKey.RightShift },

            { Key.ControlLeft, ImGuiKey.LeftCtrl },
            { Key.ControlRight, ImGuiKey.RightCtrl },

            { Key.AltLeft, ImGuiKey.LeftAlt },
            { Key.AltRight, ImGuiKey.RightAlt },

            { Key.SuperLeft, ImGuiKey.LeftSuper },
            { Key.SuperRight, ImGuiKey.RightSuper },

            { Key.Menu, ImGuiKey.Menu },

            { Key.Number0, ImGuiKey._0 },
            { Key.Number1, ImGuiKey._1 },
            { Key.Number2, ImGuiKey._2 },
            { Key.Number3, ImGuiKey._3 },
            { Key.Number4, ImGuiKey._4 },
            { Key.Number5, ImGuiKey._5 },
            { Key.Number6, ImGuiKey._6 },
            { Key.Number7, ImGuiKey._7 },
            { Key.Number8, ImGuiKey._8 },
            { Key.Number9, ImGuiKey._9 },

            { Key.A, ImGuiKey.A },
            { Key.B, ImGuiKey.B },
            { Key.C, ImGuiKey.C },
            { Key.D, ImGuiKey.D },
            { Key.E, ImGuiKey.E },
            { Key.F, ImGuiKey.F },
            { Key.G, ImGuiKey.G },
            { Key.H, ImGuiKey.H },
            { Key.I, ImGuiKey.I },
            { Key.J, ImGuiKey.J },
            { Key.K, ImGuiKey.K },
            { Key.L, ImGuiKey.L },
            { Key.M, ImGuiKey.M },
            { Key.N, ImGuiKey.N },
            { Key.O, ImGuiKey.O },
            { Key.P, ImGuiKey.P },
            { Key.Q, ImGuiKey.Q },
            { Key.R, ImGuiKey.R },
            { Key.S, ImGuiKey.S },
            { Key.T, ImGuiKey.T },
            { Key.U, ImGuiKey.U },
            { Key.V, ImGuiKey.V },
            { Key.W, ImGuiKey.W },
            { Key.X, ImGuiKey.X },
            { Key.Y, ImGuiKey.Y },
            { Key.Z, ImGuiKey.Z },

            { Key.F1, ImGuiKey.F1 },
            { Key.F2, ImGuiKey.F2 },
            { Key.F3, ImGuiKey.F3 },
            { Key.F4, ImGuiKey.F4 },
            { Key.F5, ImGuiKey.F5 },
            { Key.F6, ImGuiKey.F6 },
            { Key.F7, ImGuiKey.F7 },
            { Key.F8, ImGuiKey.F8 },
            { Key.F9, ImGuiKey.F9 },
            { Key.F10, ImGuiKey.F10 },
            { Key.F11, ImGuiKey.F11 },
            { Key.F12, ImGuiKey.F12 }
        };

        public ImGuiController(IWindow window, GL gl, IInputContext input)
        {
            _window = window;
            _gl = gl;

            _input = input;
            _mouse = input.Mice[0];
            _mouse.Scroll += OnMouseScroll;
            _keyboard = input.Keyboards[0];
            _keyboard.KeyDown += OnKeyDown;
            _keyboard.KeyUp += OnKeyUp;
            _keyboard.KeyChar += OnKeyChar;

            ImGui.CreateContext();
            _io = ImGui.GetIO();     
            _io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            //_io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable; // Enable multi-viewport later
            
            _shader = CreateShaders();
            CreateDeviceResources();
        }

        private void OnKeyChar(IKeyboard keyboard, char c) => _io.AddInputCharacter(c);

        private void OnKeyUp(IKeyboard keyboard, Key key, int c)
        {
            _io.AddKeyEvent(ToImGuiKey(key), false);
            UpdateModifiers();
        }

        private void OnKeyDown(IKeyboard keyboard, Key key, int c)
        {
            _io.AddKeyEvent(ToImGuiKey(key), true);
            UpdateModifiers();
        }

        private static ImGuiKey ToImGuiKey(Key key)
        {
            if (_keyMap.TryGetValue(key, out ImGuiKey imguiKey))
            {
                return imguiKey;
            }

            return ImGuiKey.None;
        }

        private void UpdateModifiers()
        {
            _io.AddKeyEvent(
                ImGuiKey.ModCtrl,
                _keyboard.IsKeyPressed(Key.ControlLeft) ||
                _keyboard.IsKeyPressed(Key.ControlRight));

            _io.AddKeyEvent(
                ImGuiKey.ModShift,
                _keyboard.IsKeyPressed(Key.ShiftLeft) ||
                _keyboard.IsKeyPressed(Key.ShiftRight));

            _io.AddKeyEvent(
                ImGuiKey.ModAlt,
                _keyboard.IsKeyPressed(Key.AltLeft) ||
                _keyboard.IsKeyPressed(Key.AltRight));

            _io.AddKeyEvent(
                ImGuiKey.ModSuper,
                _keyboard.IsKeyPressed(Key.SuperLeft) ||
                _keyboard.IsKeyPressed(Key.SuperRight));
        }

        private void OnMouseScroll(IMouse mouse, ScrollWheel wheel) => _io.AddMouseWheelEvent(wheel.X, wheel.Y);

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

            _io.AddMousePosEvent(_mouse.Position.X, _mouse.Position.Y);
            _io.AddMouseButtonEvent(0, _mouse.IsButtonPressed(MouseButton.Left));
            _io.AddMouseButtonEvent(1, _mouse.IsButtonPressed(MouseButton.Right));
            _io.AddMouseButtonEvent(2, _mouse.IsButtonPressed(MouseButton.Middle));

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
            _gl.Enable(GLEnum.ScissorTest);

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

                    Vector4 clip = cmd.ClipRect;
                    _gl.Scissor(
                        (int)clip.X,
                        (int)(drawData.DisplaySize.Y - clip.W),
                        (uint)(clip.Z - clip.X),
                        (uint)(clip.W - clip.Y));

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
            _gl.Disable(GLEnum.ScissorTest);
        }

        public void Dispose()
        {
            _shader.Dispose();

            _gl.DeleteTexture(_fontTexture);

            _gl.DeleteBuffer(_vertexBuffer);
            _gl.DeleteBuffer(_indexBuffer);

            _gl.DeleteVertexArray(_vertexArray);

            ImGui.DestroyContext();
        }
    }
}
