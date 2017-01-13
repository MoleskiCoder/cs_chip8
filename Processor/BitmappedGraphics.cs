namespace Processor
{
    using System;
    using System.Linq;

    public class BitmappedGraphics : IGraphicsDevice
    {
        public const int ScreenWidthLow = 64;
        public const int ScreenHeightLow = 32;

        public const int ScreenWidthHigh = 128;
        public const int ScreenHeightHigh = 64;

        private bool[] graphics;

        private bool highResolution = false;

        public bool[] Graphics
        {
            get
            {
                return this.graphics;
            }
        }

        public bool HighResolution
        {
            get
            {
                return this.highResolution;
            }

            set
            {
                this.highResolution = value;
            }
        }

        public bool LowResolution
        {
            get
            {
                return !this.HighResolution;
            }
        }

        public int Width
        {
            get
            {
                return this.HighResolution ? ScreenWidthHigh : ScreenWidthLow;
            }
        }

        public int Height
        {
            get
            {
                return this.HighResolution ? ScreenHeightHigh : ScreenHeightLow;
            }
        }

        public void Initialise()
        {
            this.HighResolution = false;
            this.AllocateMemory();
            this.Clear();
        }

        public int Draw(byte[] memory, int address, int drawX, int drawY, int width, int height)
        {
            if (memory == null)
            {
                throw new ArgumentNullException("memory");
            }

            var screenWidth = this.Width;
            var screenHeight = this.Height;

            var bytesPerRow = width / 8;

            //// https://github.com/Chromatophore/HP48-Superchip#collision-enumeration
            //// An interesting and apparently often unnoticed change to the Super Chip spec is the
            //// following: All drawing is done in XOR mode. If this causes one or more pixels to be
            //// erased, VF is <> 00, other-wise 00. In extended screen mode (aka hires), SCHIP 1.1
            //// will report the number of rows that include a pixel that XORs with the existing data,
            //// so the 'correct' way to detect collisions is Vf <> 0 rather than Vf == 1.
            var rowHits = new int[height];

            for (var row = 0; row < height; ++row)
            {
                var cellY = drawY + row;
                var cellRowOffset = cellY * screenWidth;
                var pixelAddress = address + (row * bytesPerRow);
                for (var column = 0; column < width; ++column)
                {
                    var high = column > 7;
                    var pixelMemory = memory[pixelAddress + (high ? 1 : 0)];
                    var pixel = (pixelMemory & (0x80 >> (column & 0x7))) != 0;
                    if (pixel)
                    {
                        var cellX = drawX + column;
                        if ((cellX < screenWidth) && (cellY < screenHeight))
                        {
                            var cell = cellX + cellRowOffset;
                            if (this.graphics[cell])
                            {
                                rowHits[row]++;
                            }

                            this.graphics[cell] ^= true;
                        }
                        else
                        {
                            //// https://github.com/Chromatophore/HP48-Superchip#collision-with-the-bottom-of-the-screen
                            //// Sprites that are drawn such that they contain data that runs off of the bottom of the
                            //// screen will set Vf based on the number of lines that run off of the screen,
                            //// as if they are colliding.
                            if (cellY >= screenHeight)
                            {
                                rowHits[row]++;
                            }
                        }
                    }
                }
            }

            return (from rowHit in rowHits where rowHit > 0 select rowHit).Count();
        }

        public void AllocateMemory()
        {
            var previous = this.graphics;
            this.graphics = new bool[this.Width * this.Height];

            // https://github.com/Chromatophore/HP48-Superchip#swapping-display-modes
            // Superchip has two different display modes, 64x32 and 128x64. When swapped between,
            // the display buffer is not cleared. Pixels are modified based on being XORed in 1x2 vertical
            // columns, so odd patterns can be created.
            if (previous != null)
            {
                Array.Copy(previous, 0, this.graphics, 0, Math.Min(previous.Length, this.graphics.Length));
            }
        }

        public void ClearRow(int row)
        {
            var width = this.Width;
            Array.Clear(this.graphics, row * width, width);
        }

        public void ClearColumn(int column)
        {
            var width = this.Width;
            var height = this.Height;
            for (int y = 0; y < height; ++y)
            {
                this.graphics[column + (y * width)] = false;
            }
        }

        public void CopyRow(int source, int destination)
        {
            var width = this.Width;
            Array.Copy(this.graphics, source * width, this.graphics, destination * width, width);
        }

        public void CopyColumn(int source, int destination)
        {
            var width = this.Width;
            var height = this.Height;
            for (int y = 0; y < height; ++y)
            {
                this.graphics[destination + (y * width)] = this.graphics[source + (y * width)];
            }
        }

        public void Clear()
        {
            Array.Clear(this.graphics, 0, this.Width * this.Height);
        }
    }
}
