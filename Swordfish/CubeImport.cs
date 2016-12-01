
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Diagnostics;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using SDI = SharpDX.DirectInput;

namespace Swordfish
{
    class CubeImport
    {
        private Device dev;
        private DeviceContext devCon;

        private VertexShader vShader;
        private PixelShader pShader;
        private ShaderBytecode vsByteCode;
        private ShaderBytecode psByteCode;
        private ShaderSignature signature;

        private InputLayout layout;
        private Buffer vertices;
        private Buffer indices;
        private Buffer cBuffer;

        private float formWidth;
        private float formHeight;

        private Matrix view;
        private Matrix proj;

        private Stopwatch sw;

        private ShaderResourceView textureView;
        private SamplerState colorSampler;

        private X_Mesh_Loader x1;

        private float xRot;
        private float yRot;
        private float zRot;

        private Matrix viewProj;

        struct Vertex
        {
            public Vector4 Position;
            public Vector4 Normal;
            public Vector2 TexUV;
            public Vector4 Color;
        };

        struct cBufferStruct
        {
            public Matrix wvp;
            public Matrix world;
            public Vector4 view;
            public Vector4 proj;
            public Vector4 lightDir;
            public Vector4 lightCol;
            public Vector4 lightCol2;
        };

        private cBufferStruct cBufferData;


        public CubeImport(ref Device srcDev, ref DeviceContext srcDevCon)
        {
            xRot = yRot = zRot = 0.0f;
            viewProj = Matrix.Identity;

            dev = srcDev;
            devCon = srcDevCon;

            x1 = new X_Mesh_Loader();
            x1.readSourceFile("cube.x");
            //x1.readSourceFile("terrain6s.x");
            x1.readMeshTokens();
            x1.readMeshNormalTokens();
            x1.readMeshTextureUVTokens();

            var rot = Matrix.RotationX(xRot) * Matrix.RotationY(yRot) * Matrix.RotationZ(zRot);
            var wvp = rot; // * viewProj;
            //wvp *= Matrix.Translation(10.0f, 50.0f, 10.0f);
            wvp.Transpose();

            cBufferData = new cBufferStruct();
            cBufferData.wvp = wvp;
            cBufferData.world = viewProj;
            //cBufferData.lightDir = new Vector4(2.667f, 2.667f, -5.567f, 1.0f);
            cBufferData.lightDir = new Vector4(-10.50f, 70.00f, -10.50f, 1.0f);
            cBufferData.lightCol = new Vector4(0.3f, 0.4f, 0.45f, 1.0f);
            cBufferData.lightCol2 = new Vector4(0.3f, 0.4f, 0.45f, 1.0f);
        }

        public void setDimensions(int w, int h)
        {
            formWidth = (float)w;
            formHeight = (float)h;
        }

        public void Initialise()
        {
            sw = new Stopwatch();
            sw.Start();

            Vertex[] vt = new Vertex[x1.indices.Count];
            for (int n = 0; n < (x1.indices.Count); n++)
            {
                int m = x1.indices[n];

                vt[n].Position[0] = x1.vertices[3 * m];
                vt[n].Position[1] = x1.vertices[3 * m + 2];
                vt[n].Position[2] = x1.vertices[3 * m + 1];
                vt[n].Position[3] = 1.0f;

                vt[n].Normal[0] = x1.normals[3 * m];
                vt[n].Normal[1] = x1.normals[3 * m + 2];
                vt[n].Normal[2] = x1.normals[3 * m + 1];
                vt[n].Normal[3] = 1.0f;

                vt[n].TexUV[0] = x1.texUV[2 * m];
                vt[n].TexUV[1] = x1.texUV[2 * m + 1];

                vt[n].Color[0] = 1.0f;
                vt[n].Color[1] = 1.0f;
                vt[n].Color[2] = 1.0f;
                vt[n].Color[3] = 1.0f;
            }

            vertices = Buffer.Create(dev, BindFlags.VertexBuffer, vt);


            // Vertex and Pixel shaders.
            vsByteCode = ShaderBytecode.CompileFromFile("cubeTexShader2.hlsl", "VSMain", "vs_5_0", ShaderFlags.None, EffectFlags.None);
            vShader = new VertexShader(dev, vsByteCode);

            psByteCode = ShaderBytecode.CompileFromFile("cubeTexShader2.hlsl", "PSMain", "ps_5_0", ShaderFlags.None, EffectFlags.None);
            pShader = new PixelShader(dev, psByteCode);

            signature = ShaderSignature.GetInputSignature(vsByteCode);

            // Input layout.
            layout = new InputLayout(dev, signature, new[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, InputElement.AppendAligned, 0),
                new InputElement("COLOR", 0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 0)
            });

            // Constant buffer
            cBuffer = new Buffer(dev, Utilities.SizeOf<cBufferStruct>(), ResourceUsage.Default, BindFlags.ConstantBuffer,
                CpuAccessFlags.None, ResourceOptionFlags.None, 0);

