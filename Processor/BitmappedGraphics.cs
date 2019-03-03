// <copyright file="BitmappedGraphics.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Processor
{
    using System;
    using System.Linq;

    public class BitmappedGraphics : IGraphicsDevice
    {
        public const int DefaultPlane = 0x1;

        public const int ScreenWidthLow = 64;
        public const int ScreenHeightLow = 32;

        public const int ScreenWidthHigh = 128;
        public const int ScreenHeightHigh = 64;

        public BitmappedGraphics(int numberOfPlanes)
        {
            this.NumberOfPlanes = numberOfPlanes;
            this.Graphics = new bool[this.NumberOfPlanes][];
        }

        public int NumberOfPlanes { get; }

        public int NumberOfColours => 1 << this.NumberOfPlanes;

        public bool[][] Graphics { get; }

        public bool HighResolution { get; set; } = false;

        public bool LowResolution => !this.HighResolution;

        public int Width => this.HighResolution ? ScreenWidthHigh : ScreenWidthLow;

        public int Height => this.HighResolution ? ScreenHeightHigh : ScreenHeightLow;

        public int PlaneMask { get; set; } = DefaultPlane;

        public void Initialise()
        {
            this.HighResolution = false;
            this.AllocateMemory();
            this.Clear();
        }

        public void AllocateMemory()
        {
            for (var i = 0; i < this.NumberOfPlanes; ++i)
            {
                this.AllocateMemory(i);
            }
        }

        public int Draw(IMemory memory, int address, int drawX, int drawY, int width, int height)
        {
            var bytesPerRow = width / 8;

            var hits = 0;
            for (var plane = 0; plane < this.NumberOfPlanes; ++plane)
            {
                if (this.IsPlaneSelected(plane))
                {
                    hits += this.Draw(plane, memory, address, drawX, drawY, width, height);
                    address += height * bytesPerRow;
                }
            }

            return hits;
        }

        public void ClearRow(int row)
        {
            for (var plane = 0; plane < this.NumberOfPlanes; ++plane)
            {
                this.MaybeClearRow(plane, row);
            }
        }

        public void ClearColumn(int column)
        {
            for (var plane = 0; plane < this.NumberOfPlanes; ++plane)
            {
                this.MaybeClearColumn(plane, column);
            }
        }

        public void CopyRow(int source, int destination)
        {
            for (var plane = 0; plane < this.NumberOfPlanes; ++plane)
            {
                this.MaybeCopyRow(plane, source, destination);
            }
        }

        public void CopyColumn(int source, int destination)
        {
            for (var plane = 0; plane < this.NumberOfPlanes; ++plane)
            {
                this.MaybeCopyColumn(plane, source, destination);
            }
        }

        public void Clear()
        {
            for (var plane = 0; plane < this.NumberOfPlanes; ++plane)
            {
                this.MaybeClear(plane);
            }
        }

        private bool IsPlaneSelected(int plane)
        {
            var mask = 1 << plane;
            return (this.PlaneMask & mask) != 0;
        }

        private int Draw(int plane, IMemory memory, int address, int drawX, int drawY, int width, int height)
        {
            if (memory == null)
            {
                throw new ArgumentNullException(nameof(memory));
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
                    var pixelMemory = memory.Get(pixelAddress + (high ? 1 : 0));
                    var pixel = (pixelMemory & (0x80 >> (column & 0x7))) != 0;
                    if (pixel)
                    {
                        var cellX = drawX + column;
                        if ((cellX < screenWidth) && (cellY < screenHeight))
                        {
                            var cell = cellX + cellRowOffset;
                            if (this.Graphics[plane][cell])
                            {
                                rowHits[row]++;
                            }

                            this.Graphics[plane][cell] ^= true;
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

        private void AllocateMemory(int plane)
        {
            var previous = this.Graphics[plane];
            this.Graphics[plane] = new bool[this.Width * this.Height];

            // https://github.com/Chromatophore/HP48-Superchip#swapping-display-modes
            // Superchip has two different display modes, 64x32 and 128x64. When swapped between,
            // the display buffer is not cleared. Pixels are modified based on being XORed in 1x2 vertical
            // columns, so odd patterns can be created.
            if (previous != null)
            {
                Array.Copy(previous, 0, this.Graphics[plane], 0, Math.Min(previous.Length, this.Graphics[plane].Length));
            }
        }

        private void MaybeClearRow(int plane, int row)
        {
            if (this.IsPlaneSelected(plane))
            {
                this.ClearRow(plane, row);
            }
        }

        private void ClearRow(int plane, int row)
        {
            var width = this.Width;
            Array.Clear(this.Graphics[plane], row * width, width);
        }

        private void MaybeClearColumn(int plane, int column)
        {
            if (this.IsPlaneSelected(plane))
            {
                this.ClearColumn(plane, column);
            }
        }

        private void ClearColumn(int plane, int column)
        {
            var width = this.Width;
            var height = this.Height;
            for (var y = 0; y < height; ++y)
            {
                this.Graphics[plane][column + (y * width)] = false;
            }
        }

        private void MaybeCopyRow(int plane, int source, int destination)
        {
            if (this.IsPlaneSelected(plane))
            {
                this.CopyRow(plane, source, destination);
            }
        }

        private void CopyRow(int plane, int source, int destination)
        {
            var width = this.Width;
            Array.Copy(this.Graphics[plane], source * width, this.Graphics[plane], destination * width, width);
        }

        private void MaybeCopyColumn(int plane, int source, int destination)
        {
            if (this.IsPlaneSelected(plane))
            {
                this.CopyColumn(plane, source, destination);
            }
        }

        private void CopyColumn(int plane, int source, int destination)
        {
            var width = this.Width;
            var height = this.Height;
            for (var y = 0; y < height; ++y)
            {
                this.Graphics[plane][destination + (y * width)] = this.Graphics[plane][source + (y * width)];
            }
        }

        private void MaybeClear(int plane)
        {
            if (this.IsPlaneSelected(plane))
            {
                this.Clear(plane);
            }
        }

        private void Clear(int plane) => Array.Clear(this.Graphics[plane], 0, this.Width * this.Height);
    }
}
