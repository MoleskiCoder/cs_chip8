namespace Emulator
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Processor;

    internal class Controller : Game
    {
        private static readonly int PixelSize = 10;

        private Chip8 processor;

        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private Texture2D pixel;

        public Controller()
        {
            this.graphics = new GraphicsDeviceManager(this);
            this.graphics.IsFullScreen = false;
            this.graphics.PreferredBackBufferWidth = PixelSize * Chip8.ScreenWidth;
            this.graphics.PreferredBackBufferHeight = PixelSize * Chip8.ScreenHeight;
            this.graphics.ApplyChanges();
        }

        protected override void LoadContent()
        {
            this.spriteBatch = new SpriteBatch(GraphicsDevice);

            this.pixel = new Texture2D(GraphicsDevice, 1, 1);
            this.pixel.SetData<Color>(new Color[] { Color.Black });

            this.processor = new Chip8();

            this.processor.Initialise();
            this.processor.LoadGame("PONG");
        }

        protected override void Update(GameTime gameTime)
        {
            this.processor.Step();
            this.processor.Step();
            this.processor.Step();
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
            this.spriteBatch.Begin();
            try
            {
                for (int x = 0; x < Chip8.ScreenWidth; x++)
                {
                    for (int y = 0; y < Chip8.ScreenHeight; y++)
                    {
                        if (this.processor.Graphics[x, y])
                        {
                            this.spriteBatch.Draw(this.pixel, new Rectangle(x * PixelSize, y * PixelSize, PixelSize, PixelSize), Color.White);
                        }
                    }
                }
            }
            finally
            {
                this.spriteBatch.End();
            }
        }
    }
}
