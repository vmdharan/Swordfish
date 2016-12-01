
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SDI = SharpDX.DirectInput;
using SharpDX.DXGI;
using System.Diagnostics;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using System.Collections.Generic;
using SharpDX.Windows;
using System.Windows.Forms;

namespace Swordfish
{
    class Terrain
    {
        private Device dev;
        private DeviceContext devCon;

        private VertexShader vShader;
        private VertexShader vShader2;
        private PixelShader pShader;
        private PixelShader pShader2;
        private ShaderBytecode vsByteCode;
        private ShaderBytecode vsByteCode2;
        private ShaderBytecode psByteCode;
        private ShaderBytecode psByteCode2;
        private ShaderSignature signature;
        private ShaderSignature signature2;

        private InputLayout layout;
        private InputLayout layout2;
        private Buffer vertices;
        private Buffer cBuffer;
        private Buffer bBox;

        private float formWidth;
        private float formHeight;

        private Matrix view;
        private Matrix proj;

        private Stopwatch sw;

        private ShaderResourceView textureView;
        private SamplerState colorSampler;

        private float xRot;
        private float yRot;
        private float zRot;

        private Matrix viewProj;

        public TerrainEngine te;

        private bool drawBB;

        private BBCorners bbc;


        struct Vertex
        {
            public Vector4 Position;
            public Vector4 Normal;
            public Vector2 TexUV;
            public Vector4 Color;
        };

        struct Vertex2
        {
            public Vector4 Position;
        };

        struct cBufferStruct
        {
            public Matrix wvp;
            public Matrix world;
            public Matrix modelViewIT;
            public Matrix view;
            public Vector4 proj;
            public Vector4 lightDir;
            public Vector4 lightCol;
            public Vector4 lightCol2;
        };

        private Vertex[] vt;
        private List<Vertex> vl;

        private cBufferStruct cBufferData;

        Vector3 minV;
        Vector3 maxV;
        BoundingBox bb;

        public Terrain(ref Device srcDev, ref DeviceContext srcDevCon)
        {
            xRot = yRot = zRot = 0.0f;
            viewProj = Matrix.Identity;

            dev = srcDev;
            devCon = srcDevCon;

            te = new TerrainEngine();

            drawBB = false;


            var rot = Matrix.RotationX(xRot) * Matrix.RotationY(yRot) * Matrix.RotationZ(zRot);
            var wvp = rot * viewProj;
            //wvp *= Matrix.Translation(10.0f, 50.0f, 10.0f);
            wvp.Transpose();

            cBufferData = new cBufferStruct();
            cBufferData.wvp = wvp;
            cBufferData.lightDir = new Vector4(-10.50f, 70.00f, -10.50f, 1.0f);
            cBufferData.lightCol = new Vector4(0.35f, 0.4f, 0.45f, 1.0f);
            cBufferData.lightCol2 = new Vector4(0.35f, 0.4f, 0.45f, 1.0f);
        }

        // Set the screen dimensions.
        public void setDimensions(int w, int h)
        {
            formWidth = (float)w;
            formHeight = (float)h;
        }

        // Get the minimum value.
        public float getMin(float value, float prevMin)
        {
            if (value < prevMin)
            {
                return value;
            }

            return prevMin;
        }

        // Get the maximum value.
        public float getMax(float value, float prevMax)
        {
            if (value > prevMax)
            {
                return value;
            }

            return prevMax;
        }


