namespace Processor
{
    using System;
    using System.Globalization;

    public class XoChip : Schip
    {
        private readonly byte[] audoPatternBuffer = new byte[16];

        private int nnnn;

        public XoChip(IMemory memory, IKeyboardDevice keyboard, IGraphicsDevice display, bool allowMisalignedOpcodes)
        : base(memory, keyboard, display, allowMisalignedOpcodes)
        {
        }

        protected override bool EmulateInstructions_0(int nnn, int nn, int n, int x, int y)
        {
            switch (y)
            {
                case 0xd:
                    this.UsedN = true;
                    this.SCUP(n);
                    break;

                default:
                    return base.EmulateInstructions_0(nnn, nn, n, x, y);
            }

            return true;
        }

        protected override bool EmulateInstructions_5(int nnn, int nn, int n, int x, int y)
        {
            this.UsedX = this.UsedY = true;
            switch (n)
            {
                case 2:
                    this.Save_vx_to_vy(x, y);
                    break;

                case 3:
                    this.Load_vx_to_vy(x, y);
                    break;

                default:
                    this.UsedX = this.UsedY = false;
                    return base.EmulateInstructions_5(nnn, nn, n, x, y);
            }

            return true;
        }

        protected override bool EmulateInstructions_F(int nnn, int nn, int n, int x, int y)
        {
            switch (nnn)
            {
                case 0:
                    this.Load_i_long();
                    break;

                case 0x002:
                    this.Audio();
                    break;

                default:
                    switch (nn)
                    {
                        case 0x01:
                            this.UsedX = true;
                            this.Plane(x);
                            break;

                        default:
                            return base.EmulateInstructions_F(nnn, nn, n, x, y);
                    }

                    break;
            }

            return true;
        }

        protected override void OnDisassembleInstruction(ushort programCounter, ushort instruction, int address, int operand, int n, int x, int y)
        {
            switch (instruction)
            {
                case 0xf000:
                    {
                        var pre = string.Format(CultureInfo.InvariantCulture, "PC={0:x4}\t{1:x4}\t", programCounter, instruction);
                        var post = string.Format(CultureInfo.InvariantCulture, "LD I,#{0:x4}L", this.nnnn);
                        this.OnDisassembleInstruction(pre + post);
                    }

                    break;

                default:
                    base.OnDisassembleInstruction(programCounter, instruction, address, operand, n, x, y);
                    break;
            }
        }

        //// scroll-up n (0x00DN) scroll the contents of the display up by 0-15 pixels.
        private void SCUP(int n)
        {
            this.MnemomicFormat = "SCUP\t{0:X1}";
            this.UsedN = true;

            var screenHeight = this.Display.Height;

            // Copy rows from top to bottom
            for (int y = 0; y < (screenHeight - n); ++y)
            {
                this.Display.CopyRow(y + n, y);
            }

            // Remove the bottommost rows, blanked by the scroll effect
            for (int y = 0; y < n; ++y)
            {
                this.Display.ClearRow(screenHeight - y - 1);
            }

            this.DrawNeeded = true;
        }

        // save vx - vy (0x5XY2) save an inclusive range of registers to memory starting at i.
        // https://github.com/JohnEarnest/Octo/blob/gh-pages/docs/XO-ChipSpecification.md#memory-access
        private void Save_vx_to_vy(int x, int y)
        {
            this.MnemomicFormat = "LD\t[I],V{0:X1}-V{1:X1}";

            var step = x > y ? -1 : +1;
            var address = this.I;
            var ongoing = true;
            do
            {
                this.Memory.Set(address++, this.V[x]);
                ongoing = x != y;
                x += step;
            }
            while (ongoing);
        }

        // load vx - vy (0x5XY3) load an inclusive range of registers from memory starting at i.
        // https://github.com/JohnEarnest/Octo/blob/gh-pages/docs/XO-ChipSpecification.md#memory-access
        private void Load_vx_to_vy(int x, int y)
        {
            this.MnemomicFormat = "LD\tV{0:X1}-V{1:X1},[I]";

            var step = x > y ? -1 : +1;
            var address = this.I;
            var ongoing = true;
            do
            {
                this.V[x] = this.Memory.Get(address++);
                ongoing = x != y;
                x += step;
            }
            while (ongoing);
        }

        // i := long NNNN (0xF000, 0xNNNN) load i with a 16-bit address.
        // https://github.com/JohnEarnest/Octo/blob/gh-pages/docs/XO-ChipSpecification.md#extended-memory
        private void Load_i_long()
        {
            this.nnnn = this.Memory.GetWord(this.PC);
            this.I = (ushort)this.nnnn;
            this.PC += 2;
        }

        ////plane n (0xFN01) select zero or more drawing planes by bitmask (0 <= n <= 3).
        private void Plane(int n)
        {
            this.MnemomicFormat = "PLANE\t#{0:X1}";
            this.Display.PlaneMask = n;
        }

        ////audio (0xF002) store 16 bytes starting at i in the audio pattern buffer.
        private void Audio()
        {
            this.MnemomicFormat = "AUDIO";
            Array.Copy(this.Memory.Bus, this.I, this.audoPatternBuffer, 0, this.audoPatternBuffer.Length);
        }
    }
}
