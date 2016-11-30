
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Diagnostics;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace Swordfish
{
    class Cube
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

        public Cube(ref Device srcDev, ref DeviceContext srcDevCon)
        {
            dev = srcDev;
            devCon = srcDevCon;
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

            vertices = Buffer.Create(dev, BindFlags.VertexBuffer, new[]
            {
                new Vector4(-1.0f, 1.0f, -1.0f, 1.0f), new Vector4(-1.0f, 1.0f, -1.0f, 1.0f),
                new Vector4(1.0f, 1.0f, -1.0f, 1.0f), new Vector4(1.0f, 1.0f, -1.0f, 1.0f),
                new Vector4(1.0f, 1.0f, 1.0f, 1.0f), new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
                new Vector4(-1.0f, 1.0f, 1.0f, 1.0f), new Vector4(-1.0f, 1.0f, 1.0f, 1.0f),

                new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(-1.0f, -1.0f, -1.0f, 1.0f),
                new Vector4(1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, -1.0f, -1.0f, 1.0f),
                new Vector4(1.0f, -1.0f, 1.0f, 1.0f), new Vector4(1.0f, -1.0f, 1.0f, 1.0f),
                new Vector4(-1.0f, -1.0f, 1.0f, 1.0f), new Vector4(-1.0f, -1.0f, 1.0f, 1.0f),

                new Vector4(-1.0f, -1.0f, 1.0f, 1.0f), new Vector4(-1.0f, -1.0f, 1.0f, 1.0f),
                new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(-1.0f, -1.0f, -1.0f, 1.0f),
                new Vector4(-1.0f, 1.0f, -1.0f, 1.0f), new Vector4(-1.0f, 1.0f, -1.0f, 1.0f),
                new Vector4(-1.0f, 1.0f, 1.0f, 1.0f), new Vector4(-1.0f, 1.0f, 1.0f, 1.0f),

                new Vector4(1.0f, -1.0f, 1.0f, 1.0f), new Vector4(1.0f, -1.0f, 1.0f, 1.0f),
                new Vector4(1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, -1.0f, -1.0f, 1.0f),
                new Vector4(1.0f, 1.0f, -1.0f, 1.0f), new Vector4(1.0f, 1.0f, -1.0f, 1.0f),
                new Vector4(1.0f, 1.0f, 1.0f, 1.0f), new Vector4(1.0f, 1.0f, 1.0f, 1.0f),

                new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(-1.0f, -1.0f, -1.0f, 1.0f),
                new Vector4(1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, -1.0f, -1.0f, 1.0f),
                new Vector4(1.0f, 1.0f, -1.0f, 1.0f), new Vector4(1.0f, 1.0f, -1.0f, 1.0f),
                new Vector4(-1.0f, 1.0f, -1.0f, 1.0f), new Vector4(-1.0f, 1.0f, -1.0f, 1.0f),

                new Vector4(-1.0f, -1.0f, 1.0f, 1.0f), new Vector4(-1.0f, -1.0f, 1.0f, 1.0f),
                new Vector4(1.0f, -1.0f, 1.0f, 1.0f), new Vector4(1.0f, -1.0f, 1.0f, 1.0f),
                new Vector4(1.0f, 1.0f, 1.0f, 1.0f), new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
                new Vector4(-1.0f, 1.0f, 1.0f, 1.0f), new Vector4(-1.0f, 1.0f, 1.0f, 1.0f)
            });

            indices = Buffer.Create(dev, BindFlags.IndexBuffer, new[]
            {
                3, 1, 0,
                2, 1, 3,

                6, 4, 5,
                7, 4, 6,

                11, 9, 8,
                10, 9, 11,

                14, 12, 13,
                15, 12, 14,

                19, 17, 16,
                18, 17, 19,

                22, 20, 21,
                23, 20, 22
            });

            // Vertex and Pixel shaders.
            vsByteCode = ShaderBytecode.CompileFromFile("cubeShader.hlsl", "VSMain", "vs_5_0", ShaderFlags.None, EffectFlags.None);
            vShader = new VertexShader(dev, vsByteCode);

            psByteCode = ShaderBytecode.CompileFromFile("cubeShader.hlsl", "PSMain", "ps_5_0", ShaderFlags.None, EffectFlags.None);
            pShader = new PixelShader(dev, psByteCode);

            signature = ShaderSignature.GetInputSignature(vsByteCode);

            // Input layout.
            layout = new InputLayout(dev, signature, new[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 16, 0)
            });

            // Constant buffer
            cBuffer = new Buffer(dev, Utilities.SizeOf<Matrix>(), ResourceUsage.Default, BindFlags.ConstantBuffer,
                CpuAccessFlags.None, ResourceOptionFlags.None, 0);

            // Set up matrices
            Vector3 eye = new Vector3(-5, 3, -5);
            Vector3 at = new Vector3(0, 0, 0);
            Vector3 up = Vector3.UnitY;
            view = Matrix.LookAtLH(eye, at, up);

            proj = Matrix.Identity;

            // Set up projection matrix with correct aspect ratio.
            proj = Matrix.PerspectiveFovLH((float)MathUtil.Pi / 4.0f,
                ((float)formWidth / (float)formHeight),
                0.1f, 100.0f);
        }

        public void Render()
        {
            devCon.InputAssembler.InputLayout = layout;
            devCon.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            devCon.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertices, Utilities.SizeOf<Vector4>() * 2, 0));
            devCon.InputAssembler.SetIndexBuffer(indices, Format.R32_UInt, 0);

            devCon.VertexShader.Set(vShader);
            devCon.VertexShader.SetConstantBuffer(0, cBuffer);
            devCon.PixelShader.Set(pShader);

            devCon.DrawIndexed(36, 0, 0);
        }

        public void Update()
        {
            float t = sw.ElapsedMilliseconds / 1000.0f;
            var viewProj = Matrix.Multiply(view, proj);
            var wvp = Matrix.RotationY(t * 1) * viewProj;
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
            indices.Dispose();
            layout.Dispose();

            cBuffer.Dispose();
        }
    }
}
