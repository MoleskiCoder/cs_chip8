// <copyright file="MonoGameColourPalette.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Processor
{
    using System;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    public class MonoGameColourPalette : IDisposable
    {
        private readonly IGraphicsDevice device;
        private bool disposed = false;

        public MonoGameColourPalette(IGraphicsDevice device)
        {
            this.device = device;

            this.Colours = new Color[this.device.NumberOfColours];

            // One less than the number of colours, since we don't bother holding a background pixel.
            this.Pixels = new Texture2D[this.device.NumberOfColours - 1];
        }

        public Color[] Colours { get; }

        public Texture2D[] Pixels { get; }

        public void Load(GraphicsDevice hardware)
        {
            switch (this.device.NumberOfPlanes)
            {
                case 1:
                    this.Colours[0] = Color.Black;
                    this.Colours[1] = Color.White;
                    break;

                case 2:
                    this.Colours[0] = Color.Black;
                    this.Colours[1] = Color.Red;
                    this.Colours[2] = Color.Yellow;
                    this.Colours[3] = Color.White;
                    break;

                default:
                    throw new InvalidOperationException("Undefined number of graphics bit planes in use.");
            }

            for (var i = 1; i < this.device.NumberOfColours; ++i)
            {
                this.Pixels[i - 1] = new Texture2D(hardware, 1, 1);
                this.Pixels[i - 1].SetData<Color>(new Color[] { this.Colours[i] });
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
                    if (this.Pixels != null)
                    {
                        foreach (var pixel in this.Pixels)
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
