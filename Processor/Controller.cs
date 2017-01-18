﻿namespace Processor
{
    using System;
    using System.Media;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    public class Controller : Game, IDisposable
    {
        private const Keys ToggleKey = Keys.F12;

        private const int PixelSizeHigh = 5;
        private const int PixelSizeLow = 10;

        private readonly string game;
        private readonly GraphicsDeviceManager graphics;
        private readonly SoundPlayer soundPlayer = new SoundPlayer();

        private readonly MonoGameColourPalette palette;

        private readonly Chip8 processor;

        private SpriteBatch spriteBatch;

        private bool wasToggleKeyPressed;

        private bool disposed = false;

        public Controller(Chip8 processor, string game)
        {
            this.processor = processor;
            this.game = game;
            this.graphics = new GraphicsDeviceManager(this);
            this.graphics.IsFullScreen = false;
            this.palette = new MonoGameColourPalette(this.processor.Display);
        }

        public Chip8 Processor
        {
            get
            {
                return this.processor;
            }
        }

        private int PixelSize
        {
            get
            {
                return this.processor.Display.HighResolution ? PixelSizeHigh : PixelSizeLow;
            }
        }

        public void Stop()
        {
            this.processor.Finished = true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (this.soundPlayer != null)
                    {
                        this.soundPlayer.Dispose();
                    }

                    if (this.graphics != null)
                    {
                        this.graphics.Dispose();
                    }

                    if (this.palette != null)
                    {
                        this.palette.Dispose();
                    }

                    if (this.spriteBatch != null)
                    {
                        this.spriteBatch.Dispose();
                    }
                }

                this.disposed = true;
            }
        }

        protected override void LoadContent()
        {
            this.soundPlayer.SoundLocation = @"..\..\..\Sounds\beep.wav";

            this.spriteBatch = new SpriteBatch(this.GraphicsDevice);

            this.palette.Load(this.GraphicsDevice);

            this.SetLowResolution();

            var schip = this.processor as Schip;
            if (schip != null)
            {
                schip.HighResolutionConfigured += this.Processor_HighResolution;
                schip.LowResolutionConfigured += this.Processor_LowResolution;
            }

            this.processor.BeepStarting += this.Processor_BeepStarting;
            this.processor.BeepStopped += this.Processor_BeepStopped;

            this.processor.Initialise();

            this.processor.LoadGame(this.game);
        }

        protected override void Update(GameTime gameTime)
        {
            this.RunFrame();
            this.processor.UpdateTimers();
            this.CheckFullScreen();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            if (this.processor.DrawNeeded)
            {
                try
                {
                    this.graphics.GraphicsDevice.Clear(this.palette.Colours[0]);
                    this.Draw();
                }
                finally
                {
                    this.processor.DrawNeeded = false;
                }
            }

            base.Draw(gameTime);
        }

        protected virtual void RunFrame()
        {
            for (int i = 0; i < this.processor.RuntimeConfiguration.CyclesPerFrame; ++i)
            {
                if (this.RunCycle())
                {
                    break;
                }

                this.processor.Step();
            }
        }

        protected virtual bool RunCycle()
        {
            if (this.processor.Finished)
            {
                this.Exit();
            }

            return this.processor.Display.LowResolution && this.processor.DrawNeeded;
        }

        private void Draw()
        {
            var pixelSize = this.PixelSize;
            var screenWidth = this.processor.Display.Width;
            var screenHeight = this.processor.Display.Height;

            var source = this.processor.Display.Graphics;
            var numberOfPlanes = this.processor.Display.NumberOfPlanes;

            this.spriteBatch.Begin();
            try
            {
                for (int y = 0; y < screenHeight; y++)
                {
                    var rowOffset = y * screenWidth;
                    var rectanglePositionY = y * pixelSize;
                    for (int x = 0; x < screenWidth; x++)
                    {
                        int colourIndex = 0;
                        for (int plane = 0; plane < numberOfPlanes; ++plane)
                        {
                            var bit = source[plane][x + rowOffset];
                            colourIndex |= Convert.ToByte(bit) << plane;
                        }

                        if (colourIndex != 0)
                        {
                            var colour = this.palette.Colours[colourIndex];
                            var pixel = this.palette.Pixels[colourIndex - 1];
                            var rectanglePositionX = x * pixelSize;
                            this.spriteBatch.Draw(pixel, new Rectangle(rectanglePositionX, rectanglePositionY, pixelSize, pixelSize), colour);
                        }
                    }
                }
            }
            finally
            {
                this.spriteBatch.End();
            }
        }

        private void CheckFullScreen()
        {
            var toggleKeyPressed = Keyboard.GetState().IsKeyDown(ToggleKey);
            if (toggleKeyPressed && !this.wasToggleKeyPressed)
            {
                this.graphics.ToggleFullScreen();
            }

            this.wasToggleKeyPressed = toggleKeyPressed;
        }

        private void Processor_BeepStarting(object sender, EventArgs e)
        {
            this.soundPlayer.PlayLooping();
        }

        private void Processor_BeepStopped(object sender, EventArgs e)
        {
            this.soundPlayer.Stop();
        }

        private void Processor_LowResolution(object sender, System.EventArgs e)
        {
            this.SetLowResolution();
        }

        private void Processor_HighResolution(object sender, System.EventArgs e)
        {
            this.SetHighResolution();
        }

        private void ChangeResolution(int width, int height)
        {
            this.graphics.PreferredBackBufferWidth = this.PixelSize * width;
            this.graphics.PreferredBackBufferHeight = this.PixelSize * height;
            this.graphics.ApplyChanges();
        }

        private void SetLowResolution()
        {
            this.ChangeResolution(BitmappedGraphics.ScreenWidthLow, BitmappedGraphics.ScreenHeightLow);
        }

        private void SetHighResolution()
        {
            this.ChangeResolution(BitmappedGraphics.ScreenWidthHigh, BitmappedGraphics.ScreenHeightHigh);
        }
    }
}
