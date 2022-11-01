using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Numerics;
using System.ComponentModel.Design;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Compute.OpenCL;
using System.Threading.Channels;

namespace Nms
{
    public class Shader
    {
        public int Handle;
        int VertexShader;
        int FragmentShader;

        public Shader(string vertexPath, string fragmentPath)
        {
            string VertexShaderSource = File.ReadAllText(vertexPath);
            string FragmentShaderSource = File.ReadAllText(fragmentPath);

            VertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(VertexShader, VertexShaderSource);

            FragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(FragmentShader, FragmentShaderSource);


            GL.CompileShader(VertexShader);

            GL.GetShader(VertexShader, ShaderParameter.CompileStatus, out int vsuccess);
            if (vsuccess == 0)
            {
                string infoLog = GL.GetShaderInfoLog(VertexShader);
                Console.WriteLine(infoLog);
            }

            GL.CompileShader(FragmentShader);

            GL.GetShader(FragmentShader, ShaderParameter.CompileStatus, out int fsuccess);
            if (fsuccess == 0)
            {
                string infoLog = GL.GetShaderInfoLog(FragmentShader);
                Console.WriteLine(infoLog);
            }

            Handle = GL.CreateProgram();

            GL.AttachShader(Handle, VertexShader);
            GL.AttachShader(Handle, FragmentShader);
            GL.LinkProgram(Handle);

            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(Handle);
                Console.WriteLine(infoLog);
            }
        }

        public void Use()
        {
            GL.UseProgram(Handle);
        }

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                GL.DeleteProgram(Handle);

