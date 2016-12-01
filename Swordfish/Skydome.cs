
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
    class Skydome
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
        private Buffer cBuffer;

        private float formWidth;
        private float formHeight;

        private Matrix view;
        private Matrix proj;

        private ShaderResourceView textureView;
        private SamplerState colorSampler;

        private X_Mesh_Loader x1;

        private Matrix viewProj;

        struct Vertex
        {
            public Vector4 Position;
            public Vector2 TexUV;
        };


        public Skydome(ref Device srcDev, ref DeviceContext srcDevCon)
        {
            viewProj = Matrix.Identity;

            dev = srcDev;
            devCon = srcDevCon;

            x1 = new X_Mesh_Loader();
            x1.readSourceFile("skydome.x");
            x1.readMeshTokens();
            x1.readMeshNormalTokens();
            x1.readMeshTextureUVTokens();
        }

        public void setDimensions(int w, int h)
        {
            formWidth = (float)w;
            formHeight = (float)h;
        }

        public void Initialise()
        {
            Vertex[] vt = new Vertex[x1.indices.Count];
            for (int n = 0; n < (x1.indices.Count); n++)
            {
                int m = x1.indices[n];

                vt[n].Position[0] = x1.vertices[3 * m] * 256;
                vt[n].Position[1] = x1.vertices[3 * m + 2] * 256 - 50;
                vt[n].Position[2] = x1.vertices[3 * m + 1] * 256;
                vt[n].Position[3] = 1.0f;

                //vt[n].normal.x = x1->normals[3 * m];
                //vt[n].normal.y = x1->normals[3 * m + 2];
                //vt[n].normal.z = x1->normals[3 * m + 1];

                vt[n].TexUV[0] = x1.texUV[2 * m];
                vt[n].TexUV[1] = x1.texUV[2 * m + 1];
            }

            vertices = Buffer.Create(dev, BindFlags.VertexBuffer, vt);


            // Vertex and Pixel shaders.
            vsByteCode = ShaderBytecode.CompileFromFile("cubeTexShader.hlsl", "VSMain", "vs_5_0", ShaderFlags.None, EffectFlags.None);
            vShader = new VertexShader(dev, vsByteCode);

            psByteCode = ShaderBytecode.CompileFromFile("cubeTexShader.hlsl", "PSMain", "ps_5_0", ShaderFlags.None, EffectFlags.None);
            pShader = new PixelShader(dev, psByteCode);

            signature = ShaderSignature.GetInputSignature(vsByteCode);

            // Input layout.
            layout = new InputLayout(dev, signature, new[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, InputElement.AppendAligned, 0)
            });

            // Constant buffer
            cBuffer = new Buffer(dev, Utilities.SizeOf<Matrix>(), ResourceUsage.Default, BindFlags.ConstantBuffer,
                CpuAccessFlags.None, ResourceOptionFlags.None, 0);

            // Texture
            ImageLoader imgLoader = new ImageLoader();
            var texture = imgLoader.loadImage("skydome2.png", ref dev);
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
            devCon.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertices, Utilities.SizeOf<Vector3>() * 2, 0));

            devCon.VertexShader.Set(vShader);
            devCon.VertexShader.SetConstantBuffer(0, cBuffer);
            devCon.PixelShader.Set(pShader);
            devCon.PixelShader.SetShaderResource(0, textureView);
            devCon.PixelShader.SetSampler(0, colorSampler);

            devCon.Draw(x1.indices.Count, 0);

            //var trn = Matrix.Translation(0.0f, 0.0f, 3.0f);
            //var rot = Matrix.RotationX(xRot) * Matrix.RotationY(yRot) * Matrix.RotationZ(zRot);
            //var wvp = trn * viewProj;
            //wvp.Transpose();
            //devCon.UpdateSubresource(ref wvp, cBuffer);
            //devCon.Draw(x1.indices.Count, 0);
        }


        // Rotate based on FPSCamera.
        public void Update(ref DXInput di, ref FPSCamera cam)
        {
            viewProj = Matrix.Multiply(cam.GetViewMatrix(), proj);


            // Move up or down using Q and Z.
            if ((di.currKeyboardState.IsPressed(SDI.Key.Q)) && (di.prevKeyboardState.IsPressed(SDI.Key.Q)))
            {
                //cam.ApplyZoom(0.005f);
                cam.MoveVertically(0.01f);
            }
            else if ((di.currKeyboardState.IsPressed(SDI.Key.Z)) && (di.prevKeyboardState.IsPressed(SDI.Key.Z)))
            {
                //cam.ApplyZoom(-0.005f);
                cam.MoveVertically(-0.01f);
            }
            // Strafe left or right using A and D.
            if ((di.currKeyboardState.IsPressed(SDI.Key.A)) && (di.prevKeyboardState.IsPressed(SDI.Key.A)))
            {
                cam.Strafe(-0.01f);
            }
            else if ((di.currKeyboardState.IsPressed(SDI.Key.D)) && (di.prevKeyboardState.IsPressed(SDI.Key.D)))
            {
                cam.Strafe(0.01f);
            }
            // Move forward or backward using W and S.
            if ((di.currKeyboardState.IsPressed(SDI.Key.W)) && (di.prevKeyboardState.IsPressed(SDI.Key.W)))
            {
                cam.MoveForward(0.01f);
            }
            else if ((di.currKeyboardState.IsPressed(SDI.Key.S)) && (di.prevKeyboardState.IsPressed(SDI.Key.S)))
            {
                cam.MoveForward(-0.01f);
            }

            // Pitch the camera using I and K.
            if ((di.currKeyboardState.IsPressed(SDI.Key.I)) && (di.prevKeyboardState.IsPressed(SDI.Key.I)))
            {
                cam.Pitch(-0.0001f);
            }
            else if ((di.currKeyboardState.IsPressed(SDI.Key.K)) && (di.prevKeyboardState.IsPressed(SDI.Key.K)))
            {
                cam.Pitch(0.0001f);
            }
            // Yaw the camera using J and L.
            if ((di.currKeyboardState.IsPressed(SDI.Key.J)) && (di.prevKeyboardState.IsPressed(SDI.Key.J)))
            {
                cam.Yaw(-0.0001f);
            }
            else if ((di.currKeyboardState.IsPressed(SDI.Key.L)) && (di.prevKeyboardState.IsPressed(SDI.Key.L)))
            {
                cam.Yaw(0.0001f);
            }

            //cam.ApplyRotation(yawDelta, pitchDelta);

            //var rot = Matrix.RotationX(xRot) * Matrix.RotationY(yRot) * Matrix.RotationZ(zRot);
            var wvp = viewProj; // * rot;
                                // var wvp = view;
            wvp.Transpose();
            devCon.UpdateSubresource(ref wvp, cBuffer);
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