        public void Initialise()
        {
            sw = new Stopwatch();
            sw.Start();

            // Set the minimum and maximum values for the bounding box.
            minV = new Vector3(1000.0f, 1000.0f, 1000.0f);
            maxV = new Vector3(-1000.0f, -1000.0f, -1000.0f);

            // Get the terrain size.
            int te_width = te.getTerrainSize();
            int te_length = te.getTerrainSize();

            // Initialise the vertex array.
            vl = new List<Vertex>();
            vt = new Vertex[te_width * te_length];

            for (int i = 0; i < te_width; i++)
            {
                for (int j = 0; j < te_length; j++)
                {
                    vt[te_width * i + j] = new Vertex()
                    {
                        Position = new Vector4(te.heightmap[i, j].position.X,
                            te.heightmap[i, j].position.Y,
                            te.heightmap[i, j].position.Z,
                            1.0f),
                        Normal = new Vector4(te.heightmap[i, j].normal.X,
                            te.heightmap[i, j].normal.Y,
                            te.heightmap[i, j].normal.Z,
                            1.0f),
                        TexUV = new Vector2(0.5f, 0.5f),
                        Color = new Vector4(0.13f, 0.5f, 0.2f, 1.0f)
                    };

                    // Perform a check for the min and max values.
                    // Update the bounds accordingly.
                    minV.X = getMin(vt[te_width * i + j].Position.X, minV.X);
                    minV.Y = getMin(vt[te_width * i + j].Position.Y, minV.Y);
                    minV.Z = getMin(vt[te_width * i + j].Position.Z, minV.Z);

                    maxV.X = getMax(vt[te_width * i + j].Position.X, maxV.X);
                    maxV.Y = getMax(vt[te_width * i + j].Position.Y, maxV.Y);
                    maxV.Z = getMax(vt[te_width * i + j].Position.Z, maxV.Z);
                }
            }

            for (int i = 0; i < te_width - 1; i++)
            {
                for (int j = 0; j < te_length - 1; j++)
                {
                    // Triangle 1
                    int index = i * te_width + j;
                    vl.Add(new Vertex()
                    {
                        Position = new Vector4(vt[index].Position.X,
                            vt[index].Position.Y,
                            vt[index].Position.Z,
                            vt[index].Position.W),
                        Normal = new Vector4(vt[index].Normal.X,
                            vt[index].Normal.Y,
                            vt[index].Normal.Z,
                            vt[index].Normal.W),
                        TexUV = new Vector2(0.0f, 0.0f),
                        Color = new Vector4(0.3f, 0.7f, 0.5f, 1.0f)
                    });

                    index = (i) * te_width + (j + 1);
                    vl.Add(new Vertex()
                    {
                        Position = new Vector4(vt[index].Position.X,
                            vt[index].Position.Y,
                            vt[index].Position.Z,
                            vt[index].Position.W),
                        Normal = new Vector4(vt[index].Normal.X,
                            vt[index].Normal.Y,
                            vt[index].Normal.Z,
                            vt[index].Normal.W),
                        TexUV = new Vector2(0.0f, 1.0f),
                        Color = new Vector4(0.3f, 0.7f, 0.5f, 1.0f)
                    });

                    index = (i + 1) * te_width + (j + 1);
                    vl.Add(new Vertex()
                    {
                        Position = new Vector4(vt[index].Position.X,
                            vt[index].Position.Y,
                            vt[index].Position.Z,
                            vt[index].Position.W),
                        Normal = new Vector4(vt[index].Normal.X,
                            vt[index].Normal.Y,
                            vt[index].Normal.Z,
                            vt[index].Normal.W),
                        TexUV = new Vector2(1.0f, 1.0f),
                        Color = new Vector4(0.3f, 0.7f, 0.5f, 1.0f)
                    });

                    // Triangle 2
                    index = (i + 1) * te_width + (j + 1);
                    vl.Add(new Vertex()
                    {
                        Position = new Vector4(vt[index].Position.X,
                            vt[index].Position.Y,
                            vt[index].Position.Z,
                            vt[index].Position.W),
                        Normal = new Vector4(vt[index].Normal.X,
                            vt[index].Normal.Y,
                            vt[index].Normal.Z,
                            vt[index].Normal.W),
                        TexUV = new Vector2(1.0f, 1.0f),
                        Color = new Vector4(0.3f, 0.7f, 0.5f, 1.0f)
                    });

                    index = (i + 1) * te_width + (j);
                    vl.Add(new Vertex()
                    {
                        Position = new Vector4(vt[index].Position.X,
                            vt[index].Position.Y,
                            vt[index].Position.Z,
                            vt[index].Position.W),
                        Normal = new Vector4(vt[index].Normal.X,
                            vt[index].Normal.Y,
                            vt[index].Normal.Z,
                            vt[index].Normal.W),
                        TexUV = new Vector2(1.0f, 0.0f),
                        Color = new Vector4(0.3f, 0.7f, 0.5f, 1.0f)
                    });

                    index = i * te_width + (j);
                    vl.Add(new Vertex()
                    {
                        Position = new Vector4(vt[index].Position.X,
                            vt[index].Position.Y,
                            vt[index].Position.Z,
                            vt[index].Position.W),
                        Normal = new Vector4(vt[index].Normal.X,
                            vt[index].Normal.Y,
                            vt[index].Normal.Z,
                            vt[index].Normal.W),
                        TexUV = new Vector2(0.0f, 0.0f),
                        Color = new Vector4(0.3f, 0.7f, 0.5f, 1.0f)
                    });
                }
            }

            var vt2 = vl.ToArray();
            vertices = Buffer.Create(dev, BindFlags.VertexBuffer, vt2);
            //vertices = new Buffer(dev, Utilities.SizeOf<Vertex>() * vl.Count, 
            //    ResourceUsage.Dynamic, BindFlags.VertexBuffer,
            //    CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            //dev.ImmediateContext.UpdateSubresource(vt2, vertices);


            bbc = new BBCorners(minV, maxV);
            var bb8corners = bbc.getBB8Corners();
            bb = bbc.getBoundingBox();
            bBox = Buffer.Create(dev, BindFlags.VertexBuffer, bb8corners);

            // Vertex and Pixel shaders.
            vsByteCode = ShaderBytecode.CompileFromFile("TerrainShader.hlsl", "VSMain", "vs_5_0", ShaderFlags.None, EffectFlags.None);
            vShader = new VertexShader(dev, vsByteCode);

            psByteCode = ShaderBytecode.CompileFromFile("TerrainShader.hlsl", "PSMain", "ps_5_0", ShaderFlags.None, EffectFlags.None);
            pShader = new PixelShader(dev, psByteCode);

            vsByteCode2 = ShaderBytecode.CompileFromFile("BBoxShader.hlsl", "VSMain", "vs_5_0", ShaderFlags.None, EffectFlags.None);
            vShader2 = new VertexShader(dev, vsByteCode2);

            psByteCode2 = ShaderBytecode.CompileFromFile("BBoxShader.hlsl", "PSMain", "ps_5_0", ShaderFlags.None, EffectFlags.None);
            pShader2 = new PixelShader(dev, psByteCode2);

            signature = ShaderSignature.GetInputSignature(vsByteCode);
            signature2 = ShaderSignature.GetInputSignature(vsByteCode2);

            // Input layout.
            layout = new InputLayout(dev, signature, new[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, InputElement.AppendAligned, 0),
                new InputElement("COLOR", 0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 0)
            });