                disposedValue = true;
            }
        }

        ~Shader()
        {
            GL.DeleteProgram(Handle);
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(Handle, attribName);
        }
    }

    public class Gwindow:GameWindow
    {
        //vert:x,y,z,r,g,b
        private float[] vertices = { };
        public static List<float> vertexDatas;

        private uint[] indices = { };
        public static List<uint> indiceDatas;

        int VertexBufferObject;
        int ElementBufferObject;
        Shader shader;

        public static int width;
        public static int height;

        public List<BaseUnit> Units;
        int VertexArrayObject;

        CheckerBoardEA checkerBoard;
        public Gwindow(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings() { Size = (width, height), Title = title })
        {
            Gwindow.width = width;
            Gwindow.height = height;
            Gwindow.vertexDatas=new List<float> { };
            Gwindow.indiceDatas=new List<uint> { };
            checkerBoard = new CheckerBoardEA(10, 10);
        }
        
        

        unsafe protected override void OnLoad()
        {

            base.OnLoad();
            
            GLFW.SetWindowSizeLimits(base.WindowPtr,960,720,960,720);
            GL.ClearColor(0.3f, 0.3f, 0.3f, 1.0f);
            
            VertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
            
            VertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(VertexArrayObject);

            ElementBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);

            shader = new Shader("shaders\\shader.vert", "shaders\\shader.frag");
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            checkerBoard.Render();
            vertices = Gwindow.vertexDatas.ToArray();
            indices= Gwindow.indiceDatas.ToArray();

            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.DynamicDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            shader.Use();
            GL.BindVertexArray(VertexArrayObject);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

            SwapBuffers();

            Gwindow.vertexDatas.Clear();
            Gwindow.indiceDatas.Clear();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            if (MouseState.IsButtonPressed(MouseButton.Left))
            {
                checkerBoard.Activate(MouseState.X,MouseState.Y);
            }

            checkerBoard.Frame();
        }

        protected override void OnResize(ResizeEventArgs e)
        {

            base.OnResize(e);
            
            GL.Viewport(0, 0, e.Width, e.Height);
            Gwindow.width = e.Width;
            Gwindow.height = e.Height;
        }

        protected override void OnUnload()
        {
            base.OnUnload();
            shader.Dispose();
        }


    }

    public class BaseUnit
    {
        protected Vector2 location;
        protected Vector3 color;
        protected Vector2 velocity;
        protected Vector2 accelerate;
        protected List<Vector2> vertices;
        protected UInt64 timer = 0;
        protected BaseUnit()
        {
            RawInit(new Vector2(0, 0), new Vector3(0, 0, 0), new Vector2(0, 0), new Vector2(0, 0), new List<Vector2> { });
            vertices = new List<Vector2> { };
        }
        public BaseUnit(Vector2 location, Vector3 color, Vector2 velocity, Vector2 accelerate, List<Vector2> vertices)
        {
            RawInit(location, color, velocity, accelerate, vertices);
        }

        public virtual void Frame()
        {
            this.RawFrame();
        }

        public virtual void Activate()
        {

        }
        
        public virtual void Render()
        {
            RawRender();
        }
        protected void RawInit(Vector2 location, Vector3 color, Vector2 velocity, Vector2 accelerate, List<Vector2> vertices)
        {
            this.location = location;
            this.color = color;
            this.velocity = velocity;
            this.accelerate = accelerate;
            this.vertices = vertices;
        }

        protected void RawFrame()
        {
            this.timer += 1;
            this.location.X += this.velocity.X;
            this.location.Y += this.velocity.Y;
            this.velocity.X += this.accelerate.X;
            this.velocity.Y += this.accelerate.Y;
        }

        protected void RawRender()
        {
            int currVertexCount = Gwindow.vertexDatas.Count/6;

            Gwindow.vertexDatas.Add(location.X / Gwindow.width * 2);
            Gwindow.vertexDatas.Add(location.Y / Gwindow.height * 2);
            Gwindow.vertexDatas.Add(0);
            Gwindow.vertexDatas.Add(color.X);//r
            Gwindow.vertexDatas.Add(color.Y);//g
            Gwindow.vertexDatas.Add(color.Z);//b
            for (int i = 0; i < this.vertices.Count; i++)
            {
                Gwindow.vertexDatas.Add((location.X + vertices[i].X)/Gwindow.width * 2);
                Gwindow.vertexDatas.Add((location.Y + vertices[i].Y)/Gwindow.height * 2);
                Gwindow.vertexDatas.Add(0);
                Gwindow.vertexDatas.Add(color.X);
                Gwindow.vertexDatas.Add(color.Y);
                Gwindow.vertexDatas.Add(color.Z);
            }

            Gwindow.indiceDatas.Add(Convert.ToUInt32(currVertexCount));
            Gwindow.indiceDatas.Add(Convert.ToUInt32(currVertexCount+1));
            Gwindow.indiceDatas.Add(Convert.ToUInt32(currVertexCount+vertices.Count));
            for(int i = 1; i < vertices.Count; i++)
            {
                Gwindow.indiceDatas.Add(Convert.ToUInt32(currVertexCount));
                Gwindow.indiceDatas.Add(Convert.ToUInt32(currVertexCount + i));
                Gwindow.indiceDatas.Add(Convert.ToUInt32(currVertexCount + i+1));
            }

        }

        public Vector2 Location()
        {
            return this.location;
        }

        public bool RectiveColiCheck(float x,float y)
        {
            return true; 
        }
    }

    public class CheckerBoardEA
    {
        public List<List<Piece>> pieces;

        private readonly int row;
        private readonly int col;
        const int mxx = 600;
        const int mxy = 600;
        public CheckerBoardEA(int row,int col)//row横排，col竖排
        {
            this.row = row;
            this.col = col;

            pieces = new List<List<Piece>> { };
            for(int i= 0; i < row; i++)
            {
                pieces.Add(new List<Piece> { });
                for(int j=0; j < col; j++)
                {
                    pieces[i].Add(new Piece(mxx / (row * 2) + mxx / row * i - mxx / 2, mxy / (col * 2) + mxx / col * j - mxy / 2, Convert.ToInt32(mxx / row * 0.9 / 2), Convert.ToInt32(mxy / row * 0.9 / 2)));
                }
            }
        }

        public void Frame()
        {
            for(int i = 0; i < pieces.Count; i++)
            {
                for(int j = 0; j < pieces.Count; j++)
                {
                    pieces[i][j].Frame();
                }
            }
        }

        public void Render()
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                for (int j = 0; j < pieces.Count; j++)
                {
                    pieces[i][j].Render();
                }
            }
        }

        public void Activate(float x,float y)
        {


            var cx = x - Gwindow.width / 2;
            var cy = Gwindow.height - y - Gwindow.height / 2;
            Console.WriteLine("mouseClick at("+Convert.ToString(cx)+","+Convert.ToString(cy)+")");
            for(int i=0; i<pieces.Count; i++)
            {
                for(int j=0;j<pieces.Count; j++)
                {
                    if (Math.Abs(cx) < mxx && Math.Abs(cy) < mxy && Math.Abs(pieces[i][j].Location().X - cx) < mxx / row / 2 && Math.Abs(pieces[i][j].Location().Y - cy) < mxy / col / 2)
                    {
                        pieces[i][j].Activate();
                        //Console.WriteLine("acitvate piece in row"+Convert.ToString(i)+"col"+Convert.ToString(j));
                        if (i != 0) pieces[i - 1][j].Activate();            //左
                        if (j != 0) pieces[i][j - 1].Activate();            //上
                        if (i != row - 1) pieces[i + 1][j].Activate();      //右
                        if (j != col - 1) pieces[i][j + 1].Activate();      //下
                    }
                }
            }
        }
    }

    public class Piece : BaseUnit
    {
        Int16 state;
        
        public Piece(int x,int y,int lx,int ly)//x,y,x半径,y半径
        {
            RawInit(new Vector2(x, y), new Vector3(0.9f, 0.9f, 0.9f), new Vector2(0, 0), new Vector2(0, 0), new List<Vector2> { });
            this.state = 1;

            base.vertices.Add(new Vector2(lx, ly));
            base.vertices.Add(new Vector2(-lx, ly));
            base.vertices.Add(new Vector2(-lx, -ly));
            base.vertices.Add(new Vector2(lx, -ly));
        }

        public override void Activate()
        {
            const float colourPitch= 0.8f;
            this.state *= -1;
            base.color.X += state * colourPitch;
            base.color.Y += state * colourPitch;
            base.color.Z += state * colourPitch;

            
        }
    }
    unsafe public class App
    {
        List<BaseUnit> units;

        public static void Main()
        {
           using(Gwindow gwindow=new Gwindow(960, 720, "notitle"))
            {
                gwindow.Run();
            }
            
        }
    }
    

}
