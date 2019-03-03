// <copyright file="Controller.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Processor
{
    using System;
    using System.Media;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    public class Controller : Game
    {
        private const Keys ToggleKey = Keys.F12;

        private const int PixelSizeHigh = 5;
        private const int PixelSizeLow = 10;

        private readonly string game;
        private readonly GraphicsDeviceManager graphics;
        private readonly SoundPlayer soundPlayer = new SoundPlayer();

        private readonly MonoGameColourPalette palette;
        private SpriteBatch spriteBatch;

        private bool wasToggleKeyPressed;

        private bool disposed = false;

        public Controller(Chip8 processor, string game)
        {
            this.Processor = processor;
            this.game = game;
            this.graphics = new GraphicsDeviceManager(this)
            {
                IsFullScreen = false,
            };
            this.palette = new MonoGameColourPalette(this.Processor.Display);
        }

        public Chip8 Processor { get; }

        private int PixelSize => this.Processor.Display.HighResolution ? PixelSizeHigh : PixelSizeLow;

        public void Stop() => this.Processor.Finished = true;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.soundPlayer?.Dispose();
                    this.graphics?.Dispose();
                    this.palette?.Dispose();
                    this.spriteBatch?.Dispose();
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

            if (this.Processor is Schip schip)
            {
                schip.HighResolutionConfigured += this.Processor_HighResolution;
                schip.LowResolutionConfigured += this.Processor_LowResolution;
            }

            this.Processor.BeepStarting += this.Processor_BeepStarting;
            this.Processor.BeepStopped += this.Processor_BeepStopped;

            this.Processor.Initialise();

            this.Processor.LoadGame(this.game);
        }

        protected override void Update(GameTime gameTime)
        {
            this.RunFrame();
            this.Processor.UpdateTimers();
            this.CheckFullScreen();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            if (this.Processor.DrawNeeded)
            {
                try
                {
                    this.graphics.GraphicsDevice.Clear(this.palette.Colours[0]);
                    this.Draw();
                }
                finally
                {
                    this.Processor.DrawNeeded = false;
                }
            }

            base.Draw(gameTime);
        }

        protected virtual void RunFrame()
        {
            for (var i = 0; i < this.Processor.RuntimeConfiguration.CyclesPerFrame; ++i)
            {
                if (this.RunCycle())
                {
                    break;
                }

                this.Processor.Step();
            }
        }

        protected virtual bool RunCycle()
        {
            if (this.Processor.Finished)
            {
                this.Exit();
            }

            return this.Processor.Display.LowResolution && this.Processor.DrawNeeded;
        }

        private void Draw()
        {
            var pixelSize = this.PixelSize;
            var screenWidth = this.Processor.Display.Width;
            var screenHeight = this.Processor.Display.Height;

            var source = this.Processor.Display.Graphics;
            var numberOfPlanes = this.Processor.Display.NumberOfPlanes;

            this.spriteBatch.Begin();
            try
            {
                for (var y = 0; y < screenHeight; y++)
                {
                    var rowOffset = y * screenWidth;
                    var rectanglePositionY = y * pixelSize;
                    for (var x = 0; x < screenWidth; x++)
                    {
                        var colourIndex = 0;
                        for (var plane = 0; plane < numberOfPlanes; ++plane)
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

        private void Processor_BeepStarting(object sender, EventArgs e) => this.soundPlayer.PlayLooping();

        private void Processor_BeepStopped(object sender, EventArgs e) => this.soundPlayer.Stop();

        private void Processor_LowResolution(object sender, System.EventArgs e) => this.SetLowResolution();

        private void Processor_HighResolution(object sender, System.EventArgs e) => this.SetHighResolution();

        private void ChangeResolution(int width, int height)
        {
            this.graphics.PreferredBackBufferWidth = this.PixelSize * width;
            this.graphics.PreferredBackBufferHeight = this.PixelSize * height;
            this.graphics.ApplyChanges();
        }

        private void SetLowResolution() => this.ChangeResolution(BitmappedGraphics.ScreenWidthLow, BitmappedGraphics.ScreenHeightLow);

        private void SetHighResolution() => this.ChangeResolution(BitmappedGraphics.ScreenWidthHigh, BitmappedGraphics.ScreenHeightHigh);
    }
}
