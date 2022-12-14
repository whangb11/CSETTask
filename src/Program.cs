using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Numerics;
using System.ComponentModel.Design;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Threading.Channels;
using OpenTK.Compute.OpenCL;
using System.Diagnostics;
using System.Drawing;



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



        UI ui;
        public Gwindow(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings() { Size = (width, height), Title = title })
        {
            Gwindow.width = width;
            Gwindow.height = height;
            Gwindow.vertexDatas=new List<float>();
            Gwindow.indiceDatas=new List<uint>();
            ui = new UI(720, 720);
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
            ui.Render();

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
            
            if (ui.imoperatableCountDown <= 0 && KeyboardState.IsKeyPressed(Keys.Z))
            {
                ui.Undo();
            }

            if (ui.imoperatableCountDown <= 0 && KeyboardState.IsKeyPressed(Keys.R))
            {
                ui.Remake(null);
            }
#if DEBUG
            if (KeyboardState.IsKeyPressed(Keys.Tab))
            {
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            }

            if (ui.imoperatableCountDown <= 0 && KeyboardState.IsKeyPressed(Keys.C))
            {
                ui.Remake(++ui.difficult);
            }
#endif
            if (MouseState.IsButtonPressed(MouseButton.Left))
            {
                ui.Activate(MouseState.X, MouseState.Y);
            }

            

            ui.Frame();
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
        public Vector2 location;
        protected Vector3 color;
        protected Vector2 velocity;
        protected Vector2 accelerate;
        public List<Vector2> vertices;
        protected UInt64 timer = 0;

        public static readonly Vector3 White = new(0.9f, 0.9f, 0.9f);
        public static readonly Vector3 Black = new(0.1f, 0.1f, 0.1f);

        protected BaseUnit()
        {
            RawInit(new Vector2(0, 0), new Vector3(0, 0, 0), new Vector2(0, 0), new Vector2(0, 0), new List<Vector2> { });
            vertices = new List<Vector2> { };
        }
        public BaseUnit(Vector2 location, Vector3 color, Vector2 velocity, Vector2 accelerate, List<Vector2> vertices)
        {
            RawInit(location, color, velocity, accelerate, vertices);
        }

        public BaseUnit(Vector2 location, Vector3 color, List<Vector2> vertices)
        {
            RawInit(location, color, new Vector2(0, 0), new Vector2(0, 0), vertices);
        }

        public virtual void Frame()
        {
            this.RawFrame();
        }

        public virtual int Activate()
        {
            return 0;
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
    }

    public class UI
    {
        public class CheckerBoard
        {
            
            public List<List<Piece>> pieces;
            

            private readonly int row;
            private readonly int col;
            private readonly float mxx = 720;
            private readonly float mxy = 720;
            private readonly float pitchX;
            private readonly float pitchY;
            Random rand = new Random(unchecked((int)System.DateTime.UtcNow.Ticks));
            public CheckerBoard(float pitchX, float pitchY, float mxx, float mxy,int row,int col)//row?????????col??????
            {
                this.row = row;
                this.col = col;

                this.mxx = mxx;
                this.mxy = mxy;

                this.pitchX = pitchX;
                this.pitchY = pitchY;

                pieces = new List<List<Piece>> { };
                for(int i= 0; i < row; i++)
                {
                    pieces.Add(new List<Piece> { });
                    for(int j=0; j < col; j++)
                    {
                        pieces[i].Add(new Piece(mxx / (row * 2) + mxx / row * i - mxx / 2 + pitchX, mxy / (col * 2) + mxx / col * j - mxy / 2 + pitchY, (float)(mxx / row * 0.9 / 2), (float)(mxy / row * 0.9 / 2)));
                    }
                }
            }

            public void Frame()
            {
                for(int i = 0; i < pieces.Count; i++)
                {
                    for(int j = 0; j < pieces[i].Count; j++)
                    {
                        pieces[i][j].Frame();
                    }
                }
            }

            public void Render()
            {
                for (int i = 0; i < pieces.Count; i++)
                {
                    for (int j = 0; j < pieces[i].Count; j++)
                    {
                        pieces[i][j].Render();
                    }
                }
            }

            public enum ACTIVATE_RESULT
            {
                FAILURE,
                SUCCESS
            }
            public int Activate(float cx,float cy)
            {
                Console.WriteLine("mouseClick at("+Convert.ToString(cx)+","+Convert.ToString(cy)+")");
                for(int i=0; i<pieces.Count; i++)
                {
                    for(int j = 0; j < pieces[i].Count; j++)
                    {
                        if (Math.Abs(cx) < mxx && Math.Abs(cy) < mxy && Math.Abs(pieces[i][j].Location().X - cx) < mxx / row / 2 && Math.Abs(pieces[i][j].Location().Y - cy) < mxy / col / 2)
                        {
                            pieces[i][j].Activate();
                            Console.WriteLine("acitvate piece in row" + Convert.ToString(i + 1) + "col" + Convert.ToString(j + 1));
                            if (i != 0) pieces[i - 1][j].Activate();            //???
                            if (j != 0) pieces[i][j - 1].Activate();            //???
                            if (i != row - 1) pieces[i + 1][j].Activate();      //???
                            if (j != col - 1) pieces[i][j + 1].Activate();      //???
                            Console.WriteLine("as a state of " + Convert.ToString(pieces[i][j].state));
                        }
                    }
                }
                int H = 0;
                for (int i = 0; i < pieces.Count; i++)
                {
                    for (int j = 0; j < pieces.Count; j++)
                    {
                        if (pieces[i][j].state == pieces[0][0].state)
                            H++;
                    }
                }

                if (H == row * col)
                    return (int)ACTIVATE_RESULT.SUCCESS;
                else
                    return (int)ACTIVATE_RESULT.FAILURE;
            }

            public void Shuffle(int times)
            {   
                for(int i=0; i<times; i++)
                {
                    Activate(rand.Next((int)(0 - mxx / 2 + this.pitchX),(int)((mxx / 2) + this.pitchX)), rand.Next((int)(0 - mxy / 2 + this.pitchY), (int)(mxy / 2.0f + this.pitchY)));
                }
            }
        }

        public class MultipleEP : BaseUnit
        {
            int edges;
            float size;
            public MultipleEP(float x, float y, float size,int n)
            {
                this.edges = n + 3;
                this.size = size;
                RawInit(new Vector2(x, y), new Vector3(0.9f, 0.9f, 0.9f), new Vector2(0, 0), new Vector2(0, 0), new List<Vector2>());

                for(int i=0; i<this.edges; i++)
                {
                    base.vertices.Add(new Vector2((float)(size * Math.Cos(2 * Math.PI / n * i + Math.PI / 2)), (float)(size * Math.Sin(2 * Math.PI / n * i + Math.PI / 2))));
                }
            }

            public void SetValue(int n)
            {
                this.edges = n + 3;
                base.vertices.Clear();
                for (int i = 0; i < this.edges; i++)
                {
                    base.vertices.Add(new Vector2((float)(this.size * Math.Cos(2 * Math.PI / n * i + Math.PI / 2)), (float)(this.size * Math.Sin(2 * Math.PI / n * i + Math.PI / 2))));
                }
            }
        }
        
        List<Vector2> activationRecord;
        UI.CheckerBoard checkerBoard;
        Bottom.InGame.ProcessBar processBar;
        Bottom.InGame.UndoBottom undoBottom;
        Bottom.InGame.RemakeBottom remakeBottom;

        int mxx;
        int mxy;
#if DEBUG 
        public int difficult;
#else
        int difficult;
#endif

        List<UI.MultipleEP> culmulators;
        BaseUnit lBgU;
        BaseUnit lBgD;
        public int imoperatableCountDown = 0;
        public int remakeCountDown = -1;

        public UI(int mxx,int mxy)
        {
            this.mxx = mxx;
            this.mxy = mxy;
            difficult = 3;

            checkerBoard=new CheckerBoard(120,0,this.mxx,this.mxy,difficult,difficult);
            checkerBoard.Shuffle(100);

            processBar = new Bottom.InGame.ProcessBar(-260, 0, 18, Gwindow.height / 2);
            processBar.maxProcess = difficult * difficult;

            activationRecord = new List<Vector2>();
            culmulators = new List<MultipleEP>();
            for (int i = 1; i <= 3; i++)
            {
                culmulators.Add(new MultipleEP(i * 50 - Gwindow.width / 2, 100, 20, 0));
                //culmulators.Add(new MultipleEP(0, 0, 20, 0));
            }

            lBgU = new BaseUnit(new Vector2(-390, Gwindow.height/2), new Vector3(0.9f, 0.9f, 0.9f), new List<Vector2> { new Vector2(100, 230), new Vector2(-80, 230), new Vector2(-80, -130), new Vector2(100, -130) });
            lBgD = new BaseUnit(new Vector2(-390, (0-Gwindow.height)/2), new Vector3(0.9f, 0.9f, 0.9f), new List<Vector2> { new Vector2(100, 440), new Vector2(-80, 440), new Vector2(-80, -440), new Vector2(100, -440) });

            undoBottom = new Bottom.InGame.UndoBottom(-380, 120, 90, 30, 13, 13);
            remakeBottom = new Bottom.InGame.RemakeBottom(-380, 190, 90, 30, 13, 10, 250);
        }

        public void Frame()
        {
            checkerBoard.Frame();

            int H = 0;
            for(int i = 0; i < checkerBoard.pieces.Count; i++)
            {
                for(int j = 0; j < checkerBoard.pieces[i].Count; j++)
                {
                    H += (checkerBoard.pieces[i][j].state+1)/2;
                }
            }

            processBar.currProcess = H;
            processBar.Frame();
            for(int i=0;i<culmulators.Count; i++)
            {
                culmulators[i].Frame();
            }

            if (remakeCountDown > 0)
                remakeCountDown--;
            if (remakeCountDown == 0)
            {
                Remake(++difficult);
                remakeCountDown = -1;
            }
                

            if(imoperatableCountDown > 0)
                imoperatableCountDown--;

            lBgD.Frame();
            lBgU.Frame();

            undoBottom.Frame();
        }

        public void Render()
        {
            checkerBoard.Render();
            processBar.Render();
            for (int i = 0; i < culmulators.Count; i++)
            {
                culmulators[i].Render();
            }
            lBgD.Render();
            lBgU.Render();

            undoBottom.Render();
            remakeBottom.Render();
        }

        public void Activate(float x, float y)
        {
            if (imoperatableCountDown <= 0)
            {
                Console.WriteLine("\n\n\n--------activate--------");
                var cx = x - Gwindow.width / 2;
                var cy = Gwindow.height - y - Gwindow.height / 2;
                Console.WriteLine("at location(" + cx + "," + cy + ")");
                if (cx > -250)   
                {
                    var result = checkerBoard.Activate(cx, cy);
                    activationRecord.Add(new Vector2(cx, cy));
                    Console.WriteLine("resulting in " + Convert.ToString(result));

                    if (result == (int)CheckerBoard.ACTIVATE_RESULT.SUCCESS)
                    {
                        remakeCountDown = 60;
                        imoperatableCountDown = 60;
                    }
                }
                
                if (undoBottom.CheckColli(cx, cy))
                {
                    Undo();
                }

                if (remakeBottom.CheckColli(cx, cy))
                {
                    Remake(null);
                }
            }
        }

        public void Undo()
        {
            if(activationRecord.Count > 0)
            {
                Console.WriteLine("--------undo--------");
                checkerBoard.Activate(activationRecord[activationRecord.Count-1].X, activationRecord[activationRecord.Count - 1].Y);
                activationRecord.RemoveAt(activationRecord.Count-1);
            }
            
        }

        public void Remake(int? complexy)
        {
            if (complexy.HasValue == false)
                complexy = this.difficult;
            difficult=(int)complexy;

            activationRecord.Clear();
            checkerBoard = new CheckerBoard(120, 0, mxx, mxy, difficult, difficult);

            processBar.maxProcess = difficult*difficult;
            checkerBoard.Shuffle(difficult*difficult);
        }
    }

    public class Piece : BaseUnit
    {
        public Int16 state;
        public int flag;
        enum PROPERTIES
        {
            NORMAL,
            EXPAND,
        }

        public Piece(float x,float y,float lx,float ly)//x,y,x??????,y??????
        {
            RawInit(new Vector2(x, y), new Vector3(0.9f, 0.9f, 0.9f), new Vector2(0, 0), new Vector2(0, 0), new List<Vector2>());
            this.state = 1;

            base.vertices.Add(new Vector2(lx, ly));
            base.vertices.Add(new Vector2(-lx, ly));
            base.vertices.Add(new Vector2(-lx, -ly));
            base.vertices.Add(new Vector2(lx, -ly));

            this.flag = (int)Piece.PROPERTIES.NORMAL;
        }

        public override int Activate()
        {
            const float colourPitch= 0.8f;
            this.state *= -1;
            base.color.X += state * colourPitch;
            base.color.Y += state * colourPitch;
            base.color.Z += state * colourPitch;

            return this.flag;
        }
    }

    public class Bottom
    {
        enum BOTTOMRESULT
        {
            CONTINUE,
            BACK,
            BOARDUP,
            BOARDDOWN
        }
        public class Menu
        {
            
        }

        public class InGame
        {
            public class ProcessBar
            {
                public int maxProcess = 1;
                public int currProcess = 0;

                public BaseUnit upperBar;
                public BaseUnit lowerBar;
                public ProcessBar(int x,int y,int lx,int ly)//lx:???x  ly:???y
                {
                    upperBar = new BaseUnit(new Vector2(x, Gwindow.height + y), new Vector3(0.1f, 0.1f, 0.1f), new Vector2(0, 0), new Vector2(0, 0), new List<Vector2> { new Vector2(lx, ly), new Vector2(-lx, ly), new Vector2(-lx, -ly), new Vector2(lx, -ly) });
                    lowerBar = new BaseUnit(new Vector2(x, 0 - Gwindow.height + y), new Vector3(0.9f, 0.9f, 0.9f), new Vector2(0, 0), new Vector2(0, 0), new List<Vector2> { new Vector2(lx, ly), new Vector2(-lx, ly), new Vector2(-lx, -ly), new Vector2(lx, -ly) });
                }
                
                public void Frame()
                {
                    upperBar.location.Y = 0;
                    lowerBar.location.Y = 0 - Gwindow.height + Gwindow.height * currProcess / maxProcess;

                    upperBar.Frame();
                    lowerBar.Frame();
                }

                public void Render()
                {
                    upperBar.Render();
                    lowerBar.Render();
                }
            }

            public class UndoBottom
            {
                BaseUnit bg;
                BaseUnit shape;
                float x;
                float y;
                float d;
                float h;


                public UndoBottom(float x,float y,float d,float h,float dt,float ht)
                {
                    this.x = x;
                    this.y = y;
                    this.d = d;
                    this.h = h;


                    bg = new BaseUnit(new Vector2(x, y),BaseUnit.White, new Vector2(0, 0), new Vector2(0, 0), new List<Vector2> { new Vector2(d, h), new Vector2(0 - d, h), new Vector2(0 - d, 0 - h), new Vector2(d, 0 - h) });
                    shape = new BaseUnit(new Vector2(x + dt / 2, y), BaseUnit.Black, new List<Vector2> { new Vector2(0, ht), new Vector2(0 - dt, 0), new Vector2(0, -ht) });
                }

                public void Frame()
                {
                    bg.Frame();
                    shape.Frame();
                }

                public void Render()
                {
                    bg.Render();
                    shape.Render();
                }

                public bool CheckColli(float x,float y)
                {
                    return (Math.Abs(x - this.x) <= d && Math.Abs(y - this.y) <= h);
                }
            }

            public class RemakeBottom
            {
                BaseUnit bg;
                BaseUnit shapea;
                BaseUnit shapeb;
                float x;
                float y;
                float d;
                float h;


                public RemakeBottom(float x, float y, float d, float h, float r1,float r2,int edgeCount=100)
                {
                    this.x = x;
                    this.y = y;
                    this.d = d;
                    this.h = h;


                    bg = new BaseUnit(new Vector2(x, y), BaseUnit.White, new List<Vector2> { new Vector2(d, h), new Vector2(0 - d, h), new Vector2(0 - d, 0 - h), new Vector2(d, 0 - h) });
                    shapea = new BaseUnit(new Vector2(x, y), BaseUnit.Black, new List<Vector2> ());
                    shapeb = new BaseUnit(new Vector2(x, y), BaseUnit.White, new List<Vector2>());

                    for(int i=0; i < edgeCount; i++)
                    {
                        shapea.vertices.Add(new Vector2((float)Math.Cos(Math.PI * 2 * i / edgeCount) * r1, (float)Math.Sin(Math.PI * 2 * i / edgeCount) * r1));
                        shapeb.vertices.Add(new Vector2((float)Math.Cos(Math.PI * 2 * i / edgeCount) * r2, (float)Math.Sin(Math.PI * 2 * i / edgeCount) * r2));
                    }
                }

                public void Frame()
                {
                    bg.Frame();
                    shapea.Frame();
                    shapeb.Frame();
                }

                public void Render()
                {
                    bg.Render();
                    shapea.Render();
                    shapeb.Render();
                }

                public bool CheckColli(float x, float y)
                {
                    return (Math.Abs(x - this.x) <= d && Math.Abs(y - this.y) <= h);
                }
            }
        }
    }

    unsafe public class App
    {
        public static void Main()
        {
            const string name = "Fleep";
            const string version = "1.1";
            using (Gwindow gwindow=new Gwindow(960, 720,name+version))
            {
                gwindow.Run();
            }
            
        }
    }
}
