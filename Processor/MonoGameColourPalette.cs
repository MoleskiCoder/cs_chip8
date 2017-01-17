namespace Processor
{
    using System;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    public class MonoGameColourPalette : IDisposable
    {
        private readonly IGraphicsDevice device;
        private readonly Color[] colours;
        private readonly Texture2D[] pixels;

        private bool disposed = false;

        public MonoGameColourPalette(IGraphicsDevice device)
        {
            this.device = device;

            this.colours = new Color[this.device.NumberOfColours];

            // One less than the number of colours, since we don't bother holding a background pixel.
            this.pixels = new Texture2D[this.device.NumberOfColours - 1];
        }

        public Color[] Colours
        {
            get
            {
                return this.colours;
            }
        }

        public Texture2D[] Pixels
        {
            get
            {
                return this.pixels;
            }
        }

        public void Load(GraphicsDevice hardware)
        {
            switch (this.device.NumberOfPlanes)
            {
                case 1:
                    this.colours[0] = Color.Black;
                    this.colours[1] = Color.White;
                    break;

                case 2:
                    this.colours[0] = Color.Black;
                    this.colours[1] = Color.Red;
                    this.colours[2] = Color.Yellow;
                    this.colours[3] = Color.White;
                    break;

                default:
                    throw new InvalidOperationException("Undefined number of graphics bit planes in use.");
            }

            for (int i = 1; i < this.device.NumberOfColours; ++i)
            {
                this.pixels[i - 1] = new Texture2D(hardware, 1, 1);
                this.pixels[i - 1].SetData<Color>(new Color[] { this.colours[i] });
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (this.pixels != null)
                    {
                        foreach (var pixel in this.pixels)
                        {
                            pixel.Dispose();
                        }
                    }
                }

                this.disposed = true;
            }
        }
    }
}
