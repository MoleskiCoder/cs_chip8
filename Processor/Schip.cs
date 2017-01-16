namespace Processor
{
    using System;

    public class Schip : Chip8
    {
        private const int HighFontOffset = 0x110;

        private static byte[] highFont =
        {
            0x7C, 0x82, 0x82, 0x82, 0x82, 0x82, 0x82, 0x82, 0x7C, 0x00, // 0
            0x08, 0x18, 0x38, 0x08, 0x08, 0x08, 0x08, 0x08, 0x3C, 0x00, // 1
            0x7C, 0x82, 0x02, 0x02, 0x04, 0x18, 0x20, 0x40, 0xFE, 0x00, // 2
            0x7C, 0x82, 0x02, 0x02, 0x3C, 0x02, 0x02, 0x82, 0x7C, 0x00, // 3
            0x84, 0x84, 0x84, 0x84, 0xFE, 0x04, 0x04, 0x04, 0x04, 0x00, // 4
            0xFE, 0x80, 0x80, 0x80, 0xFC, 0x02, 0x02, 0x82, 0x7C, 0x00, // 5
            0x7C, 0x82, 0x80, 0x80, 0xFC, 0x82, 0x82, 0x82, 0x7C, 0x00, // 6
            0xFE, 0x02, 0x04, 0x08, 0x10, 0x20, 0x20, 0x20, 0x20, 0x00, // 7
            0x7C, 0x82, 0x82, 0x82, 0x7C, 0x82, 0x82, 0x82, 0x7C, 0x00, // 8
            0x7C, 0x82, 0x82, 0x82, 0x7E, 0x02, 0x02, 0x82, 0x7C, 0x00, // 9
            0x10, 0x28, 0x44, 0x82, 0x82, 0xFE, 0x82, 0x82, 0x82, 0x00, // A
            0xFC, 0x82, 0x82, 0x82, 0xFC, 0x82, 0x82, 0x82, 0xFC, 0x00, // B
            0x7C, 0x82, 0x80, 0x80, 0x80, 0x80, 0x80, 0x82, 0x7C, 0x00, // C
            0xFC, 0x82, 0x82, 0x82, 0x82, 0x82, 0x82, 0x82, 0xFC, 0x00, // D
            0xFE, 0x80, 0x80, 0x80, 0xF8, 0x80, 0x80, 0x80, 0xFE, 0x00, // E
            0xFE, 0x80, 0x80, 0x80, 0xF8, 0x80, 0x80, 0x80, 0x80, 0x00, // F
        };

        private readonly byte[] r = new byte[8]; // HP48 flags

        private bool compatibility = false;

        public Schip(IMemory memory, IKeyboardDevice keyboard, IGraphicsDevice display)
        : base(memory, keyboard, display)
        {
        }

        public event EventHandler<EventArgs> HighResolutionConfigured;

        public event EventHandler<EventArgs> LowResolutionConfigured;

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
        public override int CyclesPerFrame
        {
            get
            {
                // Running 22 FPS at 1.32 kOps
                return 22;
            }
        }

        public byte[] R
        {
            get
            {
                return this.r;
            }
        }

        protected bool Compatibility
        {
            get
            {
                return this.compatibility;
            }

            set
            {
                this.compatibility = value;
            }
        }

        public override void Initialise()
        {
            base.Initialise();
            Array.Copy(highFont, 0, this.Memory.Bus, HighFontOffset, highFont.Length);
        }

        protected void OnHighResolution()
        {
            this.Display.HighResolution = true;
            this.Display.AllocateMemory();

            var handler = this.HighResolutionConfigured;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected void OnLowResolution()
        {
            this.Display.HighResolution = false;
            this.Display.AllocateMemory();

            var handler = this.LowResolutionConfigured;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected override bool EmulateInstructions_F(int nnn, byte nn, int n, int x, int y)
        {
            switch (nn)
            {
                case 0x30:
                    this.LD_HF_Vx(x);
                    break;

                case 0x75:
                    this.LD_R_Vx(x);
                    break;

                case 0x85:
                    this.LD_Vx_R(x);
                    break;

                default:
                    return base.EmulateInstructions_F(nnn, nn, n, x, y);
            }

            return true;
        }

        protected override bool EmulateInstructions_D(int nnn, byte nn, int n, int x, int y)
        {
            this.UsedX = this.UsedY = true;
            switch (n)
            {
                case 0:
                    this.XDRW(x, y);
                    break;

                default:
                    return base.EmulateInstructions_D(nnn, nn, n, x, y);
            }

            return true;
        }

        protected override bool EmulateInstructions_0(int nnn, byte nn, int n, int x, int y)
        {
            switch (nn)
            {
                case 0xfa:
                    this.COMPATIBILITY();
                    break;

                case 0xfb:
                    this.SCRIGHT();
                    break;

                case 0xfc:
                    this.SCLEFT();
                    break;

                case 0xfd:
                    this.EXIT();
                    break;

                case 0xfe:
                    this.LOW();
                    break;

                case 0xff:
                    this.HIGH();
                    break;

                default:
                    switch (y)
                    {
                        case 0xc:
                            this.SCDOWN(n);
                            break;

                        default:
                            return base.EmulateInstructions_0(nnn, nn, n, x, y);
                    }

                    break;
            }

            return true;
        }

        // https://github.com/Chromatophore/HP48-Superchip#8xy6--8xye
        // Bit shifts X register by 1, VIP: shifts Y by one and places in X, HP48-SC: ignores Y field, shifts X
        protected override void SHR(int x, int y)
        {
            this.MnemomicFormat = "SHR\tV{0:X1}";
            this.UsedY = false;
            this.V[x] >>= 1;
            this.V[0xf] = (byte)(this.V[x] & 0x1);
        }

        // https://github.com/Chromatophore/HP48-Superchip#8xy6--8xye
        // Bit shifts X register by 1, VIP: shifts Y by one and places in X, HP48-SC: ignores Y field, shifts X
        protected override void SHL(int x, int y)
        {
            this.MnemomicFormat = "SHL\tV{0:X1}";
            this.UsedY = false;
            this.V[0xf] = (byte)((this.V[x] & 0x80) == 0 ? 0 : 1);
            this.V[x] <<= 1;
        }

        // https://github.com/Chromatophore/HP48-Superchip#bnnn
        // Sets PC to address NNN + v0 -
        //  VIP: correctly jumps based on v0
        //  HP48 -SC: reads highest nibble of address to select
        //      register to apply to address (high nibble pulls double duty)
        protected override void JP_V0(int x, int nnn)
        {
            this.MnemomicFormat = "JP\t[V0],#{0:X3}";
            this.PC = (ushort)(this.V[x] + nnn);
        }

        // https://github.com/Chromatophore/HP48-Superchip#fx55--fx65
        // Saves/Loads registers up to X at I pointer - VIP: increases I, HP48-SC: I remains static
        protected override void LD_Vx_II(int x)
        {
            if (this.Compatibility)
            {
                base.LD_Vx_II(x);
            }
            else
            {
                this.MnemomicFormat = "LD\tV{0:X1},[I]";
                Array.Copy(this.Memory.Bus, this.I, this.V, 0, x + 1);
            }
        }

        // https://github.com/Chromatophore/HP48-Superchip#fx55--fx65
        // Saves/Loads registers up to X at I pointer - VIP: increases I, HP48-SC: I remains static
        protected override void LD_II_Vx(int x)
        {
            if (this.Compatibility)
            {
                base.LD_II_Vx(x);
            }
            else
            {
                this.MnemomicFormat = "LD\t[I],V{0:X1}";
                Array.Copy(this.V, 0, this.Memory.Bus, this.I, x + 1);
            }
        }

        private void LD_HF_Vx(int x)
        {
            this.MnemomicFormat = "LD\tHF,V{0:X1}";
            this.I = (ushort)(HighFontOffset + (10 * this.V[x]));
        }

        private void XDRW(int x, int y)
        {
            this.MnemomicFormat = "XDRW V{0:X1},V{1:X1}";
            this.Draw(x, y, 16, 16);
        }

        // exit
        // This opcode is used to terminate the chip8run program. It causes the chip8run program to exit
        // with a successful exit status. [Super-Chip]
        // Code generated: 0x00FD.
        private void EXIT()
        {
            this.MnemomicFormat = "EXIT";
            this.Finished = true;
        }

        // scdown n
        // Scroll the screen down n pixels. [Super-Chip]
        // This opcode delays until the start of a 60Hz clock cycle before drawing in low resolution mode.
        // (Use the delay timer to pace your games in high resolution mode.)
        // Code generated: 0x00Cn
        private void SCDOWN(int n)
        {
            this.MnemomicFormat = "SCDOWN\t{0:X1}";
            this.UsedN = true;

            var screenHeight = this.Display.Height;

            // Copy rows bottom to top
            for (int y = screenHeight - n - 1; y >= 0; --y)
            {
                this.Display.CopyRow(y, y + n);
            }

            // Remove the top columns, blanked by the scroll effect
            for (int y = 0; y < n; ++y)
            {
                this.Display.ClearRow(y);
            }

            this.DrawNeeded = true;
        }

        // compatibility
        // Mangle the "save" and "restore" opcodes to leave the I register unchanged.
        // Warning: This opcode is not a standard Chip 8 opcode. It is provided soley to allow testing and
        // porting of Chip 8 games which rely on this behaviour.
        // Code generated: 0x00FA
        private void COMPATIBILITY()
        {
            this.MnemomicFormat = "COMPATIBILITY";
            this.Compatibility = true;
        }

        // scright
        // Scroll the screen right 4 pixels. [Super-Chip]
        // This opcode delays until the start of a 60Hz clock cycle before drawing in low resolution mode.
        // (Use the delay timer to pace your games in high resolution mode.)
        // Code generated: 0x00FB
        private void SCRIGHT()
        {
            this.MnemomicFormat = "SCRIGHT";

            var screenWidth = this.Display.Width;

            // Scroll distance
            var n = 4;

            // Copy colummns from right to left
            for (int x = screenWidth - n - 1; x >= 0; --x)
            {
                this.Display.CopyColumn(x, x + n);
            }

            // Remove the leftmost columns, blanked by the scroll effect
            for (int x = 0; x < n; ++x)
            {
                this.Display.ClearColumn(x);
            }

            this.DrawNeeded = true;
        }

        // scleft
        // Scroll the screen left 4 pixels. [Super-Chip]
        // This opcode delays until the start of a 60Hz clock cycle before drawing in low resolution mode.
        // (Use the delay timer to pace your games in high resolution mode.)
        // Code generated: 0x00FC
        private void SCLEFT()
        {
            this.MnemomicFormat = "SCLEFT";

            var screenWidth = this.Display.Width;

            // Scroll distance
            var n = 4;

            // Copy columns from left to right
            for (int x = 0; x < (screenWidth - n); ++x)
            {
                this.Display.CopyColumn(x + n, x);
            }

            // Remove the rightmost columns, blanked by the scroll effect
            for (int x = 0; x < n; ++x)
            {
                this.Display.ClearColumn(screenWidth - x - 1);
            }

            this.DrawNeeded = true;
        }

        // low
        // Low resolution (64×32) graphics mode (this is the default). [Super-Chip]
        // Code generated: 0x00FE
        private void LOW()
        {
            this.MnemomicFormat = "LOW";
            this.OnLowResolution();
        }

        // high
        // High resolution (128×64) graphics mode. [Super-Chip]
        // Code generated: 0x00FF
        private void HIGH()
        {
            this.MnemomicFormat = "HIGH";
            this.OnHighResolution();
        }

        // flags.save vX
        // Store the values of registers v0 to vX into the "flags" registers (this means something in the
        // HP48 implementation). (X < 8) [Super-Chip]
        // Code generated: 0xFX75
        private void LD_R_Vx(int x)
        {
            this.MnemomicFormat = "LD\tR,V{0:X1}";
            Array.Copy(this.V, this.R, x + 1);
        }

        // flags.restore vX
        // Read the values of registers v0 to vX from the "flags" registers (this means something in the
        // HP48 implementation). (X < 8) [Super-Chip]
        // Code generated: 0xFX85
        private void LD_Vx_R(int x)
        {
            this.MnemomicFormat = "LD\tV{0:X1},R";
            Array.Copy(this.R, this.V, x + 1);
        }
    }
}
