using SharpDX.WIC;
using SharpDX.Direct3D11;

namespace Swordfish
{
    class ImageLoader
    {
        private ImagingFactory2 factory;
        private FormatConverter fmtConverter;

        private double alphaThresholdPct;

        public ImageLoader()
        {
            alphaThresholdPct = 0.0;

            factory = new ImagingFactory2();
            fmtConverter = new FormatConverter(factory);
        }

        public Texture2D loadImage(string srcFile, ref SharpDX.Direct3D11.Device dev)
        {
            var imgDecoder = new BitmapDecoder(factory, srcFile, DecodeOptions.CacheOnDemand);

            fmtConverter.Initialize(imgDecoder.GetFrame(0),
                SharpDX.WIC.PixelFormat.Format32bppPRGBA,
                BitmapDitherType.None,
                null, alphaThresholdPct, BitmapPaletteType.Custom);

            var imgSource = (BitmapSource)fmtConverter;

            int bmpW = imgSource.Size.Width;
            int bmpH = imgSource.Size.Height;

            var buffer = new SharpDX.DataStream(bmpW * bmpH * 4, true, true);
            imgSource.CopyPixels(bmpW * 4, buffer);

            // Create a texture description
            Texture2DDescription tex2Ddesc;
            tex2Ddesc = new Texture2DDescription()
            {
                Width = bmpW,
                Height = bmpH,
                ArraySize = 1,
                MipLevels = 1,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Immutable,
                Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm
            };

            // Create a data box
            SharpDX.DataRectangle dr;
            dr = new SharpDX.DataRectangle(buffer.DataPointer, bmpW * 4);

            // Create a texture object
            Texture2D tex;
            tex = new Texture2D(dev, tex2Ddesc, dr);

            return tex;
        }
    }
}
