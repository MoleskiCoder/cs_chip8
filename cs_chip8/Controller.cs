namespace Emulator
{
    using System;
    using System.Media;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    using Processor;

    internal class Controller : Game, IDisposable
    {
        private const Keys ToggleKey = Keys.F12;
        private const int CyclesPerFrame = 30;

        private readonly string game;
        private readonly GraphicsDeviceManager graphics;
        private readonly SoundPlayer soundPlayer = new SoundPlayer();

        private readonly Color backgroundColour = Color.Black;
        private readonly Color foregroundColour = Color.White;

        private Chip8 processor;

        private SpriteBatch spriteBatch;
        private Texture2D pixel;

        private bool wasToggleKeyPressed;

        private bool disposed = false;

        public Controller(string game)
        {
            this.game = game;
            this.graphics = new GraphicsDeviceManager(this);
            this.graphics.IsFullScreen = false;
        }

        private int PixelSize
        {
            get
            {
                return this.processor.HighResolution ? 5 : 10;
            }
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

                    if (this.soundPlayer != null)
                    {
                        this.graphics.Dispose();
                    }

                    if (this.soundPlayer != null)
                    {
                        this.pixel.Dispose();
                    }

                    if (this.soundPlayer != null)
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

            this.spriteBatch = new SpriteBatch(GraphicsDevice);

            this.pixel = new Texture2D(GraphicsDevice, 1, 1);
            this.pixel.SetData<Color>(new Color[] { this.foregroundColour });

            this.processor = new Chip8();

            this.SetLowResolution();

            this.processor.HighResolutionConfigured += this.Processor_HighResolution;
            this.processor.LowResolutionConfigured += this.Processor_LowResolution;
            this.processor.BeepStarting += this.Processor_BeepStarting;
            this.processor.BeepStopped += this.Processor_BeepStopped;

            this.processor.Initialise();

            this.processor.LoadGame(this.game);
        }

        protected override void Update(GameTime gameTime)
        {
            for (int i = 0; i < CyclesPerFrame; ++i)
            {
                if (this.processor.Finished)
                {
                    this.Exit();
                }

                if (this.processor.LowResolution && this.processor.DrawNeeded)
                {
                    break;
                }

                this.processor.Step();
            }

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
                    this.graphics.GraphicsDevice.Clear(this.backgroundColour);
                    this.Draw();
                }
                finally
                {
                    this.processor.DrawNeeded = false;
                }
            }

            base.Draw(gameTime);
        }

        private void Draw()
        {
            var pixelSize = this.PixelSize;
            var screenWidth = this.processor.ScreenWidth;
            var screenHeight = this.processor.ScreenHeight;

            var source = this.processor.Graphics;

            this.spriteBatch.Begin();
            try
            {
                for (int y = 0; y < screenHeight; y++)
                {
                    var rowOffset = y * screenWidth;
                    var rectanglePositionY = y * pixelSize;
                    for (int x = 0; x < screenWidth; x++)
                    {
                        if (source[x + rowOffset])
                        {
                            var rectanglePositionX = x * pixelSize;
                            this.spriteBatch.Draw(this.pixel, new Rectangle(rectanglePositionX, rectanglePositionY, pixelSize, pixelSize), Color.White);
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
            this.ChangeResolution(Chip8.ScreenWidthLow, Chip8.ScreenHeightLow);
        }

        private void SetHighResolution()
        {
            this.ChangeResolution(Chip8.ScreenWidthHigh, Chip8.ScreenHeightHigh);
        }
    }
}
