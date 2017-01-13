namespace Processor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;

    public class Chip8
    {
        private const int StandardFontOffset = 0x1b0;

        private static byte[] standardFont =
        { 
            0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
            0x20, 0x60, 0x20, 0x20, 0x70, // 1
            0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
            0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
            0x90, 0x90, 0xF0, 0x10, 0x10, // 4
            0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
            0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
            0xF0, 0x10, 0x20, 0x40, 0x40, // 7
            0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
            0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
            0xF0, 0x90, 0xF0, 0x90, 0x90, // A
            0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
            0xF0, 0x80, 0x80, 0x80, 0xF0, // C
            0xE0, 0x90, 0x90, 0x90, 0xE0, // D
            0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
            0xF0, 0x80, 0xF0, 0x80, 0x80  // F
        };

        private readonly Random randomNumbers = new Random();

        private readonly byte[] memory = new byte[4096];
        private readonly byte[] v = new byte[16];
        private readonly ushort[] stack = new ushort[16];

        private short i;
        private short pc;
        private byte delayTimer;
        private byte soundTimer;
        private ushort sp;

        private ushort opcode;

        private bool drawNeeded;

        private bool soundPlaying = false;

        private bool waitingForKeyPress;
        private int waitingForKeyPressRegister;

        private bool finished = false;

        // Disassembly members...
        private string mnemomicFormat;
        private bool usedAddress;
        private bool usedOperand;
        private bool usedN;
        private bool usedX;
        private bool usedY;

        private IKeyboardDevice keyboard;
        private IGraphicsDevice display;

        public Chip8(IKeyboardDevice keyboard, IGraphicsDevice display)
        {
            this.keyboard = keyboard;
            this.display = display;
        }

        public event EventHandler<EventArgs> BeepStarting;

        public event EventHandler<EventArgs> BeepStopped;

        public event EventHandler<EventArgs> EmulatingCycle;

        public event EventHandler<EventArgs> EmulatedCycle;

        public event EventHandler<DisassemblyEventArgs> DisassembleInstruction;

        public bool Finished
        {
            get
            {
                return this.finished;
            }

            set
            {
                this.finished = value;
            }
        }

        public bool DrawNeeded
        {
            get
            {
                return this.drawNeeded;
            }

            set
            {
                this.drawNeeded = value;
            }
        }

        public bool SoundPlaying
        {
            get
            {
                return this.soundPlaying;
            }

            private set
            {
                this.soundPlaying = value;
            }
        }

        public short PC
        {
            get
            {
                return this.pc;
            }

            set
            {
                this.pc = value;
            }
        }

        public ushort SP
        {
            get
            {
                return this.sp;
            }

            set
            {
                this.sp = value;
            }
        }

        public short I
        {
            get
            {
                return this.i;
            }

            set
            {
                this.i = value;
            }
        }

        public byte[] Memory
        {
            get
            {
                return this.memory;
            }
        }

        public byte[] V
        {
            get
            {
                return this.v;
            }
        }

        public ushort[] Stack
        {
            get
            {
                return this.stack;
            }
        }

        public IGraphicsDevice Display
        {
            get
            {
                return this.display;
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
        public virtual int CyclesPerFrame
        {
            get
            {
                // Running 13 FPS at .78 kOps
                return 13;
            }
        }

        protected string MnemomicFormat
        {
            get
            {
                return this.mnemomicFormat;
            }

            set
            {
                this.mnemomicFormat = value;
            }
        }

        protected bool UsedAddress
        {
            get
            {
                return this.usedAddress;
            }

            set
            {
                this.usedAddress = value;
            }
        }

        protected bool UsedOperand
        {
            get
            {
                return this.usedOperand;
            }

            set
            {
                this.usedOperand = value;
            }
        }

        protected bool UsedN
        {
            get
            {
                return this.usedN;
            }

            set
            {
                this.usedN = value;
            }
        }

        protected bool UsedX
        {
            get
            {
                return this.usedX;
            }

            set
            {
                this.usedX = value;
            }
        }

        protected bool UsedY
        {
            get
            {
                return this.usedY;
            }

            set
            {
                this.usedY = value;
            }
        }

        public virtual void Initialise()
        {
            this.Finished = false;
            this.DrawNeeded = false;

            this.PC = 0x200;     // Program counter starts at 0x200
            this.I = 0;          // Reset index register
            this.SP = 0;         // Reset stack pointer

            this.Display.Initialise();

            // Clear stack
            Array.Clear(this.Stack, 0, this.Stack.Length);

            // Clear registers V0-VF
            Array.Clear(this.V, 0, this.V.Length);

            // Clear memory
            Array.Clear(this.Memory, 0, this.Memory.Length);

            // Load fonts
            Array.Copy(standardFont, 0, this.Memory, StandardFontOffset, 16 * 5);

            // Reset timers
            this.delayTimer = this.soundTimer = 0;
        }

        public void LoadGame(string game)
        {
            var path = @"..\..\..\Roms\" + game;
            this.LoadRom(path, 0x200);
        }

        public void Step()
        {
            if (this.waitingForKeyPress)
            {
                this.WaitForKeyPress();
            }
            else
            {
                this.EmulateCycle();
            }
        }

        public void UpdateTimers()
        {
            this.UpdateDelayTimer();
            this.UpdateSoundTimer();
        }

        protected void OnBeepStarting()
        {
            var handler = this.BeepStarting;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }

            this.SoundPlaying = true;
        }

        protected void OnBeepStopped()
        {
           this.SoundPlaying = false;

            var handler = this.BeepStopped;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected virtual void OnEmulatingCycle(short programCounter, ushort instruction, short address, byte operand, int n, int x, int y)
        {
            var handler = this.EmulatingCycle;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected virtual void OnEmulatedCycle(short programCounter, ushort instruction, short address, byte operand, int n, int x, int y)
        {
            this.OnDisassembleInstruction(programCounter, instruction, address, operand, n, x, y);

            var handler = this.EmulatedCycle;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected virtual void OnDisassembleInstruction(short programCounter, ushort instruction, short address, byte operand, int n, int x, int y)
        {
            var handler = this.DisassembleInstruction;
            if (handler != null)
            {
                var objects = new List<object>();

                if (this.UsedAddress)
                {
                    objects.Add(address);
                }

                if (this.UsedN)
                {
                    objects.Add(n);
                }

                if (this.UsedX)
                {
                    objects.Add(x);
                }

                if (this.UsedY)
                {
                    objects.Add(y);
                }

                if (this.UsedOperand)
                {
                    objects.Add(operand);
                }

                var pre = string.Format(CultureInfo.InvariantCulture, "PC={0:x4}\t{1:x4}\t", programCounter, instruction);
                var post = string.Format(CultureInfo.InvariantCulture, this.MnemomicFormat, objects.ToArray());

                handler(this, new DisassemblyEventArgs(pre + post));
            }
        }

        protected virtual void EmulateCycle()
        {
            var high = this.Memory[this.PC];
            var low = this.Memory[this.PC + 1];
            this.opcode = (ushort)((high << 8) + low);
            var nnn = (short)(this.opcode & 0xfff);
            var nn = low;
            var n = low & 0xf;
            var x = high & 0xf;
            var y = (low & 0xf0) >> 4;

            if ((this.PC % 2) == 1)
            {
                throw new InvalidOperationException("Instruction is not on an aligned address");
            }

            var programCounter = this.PC;
            this.PC += 2;

            this.OnEmulatingCycle(programCounter, this.opcode, nnn, nn, n, x, y);
            try
            {
                if (!this.EmulateInstruction(nnn, nn, n, x, y))
                {
                    throw new IllegalInstructionException(this.opcode);
                }
            }
            finally
            {
                this.OnEmulatedCycle(programCounter, this.opcode, nnn, nn, n, x, y);
            }
        }

        protected virtual void Draw(int x, int y, int width, int height)
        {
            var hits = this.Display.Draw(this.Memory, this.i, this.V[x], this.V[y], width, height);
            this.V[0xf] = (byte)hits;
            this.DrawNeeded = true;
        }

        protected virtual bool EmulateInstruction(short nnn, byte nn, int n, int x, int y)
        {
            switch (this.opcode & 0xf000)
            {
                case 0x0000:    // Call
                    return this.EmulateInstructions_0(nn, n, y);

                // Jump
                case 0x1000:        // 1NNN     Flow        goto NNN;
                    return this.EmulateInstructions_1(nnn);

                // Call
                case 0x2000:        // 2NNN     Flow        *(0xNNN)()
                    return this.EmulateInstructions_2(nnn);

                // Conditional
                case 0x3000:        // 3XNN     Cond        if(Vx==NN)
                    return this.EmulateInstructions_3(nn, x);

                // Conditional
                case 0x4000:        // 4XNN     Cond        if(Vx!=NN)
                    return this.EmulateInstructions_4(nn, x);

                // Conditional
                case 0x5000:        // 5XNN     Cond        if(Vx==Vy)
                    return this.EmulateInstructions_5(x, y);

                case 0x6000:        // 6XNN     Const       Vx = NN
                    return this.EmulateInstructions_6(nn, x);

                case 0x7000:        // 7XNN     Const       Vx += NN
                    return this.EmulateInstructions_7(nn, x);

                case 0x8000:
                    return this.EmulateInstructions_8(n, x, y);

                case 0x9000:
                    return this.EmulateInstructions_9(n, x, y);

                case 0xa000:        // ANNN     MEM         I = NNN
                    return this.EmulateInstructions_A(nnn);

                case 0xB000:        // BNNN     Flow        PC=V0+NNN
                    return this.EmulateInstructions_B(nnn, x);

                case 0xc000:        // CXNN     Rand        Vx=rand()&NN
                    return this.EmulateInstructions_C(nn, x);

                case 0xd000:        // DXYN     Disp        draw(Vx,Vy,N)
                    return this.EmulateInstructions_D(n, x, y);

                case 0xe000:
                    return this.EmulateInstructions_E(nn, x);

                case 0xf000:
                    return this.EmulateInstructions_F(nn, x);

                default:
                    return false;
            }
        }

        protected virtual bool EmulateInstructions_F(byte nn, int x)
        {
            this.UsedX = true;
            switch (nn)
            {
                case 0x07:  // FX07     Timer       Vx = get_delay()
                    this.LD_Vx_DT(x);
                    break;

                case 0x0a:  // FX0A     KeyOp       Vx = get_key()
                    this.LD_Vx_K(x);
                    break;

                case 0x15:  // FX15     Timer       delay_timer(Vx)
                    this.LD_DT_Vx(x);
                    break;

                case 0x18:  // FX18     Sound       sound_timer(Vx)
                    this.LD_ST_Vx(x);
                    break;

                case 0x1e:  // FX1E     Mem         I +=Vx
                    this.ADD_I_Vx(x);
                    break;

                case 0x29:  // FX29     Mem         I=sprite_addr[Vx]
                    this.LD_F_Vx(x);
                    break;

                case 0x33:  // FX33     BCD
                            //                      set_BCD(Vx);
                            //                      *(I+0)=BCD(3);
                            //                      *(I+1)=BCD(2);
                            //                      *(I+2)=BCD(1);
                    this.LD_B_Vx(x);
                    break;

                case 0x55:  // FX55     MEM         reg_dump(Vx,&I)
                    this.LD_II_Vx(x);
                    break;

                case 0x65:  // FX65     MEM         reg_load(Vx,&I)
                    this.LD_Vx_II(x);
                    break;

                default:
                    return false;
            }

            return true;
        }

        protected virtual bool EmulateInstructions_E(byte nn, int x)
        {
            this.UsedX = true;
            switch (nn)
            {
                case 0x9E:  // EX9E     KeyOp       if(key()==Vx)
                    this.SKP(x);
                    break;

                case 0xa1:  // EXA1     KeyOp       if(key()!=Vx)
                    this.SKNP(x);
                    break;

                default:
                    return false;
            }

            return true;
        }

        protected virtual bool EmulateInstructions_D(int n, int x, int y)
        {
            this.UsedX = this.UsedY = this.UsedN = true;
            this.DRW(x, y, n);
            return true;
        }

        protected virtual bool EmulateInstructions_C(byte nn, int x)
        {
            this.UsedX = this.UsedOperand = true;
            this.RND(x, nn);
            return true;
        }

        protected virtual bool EmulateInstructions_B(short nnn, int x)
        {
            this.UsedAddress = true;
            this.JP_V0(x, nnn);
            return true;
        }

        protected virtual bool EmulateInstructions_A(short nnn)
        {
            this.UsedAddress = true;
            this.LD_I(nnn);
            return true;
        }

        protected virtual bool EmulateInstructions_9(int n, int x, int y)
        {
            this.UsedX = this.UsedY = true;
            switch (n)
            {
                case 0:     // 9XY0     Cond        if(Vx!=Vy)
                    this.SNE(x, y);
                    break;

                default:
                    return false;
            }

            return true;
        }

        protected virtual bool EmulateInstructions_8(int n, int x, int y)
        {
            this.UsedX = this.UsedY = true;
            switch (n)
            {
                case 0x0:   // 8XY0     Assign      Vx=Vy
                    this.LD(x, y);
                    break;

                case 0x1:   // 8XY1     BitOp       Vx=Vx|Vy
                    this.OR(x, y);
                    break;

                case 0x2:   // 8XY2     BitOp       Vx=Vx&Vy
                    this.AND(x, y);
                    break;

                case 0x3:   // 8XY3     BitOp       Vx=Vx^Vy
                    this.XOR(x, y);
                    break;

                case 0x4:   // 8XY4     Math        Vx += Vy
                    this.ADD(x, y);
                    break;

                case 0x5:   // 8XY5     Math        Vx -= Vy
                    this.SUB(x, y);
                    break;

                case 0x6:   // 8XY6     Math        Vx >> 1
                    this.SHR(x, y);
                    break;

                case 0x7:   // 8XY7     Math        Vx=Vy-Vx
                    this.SUBN(x, y);
                    break;

                case 0xe:   // 8XYE     Math        Vx << 1
                    this.SHL(x, y);
                    break;

                default:
                    return false;
            }

            return true;
        }

        protected virtual bool EmulateInstructions_7(byte nn, int x)
        {
            this.UsedX = this.UsedOperand = true;
            this.ADD(x, nn);
            return true;
        }

        protected virtual bool EmulateInstructions_6(byte nn, int x)
        {
            this.UsedX = this.UsedOperand = true;
            this.LD(x, nn);
            return true;
        }

        protected virtual bool EmulateInstructions_5(int x, int y)
        {
            this.UsedX = this.UsedY = true;
            this.SE(x, y);
            return true;
        }

        protected virtual bool EmulateInstructions_4(byte nn, int x)
        {
            this.UsedOperand = this.UsedX = true;
            this.SNE(x, nn);
            return true;
        }

        protected virtual bool EmulateInstructions_3(byte nn, int x)
        {
            this.UsedOperand = this.UsedX = true;
            this.SE(x, nn);
            return true;
        }

        protected virtual bool EmulateInstructions_2(short nnn)
        {
            this.UsedAddress = true;
            this.CALL(nnn);
            return true;
        }

        protected virtual bool EmulateInstructions_1(short nnn)
        {
            this.UsedAddress = true;
            this.JP(nnn);
            return true;
        }

        protected virtual bool EmulateInstructions_0(byte low, int n, int y)
        {
            switch (low)
            {
                case 0xe0:  // 00E0     Display     disp_clear()
                    this.CLS();
                    break;

                case 0xee:  // 00EE     Flow        return;
                    this.RET();
                    break;

                default:
                    return false;
            }

            return true;
        }

        ////

        protected virtual void CLS()
        {
            this.MnemomicFormat = "CLS";
            this.Display.Clear();
            this.DrawNeeded = true;
        }

        protected virtual void RET()
        {
            this.MnemomicFormat = "RET";
            this.PC = (short)this.Stack[--this.SP & 0xF];
        }

        protected virtual void JP(short nnn)
        {
            this.MnemomicFormat = "JP\t{0:X3}";
            this.PC = nnn;
        }

        protected virtual void CALL(short nnn)
        {
            this.MnemomicFormat = "CALL\t{0:X3}";
            this.Stack[this.SP++] = (ushort)this.PC;
            this.PC = nnn;
        }

        protected virtual void SE(int x, byte nn)
        {
            this.MnemomicFormat = "SE\tV{0:X1},#{1:X2}";
            if (this.V[x] == nn)
            {
                this.PC += 2;
            }
        }

        protected virtual void SNE(int x, byte nn)
        {
            this.MnemomicFormat = "SNE\tV{0:X1},#{1:X2}";
            if (this.V[x] != nn)
            {
                this.PC += 2;
            }
        }

        protected virtual void SE(int x, int y)
        {
            this.MnemomicFormat = "SE\tV{0:X1},V{1:X1}";
            if (this.V[x] == this.V[y])
            {
                this.PC += 2;
            }
        }

        protected virtual void LD(int x, byte nn)
        {
            this.MnemomicFormat = "LD\tV{0:X1},#{1:X2}";
            this.V[x] = nn;
        }

        protected virtual void ADD(int x, byte nn)
        {
            this.MnemomicFormat = "ADD\tV{0:X1},#{1:X2}";
            this.V[x] += nn;
        }

        protected virtual void LD(int x, int y)
        {
            this.MnemomicFormat = "LD\tV{0:X1},V{1:X1}";
            this.V[x] = this.V[y];
        }

        protected virtual void OR(int x, int y)
        {
            this.MnemomicFormat = "OR\tV{0:X1},V{1:X1}";
            this.V[x] |= this.V[y];
        }

        protected virtual void AND(int x, int y)
        {
            this.MnemomicFormat = "AND\tV{0:X1},V{1:X1}";
            this.V[x] &= this.V[y];
        }

        protected virtual void XOR(int x, int y)
        {
            this.MnemomicFormat = "XOR\tV{0:X1},V{1:X1}";
            this.V[x] ^= this.V[y];
        }

        protected virtual void ADD(int x, int y)
        {
            this.MnemomicFormat = "ADD\tV{0:X1},V{1:X1}";
            this.V[0xf] = (byte)(this.V[y] > (0xff - this.V[x]) ? 1 : 0);
            this.V[x] += this.V[y];
        }

        protected virtual void SUB(int x, int y)
        {
            this.MnemomicFormat = "SUB\tV{0:X1},V{1:X1}";
            this.V[0xf] = (byte)(this.V[x] >= this.V[y] ? 1 : 0);
            this.V[x] -= this.V[y];
        }

        protected virtual void SHR(int x, int y)
        {
            // https://github.com/Chromatophore/HP48-Superchip#8xy6--8xye
            // Bit shifts X register by 1, VIP: shifts Y by one and places in X, HP48-SC: ignores Y field, shifts X
            this.MnemomicFormat = "SHR\tV{0:X1},V{0:X1}";
            this.V[y] >>= 1;
            this.V[x] = this.V[y];
            this.V[0xf] = (byte)(this.V[x] & 0x1);
        }

        protected virtual void SUBN(int x, int y)
        {
            this.MnemomicFormat = "SUBN\tV{0:X1},V{1:X1}";
            this.V[0xf] = (byte)(this.V[x] > this.V[y] ? 0 : 1);
            this.V[x] = (byte)(this.V[y] - this.V[x]);
        }

        protected virtual void SHL(int x, int y)
        {
            // https://github.com/Chromatophore/HP48-Superchip#8xy6--8xye
            // Bit shifts X register by 1, VIP: shifts Y by one and places in X, HP48-SC: ignores Y field, shifts X
            this.MnemomicFormat = "SHL\tV{0:X1},V{1:X1}";
            this.V[0xf] = (byte)((this.V[x] & 0x80) == 0 ? 0 : 1);
            this.V[y] <<= 1;
            this.V[x] = this.V[y];
        }

        protected virtual void SNE(int x, int y)
        {
            this.MnemomicFormat = "SNE\tV{0:X1},V{1:X1}";
            if (this.V[x] != this.V[y])
            {
                this.PC += 2;
            }
        }

        protected virtual void LD_I(short nnn)
        {
            this.MnemomicFormat = "LD\tI,#{0:X3}";
            this.I = nnn;
        }

        protected virtual void JP_V0(int x, short nnn)
        {
            this.MnemomicFormat = "JP\t[V0],#{0:X3}";

            // https://github.com/Chromatophore/HP48-Superchip#bnnn
            // Sets PC to address NNN + v0 -
            //  VIP: correctly jumps based on v0
            //  HP48 -SC: reads highest nibble of address to select
            //      register to apply to address (high nibble pulls double duty)
            this.PC = (short)(this.V[0] + nnn);
        }

        protected virtual void RND(int x, byte nn)
        {
            this.MnemomicFormat = "RND\tV{0:X1},#{1:X2}";
            this.V[x] = (byte)(this.randomNumbers.Next(byte.MaxValue) & nn);
        }

        protected virtual void DRW(int x, int y, int n)
        {
            this.MnemomicFormat = "DRW\tV{0:X1},V{1:X1},#{2:X1}";
            this.UsedN = true;
            this.Draw(x, y, 8, n);
        }

        protected virtual void SKP(int x)
        {
            this.MnemomicFormat = "SKP\tV{0:X1}";
            if (this.keyboard.IsKeyPressed(this.V[x]))
            {
                this.PC += 2;
            }
        }

        protected virtual void SKNP(int x)
        {
            this.MnemomicFormat = "SKNP\tV{0:X1}";
            if (!this.keyboard.IsKeyPressed(this.V[x]))
            {
                this.PC += 2;
            }
        }

        protected virtual void LD_Vx_II(int x)
        {
            // https://github.com/Chromatophore/HP48-Superchip#fx55--fx65
            // Saves/Loads registers up to X at I pointer - VIP: increases I, HP48-SC: I remains static
            this.MnemomicFormat = "LD\tV{0:X1},[I]";
            Array.Copy(this.Memory, this.I, this.V, 0, x + 1);
            this.I += (short)(x + 1);
        }

        protected virtual void LD_II_Vx(int x)
        {
            // https://github.com/Chromatophore/HP48-Superchip#fx55--fx65
            // Saves/Loads registers up to X at I pointer - VIP: increases I, HP48-SC: I remains static
            this.MnemomicFormat = "LD\t[I],V{0:X1}";
            Array.Copy(this.V, 0, this.Memory, this.I, x + 1);
            this.I += (short)(x + 1);
        }

        protected virtual void LD_B_Vx(int x)
        {
            this.MnemomicFormat = "LD\tB,V{0:X1}";
            var content = this.V[x];
            this.memory[this.I] = (byte)(content / 100);
            this.memory[this.I + 1] = (byte)((content / 10) % 10);
            this.memory[this.I + 2] = (byte)((content % 100) % 10);
        }

        protected virtual void LD_F_Vx(int x)
        {
            this.MnemomicFormat = "LD\tF,V{0:X1}";
            this.I = (short)(StandardFontOffset + (5 * this.V[x]));
        }

        protected virtual void ADD_I_Vx(int x)
        {
            this.MnemomicFormat = "ADD\tI,V{0:X1}";

            // From wikipedia entry on CHIP-8:
            // VF is set to 1 when there is a range overflow (I+VX>0xFFF), and to 0
            // when there isn't. This is an undocumented feature of the CHIP-8 and used by the Spacefight 2091! game
            var sum = this.I + this.V[x];
            this.V[0xf] = (byte)(sum > 0xfff ? 1 : 0);

            this.I += this.V[x];
        }

        protected virtual void LD_ST_Vx(int x)
        {
            this.MnemomicFormat = "LD\tST,V{0:X1}";
            this.soundTimer = this.V[x];
        }

        protected virtual void LD_DT_Vx(int x)
        {
            this.MnemomicFormat = "LD\tDT,V{0:X1}";
            this.delayTimer = this.V[x];
        }

        protected virtual void LD_Vx_K(int x)
        {
            this.MnemomicFormat = "LD\tV{0:X1},K";
            this.waitingForKeyPress = true;
            this.waitingForKeyPressRegister = x;
        }

        protected virtual void LD_Vx_DT(int x)
        {
            this.MnemomicFormat = "LD\tV{0:X1},DT";
            this.V[x] = this.delayTimer;
        }

        ////

        private void WaitForKeyPress()
        {
            int key;
            if (this.keyboard.CheckKeyPress(out key))
            {
                this.waitingForKeyPress = false;
                this.V[this.waitingForKeyPressRegister] = (byte)key;
            }
        }

        private void UpdateDelayTimer()
        {
            if (this.delayTimer > 0)
            {
                --this.delayTimer;
            }
        }

        private void UpdateSoundTimer()
        {
            if (this.soundTimer > 0)
            {
                if (!this.SoundPlaying)
                {
                    this.OnBeepStarting();
                }

                --this.soundTimer;
            }
            else
            {
                if (this.SoundPlaying)
                {
                    this.OnBeepStopped();
                }
            }
        }

        private void LoadRom(string path, ushort offset)
        {
            using (var file = File.Open(path, FileMode.Open))
            {
                var size = file.Length;

                var bytes = new byte[size];
                using (var reader = new BinaryReader(file, new System.Text.UTF8Encoding(), true))
                {
                    reader.Read(bytes, 0, (int)size);
                }

                var headerLength = 0;
                var encoding = new ASCIIEncoding();
                var header = encoding.GetString(bytes, 0, 8);
                if (header == "HPHP48-A")
                {
                    headerLength = 13;
                }

                Array.Copy(bytes, headerLength, this.Memory, offset, size - headerLength);
            }
        }
    }
}
