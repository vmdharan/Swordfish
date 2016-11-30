using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace Swordfish
{
    class Triangle
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

        public Triangle(ref Device srcDev, ref DeviceContext srcDevCon)
        {
            dev = srcDev;
            devCon = srcDevCon;
        }

        public void Initialise()
        {
            vertices = Buffer.Create(dev, BindFlags.VertexBuffer, new[]
            {
                new Vector4(0.0f, 0.5f, 0.5f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                new Vector4(0.5f, -0.5f, 0.5f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                new Vector4(-0.5f, -0.5f, 0.5f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f)
            });

            // Vertex and Pixel shaders.
            vsByteCode = ShaderBytecode.CompileFromFile("triShader.hlsl", "VSMain", "vs_5_0", ShaderFlags.None, EffectFlags.None);
            vShader = new VertexShader(dev, vsByteCode);

            psByteCode = ShaderBytecode.CompileFromFile("triShader.hlsl", "PSMain", "ps_5_0", ShaderFlags.None, EffectFlags.None);
            pShader = new PixelShader(dev, psByteCode);

            signature = ShaderSignature.GetInputSignature(vsByteCode);

            // Input layout.
            layout = new InputLayout(dev, signature, new[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 16, 0)
            });
        }

        public void Render()
        {
            devCon.InputAssembler.InputLayout = layout;
            devCon.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            devCon.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertices, 32, 0));

            devCon.VertexShader.Set(vShader);
            devCon.PixelShader.Set(pShader);

            devCon.Draw(3, 0);
        }

        public void Update()
        {

        }

        public void CleanUp()
        {
            vsByteCode.Dispose();
            vShader.Dispose();
            psByteCode.Dispose();
            pShader.Dispose();

            vertices.Dispose();
            layout.Dispose();
        }
    }
}
