namespace Emulator
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Processor;

    internal class Controller : Game
    {
        private Chip8 processor;

        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private Texture2D pixel;

        public Controller()
        {
            this.graphics = new GraphicsDeviceManager(this);
            this.graphics.IsFullScreen = false;
        }

        protected override void LoadContent()
        {
            this.spriteBatch = new SpriteBatch(GraphicsDevice);

            this.pixel = new Texture2D(GraphicsDevice, 1, 1);
            this.pixel.SetData<Color>(new Color[] { Color.Black });

            this.processor = new Chip8();

            this.SetLowResolution();

            this.processor.HighResolutionConfigured += this.Processor_HighResolution;
            this.processor.LowResolutionConfigured += this.Processor_LowResolution;

            this.processor.Initialise();
            this.processor.LoadGame("PONG");
        }

        protected override void Update(GameTime gameTime)
        {
            var cyclesPerFrame = 20;
            for (int i = 0; i < cyclesPerFrame; ++i)
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
            base.Update(gameTime);
        }
            
        protected override void Draw(GameTime gameTime)
        {
            if (this.processor.DrawNeeded)
            {
                try
                {
                    this.graphics.GraphicsDevice.Clear(Color.CornflowerBlue);
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
            var pixelSize = this.processor.PixelSize;
            this.spriteBatch.Begin();
            try
            {
                for (int x = 0; x < this.processor.ScreenWidth; x++)
                {
                    for (int y = 0; y < this.processor.ScreenHeight; y++)
                    {
                        if (this.processor.Graphics[x, y])
                        {
                            this.spriteBatch.Draw(this.pixel, new Rectangle(x * pixelSize, y * pixelSize, pixelSize, pixelSize), Color.White);
                        }
                    }
                }
            }
            finally
            {
                this.spriteBatch.End();
            }
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
            this.graphics.PreferredBackBufferWidth = this.processor.PixelSize * width;
            this.graphics.PreferredBackBufferHeight = this.processor.PixelSize * height;
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
