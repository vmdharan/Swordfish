using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.Windows;
using System.Diagnostics;
using Device = SharpDX.Direct3D11.Device;

namespace Swordfish
{
    class GameEngine
    {
        // Configurable variables.
        private int SF_RenderWidth = 800;
        private int SF_RenderHeight = 600;
        private string SF_RenderWindowText = "Swordfish Engine";

        // Core pipeline variables.
        private int fpsCount;
        private Stopwatch fpsTimer;

        private RenderForm form;
        private SwapChainDescription swapChainDesc;

        private Device dev;
        private DeviceContext devCon;
        private SwapChain swapChain;

        private Factory factory;

        private RenderTargetView renderView;
        private Texture2D backBuffer;

        private Texture2D depthBuffer;
        private DepthStencilView depthStencilView;

        private DepthStencilState depthStencilState1;
        private DepthStencilState depthStencilState2;

        private RasterizerState rasterizerState;

        private BlendState blendState1;
        private BlendState blendState2;

        // Components
        private Triangle tri;
        
        // Constructor
        // This is the outer level for the game engine. The basic structure is as follows:
        // Step 1 - Initialise the DX pipeline followed by all the components.
        // Step 2 - Execute the rendering loop, which in turn calls the per-frame update.
        // Step 3 - Clean up the components and the DX pipeline for a graceful program exit.
        public GameEngine()
        {
            // FPS
            fpsCount = 0;
            fpsTimer = new Stopwatch();
            fpsTimer.Start();

            // Initialise pipeline
            InitialiseDX();

            // Initialise components
            InitialiseComponents();

            // Render loop
            RenderDX();

            // Clean up components
            CleanUpComponents();

            // Clean up 
            CleanUpDX();
        }

