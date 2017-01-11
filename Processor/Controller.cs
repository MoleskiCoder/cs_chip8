namespace Processor
{
    using System;
    using System.Media;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    public class Controller : Game, IDisposable
    {
        private const Keys ToggleKey = Keys.F12;

        private const int CyclesPerFrameFast = 22;
        private const int CyclesPerFrameSlow = 13;

        private const int PixelSizeHigh = 5;
        private const int PixelSizeLow = 10;

        private readonly EmulationType machineType;
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

        public Controller(EmulationType machineType, string game)
        {
            this.machineType = machineType;
            this.game = game;
            this.graphics = new GraphicsDeviceManager(this);
            this.graphics.IsFullScreen = false;
        }

        public Chip8 Processor
        {
            get
            {
                return this.processor;
            }
        }

        // https://github.com/Chromatophore/HP48-Superchip#platform-speed
        // The HP48 calculator is much faster than the Cosmac VIP, but,
        // there is still no solid understanding of how much faster it is for
        // most instructions for the purposes of designing compelling programs with
        // Octo. A modified version of cmark77, a Chip-8 graphical benchmark tool
        // written by taqueso on the Something Awful forums was used and
        // yielded scores of 0.80 kOPs in standard/lores and 1.3 kOps in extended/hires.
        // However graphical ops are significantly more costly than other ops on period
        // hardware versus Octo (where they are basically free) and as a result a raw
        // computational cycles/second speed assessment still has not been completed.
        protected int CyclesPerFrame
        {
            get
            {
                // The following gives:
                //  HP-48 running 22 FPS at 1.32 kOps
                //  VIP running 13 FPS at .78 kOps
                return this.machineType == EmulationType.HP48 ? CyclesPerFrameFast : CyclesPerFrameSlow;
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

                    if (this.pixel != null)
                    {
                        this.pixel.Dispose();
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

            this.spriteBatch = new SpriteBatch(GraphicsDevice);

            this.pixel = new Texture2D(GraphicsDevice, 1, 1);
            this.pixel.SetData<Color>(new Color[] { this.foregroundColour });

            this.processor = new Chip8(this.machineType, new MonoGameKeyboard(), new BitmappedGraphics());

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

        protected virtual void RunFrame()
        {
            for (int i = 0; i < this.CyclesPerFrame; ++i)
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
                            this.spriteBatch.Draw(this.pixel, new Rectangle(rectanglePositionX, rectanglePositionY, pixelSize, pixelSize), this.foregroundColour);
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