            // Texture
            ImageLoader imgLoader = new ImageLoader();
            var texture = imgLoader.loadImage("cube_uv.png", ref dev);
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
            Vector3 eye = new Vector3(-8, 5, -8);
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
            devCon.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertices, Utilities.SizeOf<Vertex>(), 0));
            //devCon.InputAssembler.SetIndexBuffer(indices, Format.R32_UInt, 0);

            devCon.VertexShader.Set(vShader);
            devCon.VertexShader.SetConstantBuffer(0, cBuffer);
            devCon.PixelShader.Set(pShader);
            devCon.PixelShader.SetConstantBuffer(0, cBuffer);
            devCon.PixelShader.SetShaderResource(0, textureView);
            devCon.PixelShader.SetSampler(0, colorSampler);

            //devCon.DrawIndexed(36, 0, 0);
            devCon.Draw(x1.indices.Count, 0);

            /*
            var trn = Matrix.Translation(0.0f, 0.0f, 3.0f);
            //var rot = Matrix.RotationX(xRot) * Matrix.RotationY(yRot) * Matrix.RotationZ(zRot);
            var wvp = trn * viewProj;
            wvp.Transpose();
            devCon.UpdateSubresource(ref wvp, cBuffer);
            devCon.Draw(x1.indices.Count, 0);
            */
        }

        // Automatically rotate around Y-axis.
        public void Update()
        {
            float t = sw.ElapsedMilliseconds / 1000.0f;
            viewProj = Matrix.Multiply(view, proj);
            var wvp = Matrix.RotationY(t * 1) * viewProj;
            wvp.Transpose();
            devCon.UpdateSubresource(ref wvp, cBuffer);
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
            devCon.UpdateSubresource(ref wvp, cBuffer);
        }

        // Rotate based on ArcCamera.
        public void Update(ref DXInput di, ref ArcCamera arc)
        {
            float t = sw.ElapsedMilliseconds / 1000.0f;
            viewProj = Matrix.Multiply(arc.GetViewMatrix(), proj);

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
            devCon.UpdateSubresource(ref wvp, cBuffer);
        }

        // Rotate based on FPSCamera.
        public void Update(ref DXInput di, ref FPSCamera cam)
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

            // Move up or down using Q and Z.
            if ((di.currKeyboardState.IsPressed(SDI.Key.Q)) && (di.prevKeyboardState.IsPressed(SDI.Key.Q)))
            {
                //cam.ApplyZoom(0.005f);
                cam.MoveVertically(0.001f);
            }
            else if ((di.currKeyboardState.IsPressed(SDI.Key.Z)) && (di.prevKeyboardState.IsPressed(SDI.Key.Z)))
            {
                //cam.ApplyZoom(-0.005f);
                cam.MoveVertically(-0.001f);
            }
            // Strafe left or right using A and D.
            if ((di.currKeyboardState.IsPressed(SDI.Key.A)) && (di.prevKeyboardState.IsPressed(SDI.Key.A)))
            {
                yawDelta = -0.001f;
                cam.Strafe(-0.001f);
            }
            else if ((di.currKeyboardState.IsPressed(SDI.Key.D)) && (di.prevKeyboardState.IsPressed(SDI.Key.D)))
            {
                yawDelta = 0.001f;
                cam.Strafe(0.001f);
            }
            // Move forward or backward using W and S.
            if ((di.currKeyboardState.IsPressed(SDI.Key.W)) && (di.prevKeyboardState.IsPressed(SDI.Key.W)))
            {
                pitchDelta = -0.001f;
                cam.MoveForward(0.001f);
            }
            else if ((di.currKeyboardState.IsPressed(SDI.Key.S)) && (di.prevKeyboardState.IsPressed(SDI.Key.S)))
            {
                pitchDelta = 0.001f;
                cam.MoveForward(-0.001f);
            }

            // Pitch the camera using I and K.
            if ((di.currKeyboardState.IsPressed(SDI.Key.I)) && (di.prevKeyboardState.IsPressed(SDI.Key.I)))
            {
                cam.Pitch(-0.001f);
            }
            else if ((di.currKeyboardState.IsPressed(SDI.Key.K)) && (di.prevKeyboardState.IsPressed(SDI.Key.K)))
            {
                cam.Pitch(0.001f);
            }
            // Yaw the camera using J and L.
            if ((di.currKeyboardState.IsPressed(SDI.Key.J)) && (di.prevKeyboardState.IsPressed(SDI.Key.J)))
            {
                cam.Yaw(-0.001f);
            }
            else if ((di.currKeyboardState.IsPressed(SDI.Key.L)) && (di.prevKeyboardState.IsPressed(SDI.Key.L)))
            {
                cam.Yaw(0.001f);
            }

            //cam.ApplyRotation(yawDelta, pitchDelta);
            var trn = Matrix.Translation(cBufferData.lightDir.X, cBufferData.lightDir.Y, cBufferData.lightDir.Z);
            var rot = Matrix.RotationX(xRot) * Matrix.RotationY(yRot) * Matrix.RotationZ(zRot);
            cBufferData.world = rot * trn;
            var wvp = rot * trn * viewProj;
            wvp.Transpose();
            cBufferData.wvp = wvp;
            devCon.UpdateSubresource(ref cBufferData, cBuffer);
        }


        public void CleanUp()
        {
            vsByteCode.Dispose();
            vShader.Dispose();
            psByteCode.Dispose();
            pShader.Dispose();

            vertices.Dispose();
            //indices.Dispose();
            layout.Dispose();

            cBuffer.Dispose();
        }
    }
}