        // Initialise the DX pipeline.
        public void InitialiseDX()
        {
            // Create a new rendering form.
            form = new RenderForm(SF_RenderWindowText);
            form.Width = SF_RenderWidth;
            form.Height = SF_RenderHeight;
            form.AllowUserResizing = false;

            // Create the swapchain description.
            swapChainDesc = new SwapChainDescription()
            {
                BufferCount = 1,
                ModeDescription = new ModeDescription(
                    form.ClientSize.Width,
                    form.ClientSize.Height,
                    new Rational(60, 1),
                    Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = form.Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            // Create the device and swapchain.
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None,
                swapChainDesc, out dev, out swapChain);
            devCon = dev.ImmediateContext;

            // Ignore all windows events.
            factory = swapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);

            // Render Target View.
            backBuffer = SharpDX.Direct3D11.Resource.FromSwapChain<Texture2D>(swapChain, 0);
            renderView = new RenderTargetView(dev, backBuffer);

            // Depth buffer
            depthBuffer = new Texture2D(dev, new Texture2DDescription()
            {
                Format = Format.D32_Float_S8X24_UInt,
                ArraySize = 1,
                MipLevels = 1,
                Width = form.ClientSize.Width,
                Height = form.ClientSize.Height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });

            // Depth buffer view
            depthStencilView = new DepthStencilView(dev, depthBuffer);

            // Depth stencil states

            // Depth disabled
            var dSS = new DepthStencilStateDescription();
            dSS.IsStencilEnabled = false;
            dSS.StencilReadMask = 0xFF;
            dSS.StencilWriteMask = 0xFF;
            dSS.FrontFace.FailOperation = StencilOperation.Keep;
            dSS.FrontFace.DepthFailOperation = StencilOperation.Increment;
            dSS.FrontFace.PassOperation = StencilOperation.Keep;
            dSS.FrontFace.Comparison = Comparison.Always;
            dSS.BackFace.FailOperation = StencilOperation.Keep;
            dSS.BackFace.DepthFailOperation = StencilOperation.Decrement;
            dSS.BackFace.PassOperation = StencilOperation.Keep;
            dSS.BackFace.Comparison = Comparison.Always;

            depthStencilState1 = new DepthStencilState(dev, dSS);
            //devCon.OutputMerger.SetDepthStencilState(depthStencilState1, 1);

            // Depth enabled
            var dSS2 = new DepthStencilStateDescription();
            dSS2.IsStencilEnabled = true;
            dSS2.StencilReadMask = 0xFF;
            dSS2.StencilWriteMask = 0xFF;
            dSS2.FrontFace.FailOperation = StencilOperation.Keep;
            dSS2.FrontFace.DepthFailOperation = StencilOperation.Increment;
            dSS2.FrontFace.PassOperation = StencilOperation.Keep;
            dSS2.FrontFace.Comparison = Comparison.Always;
            dSS2.BackFace.FailOperation = StencilOperation.Keep;
            dSS2.BackFace.DepthFailOperation = StencilOperation.Decrement;
            dSS2.BackFace.PassOperation = StencilOperation.Keep;
            dSS2.BackFace.Comparison = Comparison.Always;

            depthStencilState1 = new DepthStencilState(dev, dSS2);
            //devCon.OutputMerger.SetDepthStencilState(depthStencilState2, 2);


            // Create the rasterizer state.
            rasterizerState = new RasterizerState(dev, new RasterizerStateDescription()
            {
                CullMode = CullMode.Back,
                DepthBias = 0,
                DepthBiasClamp = 0,
                FillMode = FillMode.Solid,
                SlopeScaledDepthBias = 0,
                IsAntialiasedLineEnabled = false,
                IsDepthClipEnabled = true,
                IsFrontCounterClockwise = false,
                IsMultisampleEnabled = false,
                IsScissorEnabled = false
            });

            // Prepare all stages.
            devCon.Rasterizer.State = rasterizerState;
            devCon.Rasterizer.SetViewport(new Viewport(0, 0, form.ClientSize.Width,
                form.ClientSize.Height, 0.0f, 1.0f));
            devCon.OutputMerger.SetTargets(depthStencilView, renderView);

            // Blend state description
            var BlendDesc1 = new BlendStateDescription();

            BlendDesc1.RenderTarget[0].IsBlendEnabled = true;
            BlendDesc1.RenderTarget[0].BlendOperation = BlendOperation.Add;
            BlendDesc1.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
            BlendDesc1.RenderTarget[0].DestinationBlend = BlendOption.One;
            BlendDesc1.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            BlendDesc1.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
            BlendDesc1.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
            BlendDesc1.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
            BlendDesc1.AlphaToCoverageEnable = false;
            BlendDesc1.IndependentBlendEnable = false;

            var BlendDesc2 = new BlendStateDescription();

            BlendDesc2.RenderTarget[0].IsBlendEnabled = false;
            BlendDesc2.RenderTarget[0].BlendOperation = BlendOperation.Add;
            BlendDesc2.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
            BlendDesc2.RenderTarget[0].DestinationBlend = BlendOption.One;
            BlendDesc2.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            BlendDesc2.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
            BlendDesc2.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
            BlendDesc2.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
            BlendDesc2.AlphaToCoverageEnable = false;
            BlendDesc2.IndependentBlendEnable = false;

            //RawColor4 blendFactor = new RawColor4(0.0f, 0.0f, 0.0f, 0.0f);

            blendState1 = new BlendState(dev, BlendDesc1);
            //devCon.OutputMerger.SetBlendState(blendState1, blendFactor, 0xFFFFFFFF);

            blendState2 = new BlendState(dev, BlendDesc2);
            //devCon.OutputMerger.SetBlendState(blendState2, blendFactor, 0xFFFFFFFF);

        }

        // Initialise components
        public void InitialiseComponents()
        {
            tri = new Triangle(ref dev, ref devCon);
            tri.Initialise();
        }

        // Render DX
        public void RenderDX()
        {
            // Main rendering loop
            RenderLoop.Run(form, () =>
            {
                devCon.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
                devCon.ClearRenderTargetView(renderView, new Color4(0.0f, 0.0675f, 0.1325f, 1.0f));
                RenderComponents();

                swapChain.Present(0, PresentFlags.None);

                UpdateDX();
                UpdateComponents();
            });
        }

        // Render components
        public void RenderComponents()
        {
            tri.Render();
        }

        // Update DX
        public void UpdateDX()
        {
            fpsCount++;
            if (fpsTimer.ElapsedMilliseconds > 1000)
            {
                form.Text = string.Format(SF_RenderWindowText + " - FPS: {0:F2}", 
                    1000.0 * fpsCount / (float)fpsTimer.ElapsedMilliseconds);
                fpsCount = 0;
                fpsTimer.Reset();
                fpsTimer.Stop();
                fpsTimer.Start();
            }
        }

        // Update components
        public void UpdateComponents()
        {
            tri.Update();
        }

        // Clean up components
        public void CleanUpComponents()
        {

        }

        // Clean up DX
        public void CleanUpDX()
        {
            renderView.Dispose();
            backBuffer.Dispose();
            devCon.ClearState();
            devCon.Flush();
            dev.Dispose();
            devCon.Dispose();
            swapChain.Dispose();
            factory.Dispose();
            form.Dispose();
        }
    }
}
