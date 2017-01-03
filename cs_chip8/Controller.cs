namespace Emulator
{
    using System;
    using System.Media;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Processor;

    internal class Controller : Game, IDisposable
    {
        private Chip8 processor;

        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private Texture2D pixel;

        private SoundPlayer soundPlayer = new SoundPlayer();

        private bool disposed = false;

        public Controller()
        {
            this.graphics = new GraphicsDeviceManager(this);
            this.graphics.IsFullScreen = false;
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
            this.pixel.SetData<Color>(new Color[] { Color.Black });

            this.processor = new Chip8();

            this.SetLowResolution();

            this.processor.HighResolutionConfigured += this.Processor_HighResolution;
            this.processor.LowResolutionConfigured += this.Processor_LowResolution;
            this.processor.BeepStarting += this.Processor_BeepStarting;
            this.processor.BeepStopped += this.Processor_BeepStopped;

            this.processor.Initialise();

            ////this.processor.LoadGame(@"GAMES\PONG.ch8");

            ////this.processor.LoadGame(@"SGAMES\ALIEN");
            ////this.processor.LoadGame(@"SGAMES\ANT");
            ////this.processor.LoadGame(@"SGAMES\BLINKY");
            ////this.processor.LoadGame(@"SGAMES\CAR");
            ////this.processor.LoadGame(@"SGAMES\DRAGON1");
            ////this.processor.LoadGame(@"SGAMES\DRAGON2");
            ////this.processor.LoadGame(@"SGAMES\FIELD");
            ////this.processor.LoadGame(@"SGAMES\JOUST23");
            ////this.processor.LoadGame(@"SGAMES\MAZE");
            ////this.processor.LoadGame(@"SGAMES\MINES");
            ////this.processor.LoadGame(@"SGAMES\PIPER");
            ////this.processor.LoadGame(@"SGAMES\RACE");
            this.processor.LoadGame(@"SGAMES\SPACEFIG");
            ////this.processor.LoadGame(@"SGAMES\SQUARE");
            ////this.processor.LoadGame(@"SGAMES\TEST");
            ////this.processor.LoadGame(@"SGAMES\UBOAT");
            ////this.processor.LoadGame(@"SGAMES\WORM3");

            ////this.processor.LoadGame(@"Chip-8 Pack\SuperChip Test Programs\BMP Viewer - Flip-8 logo [Newsdee, 2006].ch8");
            ////this.processor.LoadGame(@"Chip-8 Pack\SuperChip Test Programs\BMP Viewer - Kyori (SC example) [Hap, 2005].ch8");
            ////this.processor.LoadGame(@"Chip-8 Pack\SuperChip Test Programs\BMP Viewer - Let's Chip-8! [Koppepan, 2005].ch8");
            ////this.processor.LoadGame(@"Chip-8 Pack\SuperChip Test Programs\BMP Viewer (16x16 tiles) (MAME) [IQ_132].ch8");
            ////this.processor.LoadGame(@"Chip-8 Pack\SuperChip Test Programs\BMP Viewer (Google) [IQ_132].ch8");
            ////this.processor.LoadGame(@"Chip-8 Pack\SuperChip Test Programs\Emutest [Hap, 2006].ch8");    // XXXX Wrong!!
            ////this.processor.LoadGame(@"Chip-8 Pack\SuperChip Test Programs\Font Test [Newsdee, 2006].ch8");
            ////this.processor.LoadGame(@"Chip-8 Pack\SuperChip Test Programs\Hex Mixt.ch8");
            ////this.processor.LoadGame(@"Chip-8 Pack\SuperChip Test Programs\Line Demo.ch8");
            ////this.processor.LoadGame(@"Chip-8 Pack\SuperChip Test Programs\SC Test.ch8");
            ////this.processor.LoadGame(@"Chip-8 Pack\SuperChip Test Programs\SCHIP Test [iq_132].ch8");
            ////this.processor.LoadGame(@"Chip-8 Pack\SuperChip Test Programs\Scroll Test (modified) [Garstyciuks].ch8");
            ////this.processor.LoadGame(@"Chip-8 Pack\SuperChip Test Programs\Scroll Test.ch8");
            ////this.processor.LoadGame(@"Chip-8 Pack\SuperChip Test Programs\SuperChip Test.ch8");
            ////this.processor.LoadGame(@"Chip-8 Pack\SuperChip Test Programs\Test128.ch8");

        }

        protected override void Update(GameTime gameTime)
        {
            var cyclesPerFrame = 30;
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
            var screenWidth = this.processor.ScreenWidth;
            var screenHeight = this.processor.ScreenHeight;

            var graphics = this.processor.Graphics;

            this.spriteBatch.Begin();
            try
            {
                for (int y = 0; y < screenHeight; y++)
                {
                    var rowOffset = y * screenWidth;
                    var rectanglePositionY = y * pixelSize;
                    for (int x = 0; x < screenWidth; x++)
                    {
                        if (graphics[x + rowOffset])
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