            layout2 = new InputLayout(dev, signature2, new[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0)
            });

            // Constant buffer
            cBuffer = new Buffer(dev, Utilities.SizeOf<cBufferStruct>(), ResourceUsage.Default, BindFlags.ConstantBuffer,
                CpuAccessFlags.None, ResourceOptionFlags.None, 0);

            // Texture
            ImageLoader imgLoader = new ImageLoader();
            var texture = imgLoader.loadImage("grasstex.png", ref dev);
            textureView = new ShaderResourceView(dev, texture);

            colorSampler = new SamplerState(dev, new SamplerStateDescription()
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                BorderColor = Color.Black,
                ComparisonFunction = Comparison.Never,
                MaximumAnisotropy = 16,
                MipLodBias = 0,
                MinimumLod = -float.MaxValue,
                MaximumLod = float.MaxValue
            });


            // Set up matrices
            Vector3 eye = new Vector3(-20, 5, -20);
            Vector3 at = new Vector3(0, 0, 0);
            Vector3 up = Vector3.UnitY;
            view = Matrix.LookAtLH(eye, at, up);

            proj = Matrix.Identity;

            // Set up projection matrix with correct aspect ratio.
            proj = Matrix.PerspectiveFovLH((float)MathUtil.Pi / 4.0f,
                ((float)formWidth / (float)formHeight),
                0.1f, 1000.0f);
        }

        public void Render()
        {
            devCon.InputAssembler.InputLayout = layout;
            devCon.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            devCon.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertices,
                Utilities.SizeOf<Vertex>(), 0));

            devCon.VertexShader.Set(vShader);
            devCon.VertexShader.SetConstantBuffer(0, cBuffer);
            devCon.PixelShader.Set(pShader);
            devCon.PixelShader.SetConstantBuffer(0, cBuffer);
            devCon.PixelShader.SetShaderResource(0, textureView);
            devCon.PixelShader.SetSampler(0, colorSampler);


            devCon.Draw(vl.Count, 0);

            /*var wvp = Matrix.Translation(0.0f, 0.1f, 0.0f) * viewProj;
            wvp.Transpose();
            cBufferData.wvp = wvp;
            devCon.UpdateSubresource(ref cBufferData, cBuffer);
            devCon.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            devCon.Draw(vl.Count, 0);*/

            // draw the bounding box
            if (drawBB == true)
            {
                devCon.InputAssembler.InputLayout = layout2;
                devCon.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineStrip;
                devCon.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(bBox,
                    Utilities.SizeOf<Vertex2>(), 0));

                devCon.VertexShader.Set(vShader2);
                devCon.VertexShader.SetConstantBuffer(0, cBuffer);
                devCon.PixelShader.Set(pShader2);
                devCon.PixelShader.SetConstantBuffer(0, cBuffer);
                devCon.Draw(20, 0);
            }
        }

        // Automatically rotate around Y-axis.
        public void Update()
        {
            float t = sw.ElapsedMilliseconds / 1000.0f;
            viewProj = Matrix.Multiply(view, proj);
            var wvp = Matrix.RotationY(t * 1) * viewProj;
            wvp.Transpose();
            cBufferData.wvp = wvp;
            devCon.UpdateSubresource(ref cBufferData, cBuffer);
        }

        // Rotate based on mouse input.
        public void Update(ref DXInput di)
        {
            float t = sw.ElapsedMilliseconds / 1000.0f;
            viewProj = Matrix.Multiply(view, proj);

            // Left mouse button
            if ((di.currMouseState.Buttons[0] == true) && (di.prevMouseState.Buttons[0] == true))
            {
                xRot += 0.00025f;
            }
            // Middle mouse button
            else if ((di.currMouseState.Buttons[2] == true) && (di.prevMouseState.Buttons[2] == true))
            {
                zRot += 0.00025f;
            }
            // Right mouse button
            else if ((di.currMouseState.Buttons[1] == true) && (di.prevMouseState.Buttons[1] == true))
            {
                yRot += 0.00025f;
            }

            var rot = Matrix.RotationX(xRot) * Matrix.RotationY(yRot) * Matrix.RotationZ(zRot);
            var wvp = rot * viewProj;
            wvp.Transpose();
            cBufferData.wvp = wvp;
            devCon.UpdateSubresource(ref cBufferData, cBuffer);
        }

        // Rotate based on ArcCamera.
        public void Update(ref DXInput di, ref ArcCamera arc)
        {
            float t = sw.ElapsedMilliseconds / 1000.0f;
            viewProj = Matrix.Multiply(arc.GetViewMatrix(), proj);

            // Left mouse button
            if ((di.currMouseState.Buttons[0] == true) && (di.prevMouseState.Buttons[0] == true))
            {
                xRot += 0.00125f;
            }
            // Middle mouse button
            else if ((di.currMouseState.Buttons[2] == true) && (di.prevMouseState.Buttons[2] == true))
            {
                zRot += 0.00125f;
            }
            // Right mouse button
            else if ((di.currMouseState.Buttons[1] == true) && (di.prevMouseState.Buttons[1] == true))
            {
                yRot += 0.00125f;
            }

            // Apply zoom distance using Q and Z.
            if ((di.currKeyboardState.IsPressed(SDI.Key.Q)) && (di.prevKeyboardState.IsPressed(SDI.Key.Q)))
            {
                arc.ApplyZoom(0.05f);
            }
            if ((di.currKeyboardState.IsPressed(SDI.Key.Z)) && (di.prevKeyboardState.IsPressed(SDI.Key.Z)))
            {
                arc.ApplyZoom(-0.05f);
            }

            float yawDelta = 0.0f;
            float pitchDelta = 0.0f;

            // Apply zoom distance using Q and Z.
            if ((di.currKeyboardState.IsPressed(SDI.Key.Q)) && (di.prevKeyboardState.IsPressed(SDI.Key.Q)))
            {
                arc.ApplyZoom(0.005f);
            }
            else if ((di.currKeyboardState.IsPressed(SDI.Key.Z)) && (di.prevKeyboardState.IsPressed(SDI.Key.Z)))
            {
                arc.ApplyZoom(-0.005f);
            }
            // Apply yaw using A and D.
            if ((di.currKeyboardState.IsPressed(SDI.Key.A)) && (di.prevKeyboardState.IsPressed(SDI.Key.A)))
            {
                yawDelta = -0.001f;
            }
            else if ((di.currKeyboardState.IsPressed(SDI.Key.D)) && (di.prevKeyboardState.IsPressed(SDI.Key.D)))
            {
                yawDelta = 0.001f;
            }
            // Apply pitch using W and S.
            if ((di.currKeyboardState.IsPressed(SDI.Key.W)) && (di.prevKeyboardState.IsPressed(SDI.Key.W)))
            {
                pitchDelta = -0.001f;
            }
            else if ((di.currKeyboardState.IsPressed(SDI.Key.S)) && (di.prevKeyboardState.IsPressed(SDI.Key.S)))
            {
                pitchDelta = 0.001f;
            }

            arc.ApplyRotation(yawDelta, pitchDelta);

            var rot = Matrix.RotationX(xRot) * Matrix.RotationY(yRot) * Matrix.RotationZ(zRot);
            var wvp = rot * viewProj;
            wvp.Transpose();
            cBufferData.wvp = wvp;
            devCon.UpdateSubresource(ref cBufferData, cBuffer);
        }

        // Rotate based on FPSCamera.
        public void Update(ref DXInput di, ref FPSCamera cam, ref RenderForm form)
        {
            float t = sw.ElapsedMilliseconds / 1000.0f;
            viewProj = Matrix.Multiply(cam.GetViewMatrix(), proj);

            // Left mouse button
            if ((di.currMouseState.Buttons[0] == true) && (di.prevMouseState.Buttons[0] == true))
            {
                xRot += 0.0025f;
            }
            // Middle mouse button
            else if ((di.currMouseState.Buttons[2] == true) && (di.prevMouseState.Buttons[2] == true))
            {
                zRot += 0.0025f;
            }
            // Right mouse button
            else if ((di.currMouseState.Buttons[1] == true) && (di.prevMouseState.Buttons[1] == true))
            {
                yRot += 0.0025f;
            }

            float yawDelta = 0.0f;
            float pitchDelta = 0.0f;

            float moveFactorV = 0.001f;
            float moveFactorH = 0.001f;
            float pitchYawFactor = 0.00005f;

            // Move up or down using Q and Z.
            if ((di.currKeyboardState.IsPressed(SDI.Key.Q)) && (di.prevKeyboardState.IsPressed(SDI.Key.Q)))
            {
                //cam.ApplyZoom(0.005f);
                cam.MoveVertically(moveFactorV);
            }
            else if ((di.currKeyboardState.IsPressed(SDI.Key.Z)) && (di.prevKeyboardState.IsPressed(SDI.Key.Z)))
            {
                //cam.ApplyZoom(-0.005f);
                cam.MoveVertically(-moveFactorV);
            }
            // Strafe left or right using A and D.
            if ((di.currKeyboardState.IsPressed(SDI.Key.A)) && (di.prevKeyboardState.IsPressed(SDI.Key.A)))
            {
                yawDelta = -0.001f;
                cam.Strafe(-moveFactorH);
            }
            else if ((di.currKeyboardState.IsPressed(SDI.Key.D)) && (di.prevKeyboardState.IsPressed(SDI.Key.D)))
            {
                yawDelta = 0.001f;
                cam.Strafe(moveFactorH);
            }
            // Move forward or backward using W and S.
            if ((di.currKeyboardState.IsPressed(SDI.Key.W)) && (di.prevKeyboardState.IsPressed(SDI.Key.W)))
            {
                pitchDelta = -0.001f;
                cam.MoveForward(moveFactorH);
            }
            else if ((di.currKeyboardState.IsPressed(SDI.Key.S)) && (di.prevKeyboardState.IsPressed(SDI.Key.S)))
            {
                pitchDelta = 0.001f;
                cam.MoveForward(-moveFactorH);
            }

            // Pitch the camera using I and K.
            if ((di.currKeyboardState.IsPressed(SDI.Key.I)) && (di.prevKeyboardState.IsPressed(SDI.Key.I)))
            {
                cam.Pitch(-pitchYawFactor);
            }
            else if ((di.currKeyboardState.IsPressed(SDI.Key.K)) && (di.prevKeyboardState.IsPressed(SDI.Key.K)))
            {
                cam.Pitch(pitchYawFactor);
            }
            // Yaw the camera using J and L.
            if ((di.currKeyboardState.IsPressed(SDI.Key.J)) && (di.prevKeyboardState.IsPressed(SDI.Key.J)))
            {
                cam.Yaw(-pitchYawFactor);
            }
            else if ((di.currKeyboardState.IsPressed(SDI.Key.L)) && (di.prevKeyboardState.IsPressed(SDI.Key.L)))
            {
                cam.Yaw(pitchYawFactor);
            }

            //cam.ApplyRotation(yawDelta, pitchDelta);


            // Limit camera position.
            float camX = cam.getCamX();
            float camY = cam.getCamY();
            float camZ = cam.getCamZ();
            int updated = 0;

            int te_size = te.getTerrainSize();

            for (int i = 0; i < te_size; i++)
            {
                for (int j = 0; j < te_size; j++)
                {
                    if (((int)vt[te_size * i + j].Position.X == (int)camX)
                        && ((int)vt[te_size * i + j].Position.Z == (int)camZ))
                    {
                        if (camY < vt[te_size * i + j].Position.Y)
                        {
                            cam.setCamY(vt[te_size * i + j].Position.Y + 0.5f);
                        }

                        updated = 1;
                        break;
                    }
                }

                if (updated == 1) { break; }
            }

            viewProj = Matrix.Multiply(cam.GetViewMatrix(), proj);

            var rot = Matrix.RotationX(xRot) * Matrix.RotationY(yRot) * Matrix.RotationZ(zRot);
            cBufferData.world = rot;
            var wvp = viewProj;// * rot;
            wvp.Transpose();
            cBufferData.wvp = wvp;
            cBufferData.view = cam.GetViewMatrix();
            cBufferData.modelViewIT = Matrix.Transpose(Matrix.Invert(cBufferData.world));
            devCon.UpdateSubresource(ref cBufferData, cBuffer);

            // Get mouse coordinates
            float xc = di.mouseX;
            float yc = di.mouseY;
            float zc = di.mouseZ;

            // Get the system cursor position.
            System.Drawing.Point p = new System.Drawing.Point(Cursor.Position.X, Cursor.Position.Y);
            //xc = p.X;
            //yc = p.Y;

            xc = form.PointToClient(p).X;
            yc = form.PointToClient(p).Y;
            //xc = (2.0f * xc / formWidth) - 1.0f;
            //yc = ((2.0f * yc / formHeight) - 1.0f) * -1.0f;


            //int minCursorX = form.DesktopLocation.X;
            //int minCursorY = form.DesktopLocation.Y;
            //int maxCursorX = form.DesktopBounds.Right;
            //int maxCursorY = form.DesktopBounds.Bottom;

            /*if(xc < minCursorX) { xc = minCursorX; }
            if(yc < minCursorY) { yc = minCursorY; }
            if(xc > maxCursorX) { xc = maxCursorX; }
            if(yc > maxCursorY) { yc = maxCursorY; }*/

            //System.Console.WriteLine("{0} {1} {2}", xc, yc, zc);
            //System.Console.WriteLine("{0} {1} {2} {3}", minCursorX, minCursorY, maxCursorX, maxCursorY);

            //System.Console.WriteLine("{0} {1}", xc, yc);

            bool rp = rayPick(new Vector2(xc, yc), bb);

            if (rp == true)
            {
                cBufferData.lightCol2 = new Vector4(1.3f, 1.4f, 1.45f, 1.0f);
            }
            else
            {
                cBufferData.lightCol2 = new Vector4(0.13f, 0.14f, 0.145f, 1.0f);
            }

            devCon.UpdateSubresource(ref cBufferData, cBuffer);
        }

        public bool rayPick(Vector2 mCoord, BoundingBox bb)
        {
            int w = (int)formWidth;
            int h = (int)formHeight;

            ViewportF viewport = new ViewportF();
            viewport.Height = formHeight;
            viewport.Width = formWidth;
            viewport.MaxDepth = 1.0f;
            viewport.MinDepth = 0.0f;
            viewport.X = 0.0f;
            viewport.Y = 0.0f;

            Ray ray2 = Ray.GetPickRay((int)mCoord.X, (int)mCoord.Y, viewport, viewProj);

            /*
            mCoord.X = mCoord.X / proj.M11;
            mCoord.Y = mCoord.Y / proj.M22;
            Matrix invView = Matrix.Invert(view);

            Vector3 ZNearPlane = Vector3.Unproject(new Vector3(mCoord, 0.0f), 0, 0, w, h, -512.0f, 512.0f, invView);
            Vector3 ZFarPlane = Vector3.Unproject(new Vector3(mCoord, 1.0f), 0, 0, w, h, -512.0f, 512.0f, invView);
            Vector3 Direction = (ZFarPlane - ZNearPlane);
            Direction.Normalize();

            Ray ray = new Ray(ZNearPlane, Direction);
            */


            if (ray2.Intersects(bb))
            {
                //System.Console.WriteLine("{0} {1} {2}", ray2.Position.X, ray2.Position.Y, ray2.Position.Z);
                return true;
            }

            return false;
        }


        public void CleanUp()
        {
            vsByteCode.Dispose();
            vShader.Dispose();
            psByteCode.Dispose();
            pShader.Dispose();

            vertices.Dispose();
            layout.Dispose();

            cBuffer.Dispose();
        }
    }
}
