﻿namespace Processor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;

    public class Chip8
    {
        private const int StandardFontOffset = 0x1b0;
        private const int HighFontOffset = 0x110;

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

        private readonly EmulationType emulating;

        private readonly Random randomNumbers = new Random();

        private readonly byte[] memory = new byte[4096];
        private readonly byte[] v = new byte[16];
        private readonly ushort[] stack = new ushort[16];
        private readonly byte[] r = new byte[8]; // HP48 flags

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

        private bool compatibility = false;

        // Disassembly members...
        private string mnemomicFormat;
        private bool usedAddress;
        private bool usedOperand;
        private bool usedN;
        private bool usedX;
        private bool usedY;

        private IKeyboardDevice keyboard;
        private IGraphicsDevice display;

        public Chip8(EmulationType emulating, IKeyboardDevice keyboard, IGraphicsDevice display)
        {
            this.emulating = emulating;
            this.keyboard = keyboard;
            this.display = display;
        }

        public event EventHandler<EventArgs> HighResolutionConfigured;

        public event EventHandler<EventArgs> LowResolutionConfigured;

        public event EventHandler<EventArgs> BeepStarting;

        public event EventHandler<EventArgs> BeepStopped;

        public event EventHandler<EventArgs> EmulatingCycle;

        public event EventHandler<EventArgs> EmulatedCycle;

        public event EventHandler<DisassemblyEventArgs> BeginCycleDisassembly;

        public event EventHandler<DisassemblyEventArgs> FinishCycleDisassembly;

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

        public void Initialise()
        {
            this.Finished = false;
            this.DrawNeeded = false;
            this.display.HighResolution = false;

            this.pc = 0x200;     // Program counter starts at 0x200
            this.i = 0;          // Reset index register
            this.sp = 0;         // Reset stack pointer

            this.display.AllocateMemory();
            this.display.Clear();

            // Clear stack
            Array.Clear(this.stack, 0, 16);

            // Clear registers V0-VF
            Array.Clear(this.v, 0, 16);

            // Clear memory
            Array.Clear(this.memory, 0, 4096);

            // Load fonts
            Array.Copy(standardFont, 0, this.memory, StandardFontOffset, 16 * 5);
            Array.Copy(highFont, 0, this.memory, HighFontOffset, 16 * 10);

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

        protected void OnHighResolution()
        {
            this.display.HighResolution = true;
            this.display.AllocateMemory();

            var handler = this.HighResolutionConfigured;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected void OnLowResolution()
        {
            this.display.HighResolution = false;
            this.display.AllocateMemory();

            var handler = this.LowResolutionConfigured;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
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

            this.OnBeginCycleDisassembly(programCounter, instruction, address, operand, n, x, y);
        }

        protected virtual void OnEmulatedCycle(short programCounter, ushort instruction, short address, byte operand, int n, int x, int y)
        {
            this.OnFinishCycleDisassembly(programCounter, instruction, address, operand, n, x, y);

            var handler = this.EmulatedCycle;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected virtual void OnBeginCycleDisassembly(short programCounter, ushort instruction, short address, byte operand, int n, int x, int y)
        {
            this.mnemomicFormat = "UNKNOWN";
            this.usedAddress = this.usedOperand = this.usedN = this.usedX = this.usedY = false;

            var handler = this.BeginCycleDisassembly;
            if (handler != null)
            {
                var output = string.Format(CultureInfo.InvariantCulture, "PC={0:x4}\t{1:x4}\t", this.pc, this.opcode);
                handler(this, new DisassemblyEventArgs(output));
            }
        }

        protected virtual void OnFinishCycleDisassembly(short programCounter, ushort instruction, short address, byte operand, int n, int x, int y)
        {
            var handler = this.FinishCycleDisassembly;
            if (handler != null)
            {
                var objects = new List<object>();

                if (this.usedAddress)
                {
                    objects.Add(address);
                }

                if (this.usedN)
                {
                    objects.Add(n);
                }

                if (this.usedX)
                {
                    objects.Add(x);
                }

                if (this.usedY)
                {
                    objects.Add(y);
                }

                if (this.usedOperand)
                {
                    objects.Add(operand);
                }

                var output = string.Format(CultureInfo.InvariantCulture, this.mnemomicFormat + "\n", objects.ToArray());

                handler(this, new DisassemblyEventArgs(output));
            }
        }

        private void WaitForKeyPress()
        {
            int key;
            if (this.keyboard.CheckKeyPress(out key))
            {
                this.waitingForKeyPress = false;
                this.v[this.waitingForKeyPressRegister] = (byte)key;
            }
        }

        private void EmulateCycle()
        {
            var high = this.memory[this.pc];
            var low = this.memory[this.pc + 1];
            this.opcode = (ushort)((high << 8) + low);
            var nnn = (short)(this.opcode & 0xfff);
            var nn = low;
            var n = low & 0xf;
            var x = high & 0xf;
            var y = (low & 0xf0) >> 4;

            if ((this.pc % 2) == 1)
            {
                throw new InvalidOperationException("Instruction is not on an aligned address");
            }

            var programCounter = this.pc;
            this.OnEmulatingCycle(programCounter, this.opcode, nnn, nn, n, x, y);

            this.pc += 2;

            switch (this.opcode & 0xf000)
            {
                case 0x0000:    // Call
                    switch (low)
                    {
                        case 0xe0:  // 00E0     Display     disp_clear()
                            this.CLS();
                            break;

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

                        case 0xee:  // 00EE     Flow        return;
                            this.RET();
                            break;

                        default:
                            switch (y)
                            {
                                case 0xc:
                                    this.SCDOWN(n);
                                    break;

                                default:
                                    throw new IllegalInstructionException(this.opcode, "RCA1802 Call");
                            }

                            break;
                    }

                    break;

                // Jump
                case 0x1000:        // 1NNN     Flow        goto NNN;
                    this.JP(nnn);
                    break;

                // Call
                case 0x2000:        // 2NNN     Flow        *(0xNNN)()
                    this.CALL(nnn);
                    break;

                // Conditional
                case 0x3000:        // 3XNN     Cond        if(Vx==NN)
                    this.SE(x, nn);
                    break;

                // Conditional
                case 0x4000:        // 4XNN     Cond        if(Vx!=NN)
                    this.SNE(x, nn);
                    break;

                // Conditional
                case 0x5000:        // 5XNN     Cond        if(Vx==Vy)
                    this.SE(x, y);
                    break;

                case 0x6000:        // 6XNN     Const       Vx = NN
                    this.LD(x, nn);
                    break;

                case 0x7000:        // 7XNN     Const       Vx += NN
                    this.ADD(x, nn);
                    break;

                case 0x8000:
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
                            throw new IllegalInstructionException(this.opcode);
                    }

                    break;

                case 0x9000:
                    switch (n)
                    {
                        case 0:     // 9XY0     Cond        if(Vx!=Vy)
                            this.SNE(x, y);
                            break;

                        default:
                            throw new IllegalInstructionException(this.opcode);
                    }

                    break;

                case 0xa000:        // ANNN     MEM         I = NNN
                    this.LD_I(nnn);
                    break;

                case 0xB000:        // BNNN     Flow        PC=V0+NNN
                    this.JP_V0(x, nnn);
                    break;

                case 0xc000:        // CXNN     Rand        Vx=rand()&NN
                    this.RND(x, nn);
                    break;

                case 0xd000:        // DXYN     Disp        draw(Vx,Vy,N)
                    switch (n)
                    {
                        case 0:
                            this.XDRW(x, y);
                            break;

                        default:
                            this.DRW(x, y, n);
                            break;
                    }

                    break;

                case 0xe000:
                    switch (nn)
                    {
                        case 0x9E:  // EX9E     KeyOp       if(key()==Vx)
                            this.SKP(x);
                            break;

                        case 0xa1:  // EXA1     KeyOp       if(key()!=Vx)
                            this.SKNP(x);
                            break;

                        default:
                            throw new IllegalInstructionException(this.opcode);
                    }

                    break;

                case 0xf000:
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

                        case 0x30:
                            this.LD_HF_Vx(x);
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

                        case 0x75:
                            this.LD_R_Vx(x);
                            break;

                        case 0x85:
                            this.LD_Vx_R(x);
                            break;

                        default:
                            throw new IllegalInstructionException(this.opcode);
                    }

                    break;

                default:
                    throw new IllegalInstructionException(this.opcode);
            }

            this.OnEmulatedCycle(programCounter, this.opcode, nnn, nn, n, x, y);
        }

        ////

        // scdown n
        // Scroll the screen down n pixels. [Super-Chip]
        // This opcode delays until the start of a 60Hz clock cycle before drawing in low resolution mode.
        // (Use the delay timer to pace your games in high resolution mode.)
        // Code generated: 0x00Cn
        private void SCDOWN(int n)
        {
            this.usedN = true;
            this.mnemomicFormat = "SCDOWN\t{0:X1}";

            this.VerifyRunningHp48();

            var screenHeight = this.display.Height;

            // Copy rows bottom to top
            for (int y = screenHeight - n - 1; y >= 0; --y)
            {
                this.display.CopyRow(y + n, y);
            }

            // Remove the top columns, blanked by the scroll effect
            for (int y = 0; y < n; ++y)
            {
                this.display.ClearRow(y);
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
            this.mnemomicFormat = "COMPATIBILITY";
            this.compatibility = true;
        }

        // scright
        // Scroll the screen right 4 pixels. [Super-Chip]
        // This opcode delays until the start of a 60Hz clock cycle before drawing in low resolution mode.
        // (Use the delay timer to pace your games in high resolution mode.)
        // Code generated: 0x00FB
        private void SCRIGHT()
        {
            this.mnemomicFormat = "SCRIGHT";

            this.VerifyRunningHp48();

            var screenWidth = this.display.Width;

            // Scroll distance
            var n = 4;

            // Copy colummns from right to left
            for (int x = screenWidth - n - 1; x >= 0; --x)
            {
                this.display.CopyColumn(x + n, x);
            }

            // Remove the leftmost columns, blanked by the scroll effect
            for (int x = 0; x < n; ++x)
            {
                this.display.ClearColumn(x);
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
            this.mnemomicFormat = "SCLEFT";

            this.VerifyRunningHp48();

            var screenWidth = this.display.Width;

            // Scroll distance
            var n = 4;

            // Copy columns from left to right
            for (int x = 0; x < screenWidth - n - 1; ++x)
            {
                this.display.CopyColumn(x, x + n);
            }

            // Remove the rightmost columns, blanked by the scroll effect
            for (int x = screenWidth - n - 1; x < screenWidth; ++x)
            {
                this.display.ClearColumn(x);
            }

            this.DrawNeeded = true;
        }

        // low
        // Low resolution (64×32) graphics mode (this is the default). [Super-Chip]
        // Code generated: 0x00FE
        private void LOW()
        {
            this.mnemomicFormat = "LOW";
            this.VerifyRunningHp48();
            this.OnLowResolution();
        }

        // high
        // High resolution (128×64) graphics mode. [Super-Chip]
        // Code generated: 0x00FF
        private void HIGH()
        {
            this.mnemomicFormat = "HIGH";
            this.VerifyRunningHp48();
            this.OnHighResolution();
        }

        // flags.save vX
        // Store the values of registers v0 to vX into the "flags" registers (this means something in the
        // HP48 implementation). (X < 8) [Super-Chip]
        // Code generated: 0xFX75
        private void LD_R_Vx(int x)
        {
            this.mnemomicFormat = "LD\tR,V{0:X1}";
            this.usedX = true;

            this.VerifyRunningHp48();
            Array.Copy(this.v, this.r, x + 1);
        }

        // flags.restore vX
        // Read the values of registers v0 to vX from the "flags" registers (this means something in the
        // HP48 implementation). (X < 8) [Super-Chip]
        // Code generated: 0xFX85
        private void LD_Vx_R(int x)
        {
            this.mnemomicFormat = "LD\tV{0:X1},R";
            this.usedX = true;

            this.VerifyRunningHp48();
            Array.Copy(this.r, this.v, x + 1);
        }

        // exit
        // This opcode is used to terminate the chip8run program. It causes the chip8run program to exit
        // with a successful exit status. [Super-Chip]
        // Code generated: 0x00FD.
        private void EXIT()
        {
            this.mnemomicFormat = "EXIT";
            this.VerifyRunningHp48();
            this.Finished = true;
        }

        ////

        private void CLS()
        {
            this.mnemomicFormat = "CLS";
            this.display.Clear();
            this.DrawNeeded = true;
        }

        private void RET()
        {
            this.mnemomicFormat = "RET";
            this.pc = (short)this.stack[--this.sp & 0xF];
        }

        private void JP(short nnn)
        {
            this.mnemomicFormat = "JP\t{0:X3}";
            this.usedAddress = true;
            this.pc = nnn;
        }

        private void CALL(short nnn)
        {
            this.mnemomicFormat = "CALL\t{0:X3}";
            this.usedAddress = true;
            this.stack[this.sp++] = (ushort)this.pc;
            this.pc = nnn;
        }

        private void SE(int x, byte nn)
        {
            this.mnemomicFormat = "SE\tV{0:X1},#{1:X2}";
            this.usedOperand = this.usedX = true;

            if (this.v[x] == nn)
            {
                this.pc += 2;
            }
        }

        private void SNE(int x, byte nn)
        {
            this.mnemomicFormat = "SNE\tV{0:X1},#{1:X2}";
            this.usedOperand = this.usedX = true;

            if (this.v[x] != nn)
            {
                this.pc += 2;
            }
        }

        private void SE(int x, int y)
        {
            this.mnemomicFormat = "SE\tV{0:X1},V{1:X1}";
            this.usedX = this.usedY = true;

            if (this.v[x] == this.v[y])
            {
                this.pc += 2;
            }
        }

        private void LD(int x, byte nn)
        {
            this.mnemomicFormat = "LD\tV{0:X1},#{1:X2}";
            this.usedX = this.usedOperand = true;

            this.v[x] = nn;
        }

        private void ADD(int x, byte nn)
        {
            this.mnemomicFormat = "ADD\tV{0:X1},#{1:X2}";
            this.usedX = this.usedOperand = true;

            this.v[x] += nn;
        }

        private void LD(int x, int y)
        {
            this.mnemomicFormat = "LD\tV{0:X1},V{1:X1}";
            this.usedX = this.usedY = true;

            this.v[x] = this.v[y];
        }

        private void OR(int x, int y)
        {
            this.mnemomicFormat = "OR\tV{0:X1},V{1:X1}";
            this.usedX = this.usedY = true;

            this.v[x] |= this.v[y];
        }

        private void AND(int x, int y)
        {
            this.mnemomicFormat = "AND\tV{0:X1},V{1:X1}";
            this.usedX = this.usedY = true;
            this.v[x] &= this.v[y];
        }

        private void XOR(int x, int y)
        {
            this.mnemomicFormat = "XOR\tV{0:X1},V{1:X1}";
            this.usedX = this.usedY = true;
            this.v[x] ^= this.v[y];
        }

        private void ADD(int x, int y)
        {
            this.mnemomicFormat = "ADD\tV{0:X1},V{1:X1}";
            this.usedX = this.usedY = true;
            this.v[0xf] = (byte)(this.v[y] > (0xff - this.v[x]) ? 1 : 0);
            this.v[x] += this.v[y];
        }

        private void SUB(int x, int y)
        {
            this.mnemomicFormat = "SUB\tV{0:X1},V{1:X1}";
            this.usedX = this.usedY = true;
            this.v[0xf] = (byte)(this.v[x] >= this.v[y] ? 1 : 0);
            this.v[x] -= this.v[y];
        }

        private void SHR(int x, int y)
        {
            // https://github.com/Chromatophore/HP48-Superchip#8xy6--8xye
            // Bit shifts X register by 1, VIP: shifts Y by one and places in X, HP48-SC: ignores Y field, shifts X
            if (this.emulating == EmulationType.ComsmacVip)
            {
                this.mnemomicFormat = "SHR\tV{0:X1},V{0:X1}";
                this.usedX = this.usedY = true;

                this.v[y] >>= 1;
                this.v[x] = this.v[y];
            }
            else
            {
                this.mnemomicFormat = "SHR\tV{0:X1}";
                this.usedX = true;

                this.v[x] >>= 1;
            }

            this.v[0xf] = (byte)(this.v[x] & 0x1);
        }

        private void SUBN(int x, int y)
        {
            this.mnemomicFormat = "SUBN\tV{0:X1},V{1:X1}";
            this.usedX = this.usedY = true;

            this.v[0xf] = (byte)(this.v[x] > this.v[y] ? 0 : 1);
            this.v[x] = (byte)(this.v[y] - this.v[x]);
        }

        private void SHL(int x, int y)
        {
            this.v[0xf] = (byte)((this.v[x] & 0x80) == 0 ? 0 : 1);

            // https://github.com/Chromatophore/HP48-Superchip#8xy6--8xye
            // Bit shifts X register by 1, VIP: shifts Y by one and places in X, HP48-SC: ignores Y field, shifts X
            if (this.emulating == EmulationType.ComsmacVip)
            {
                this.mnemomicFormat = "SHL\tV{0:X1},V{1:X1}";
                this.usedX = this.usedY = true;

                this.v[y] <<= 1;
                this.v[x] = this.v[y];
            }
            else
            {
                this.mnemomicFormat = "SHL\tV{0:X1}";
                this.usedX = true;

                this.v[x] <<= 1;
            }
        }

        private void SNE(int x, int y)
        {
            this.mnemomicFormat = "SNE\tV{0:X1},V{1:X1}";
            this.usedX = this.usedY = true;

            if (this.v[x] != this.v[y])
            {
                this.pc += 2;
            }
        }

        private void LD_I(short nnn)
        {
            this.mnemomicFormat = "LD\tI,#{0:X3}";
            this.usedAddress = true;

            this.i = nnn;
        }

        private void JP_V0(int x, short nnn)
        {
            this.mnemomicFormat = "JP\t[V0],#{0:X3}";
            this.usedAddress = true;

            // https://github.com/Chromatophore/HP48-Superchip#bnnn
            // Sets PC to address NNN + v0 -
            //  VIP: correctly jumps based on v0
            //  HP48 -SC: reads highest nibble of address to select
            //      register to apply to address (high nibble pulls double duty)
            var register = this.emulating == EmulationType.HP48 ? x : 0;
            this.pc = (short)(this.v[register] + nnn);
        }

        private void RND(int x, byte nn)
        {
            this.mnemomicFormat = "RND\tV{0:X1},#{1:X2}";
            this.usedOperand = this.usedX = true;
            this.v[x] = (byte)(this.randomNumbers.Next(byte.MaxValue) & nn);
        }

        private void XDRW(int x, int y)
        {
            this.mnemomicFormat = "XDRW V{0:X1},V{1:X1}";
            this.usedX = this.usedY = true;
            this.VerifyRunningHp48();
            this.Draw(x, y, 16, 16);
        }

        private void DRW(int x, int y, int n)
        {
            this.mnemomicFormat = "DRW\tV{0:X1},V{1:X1},#{2:X1}";
            this.usedX = this.usedY = this.usedN = true;
            this.Draw(x, y, 8, n);
        }

        private void SKP(int x)
        {
            this.mnemomicFormat = "SKP\tV{0:X1}";
            this.usedX = true;

            if (this.keyboard.IsKeyPressed(this.v[x]))
            {
                this.pc += 2;
            }
        }

        private void SKNP(int x)
        {
            this.mnemomicFormat = "SKNP\tV{0:X1}";
            this.usedX = true;

            if (!this.keyboard.IsKeyPressed(this.v[x]))
            {
                this.pc += 2;
            }
        }

        private void LD_Vx_II(int x)
        {
            this.mnemomicFormat = "LD\tV{0:X1},[I]";
            this.usedX = true;

            Array.Copy(this.memory, this.i, this.v, 0, x + 1);

            // https://github.com/Chromatophore/HP48-Superchip#fx55--fx65
            // Saves/Loads registers up to X at I pointer - VIP: increases I, HP48-SC: I remains static
            if (this.compatibility || (this.emulating == EmulationType.ComsmacVip))
            {
                this.i += (short)(x + 1);
            }
        }

        private void LD_II_Vx(int x)
        {
            this.mnemomicFormat = "LD\t[I],V{0:X1}";
            this.usedX = true;

            Array.Copy(this.v, 0, this.memory, this.i, x + 1);

            // https://github.com/Chromatophore/HP48-Superchip#fx55--fx65
            // Saves/Loads registers up to X at I pointer - VIP: increases I, HP48-SC: I remains static
            if (this.compatibility || (this.emulating == EmulationType.ComsmacVip))
            {
                this.i += (short)(x + 1);
            }
        }

        private void LD_B_Vx(int x)
        {
            this.mnemomicFormat = "LD\tB,V{0:X1}";
            this.usedX = true;

            var content = this.v[x];
            this.memory[this.i] = (byte)(content / 100);
            this.memory[this.i + 1] = (byte)((content / 10) % 10);
            this.memory[this.i + 2] = (byte)((content % 100) % 10);
        }

        private void LD_F_Vx(int x)
        {
            this.mnemomicFormat = "LD\tF,V{0:X1}";
            this.usedX = true;
            this.i = (short)(StandardFontOffset + (5 * this.v[x]));
        }

        private void LD_HF_Vx(int x)
        {
            this.mnemomicFormat = "LD\tHF,V{0:X1}";
            this.usedX = true;
            this.i = (short)(HighFontOffset + (10 * this.v[x]));
        }

        private void ADD_I_Vx(int x)
        {
            this.mnemomicFormat = "ADD\tI,V{0:X1}";
            this.usedX = true;

            // From wikipedia entry on CHIP-8:
            // VF is set to 1 when there is a range overflow (I+VX>0xFFF), and to 0
            // when there isn't. This is an undocumented feature of the CHIP-8 and used by the Spacefight 2091! game
            var sum = this.i + this.v[x];
            this.v[0xf] = (byte)(sum > 0xfff ? 1 : 0);

            this.i += this.v[x];
        }

        private void LD_ST_Vx(int x)
        {
            this.mnemomicFormat = "LD\tST,V{0:X1}";
            this.usedX = true;
            this.soundTimer = this.v[x];
        }

        private void LD_DT_Vx(int x)
        {
            this.mnemomicFormat = "LD\tDT,V{0:X1}";
            this.usedX = true;
            this.delayTimer = this.v[x];
        }

        private void LD_Vx_K(int x)
        {
            this.mnemomicFormat = "LD\tV{0:X1},K";
            this.usedX = true;
            this.waitingForKeyPress = true;
            this.waitingForKeyPressRegister = x;
        }

        private void LD_Vx_DT(int x)
        {
            this.mnemomicFormat = "LD\tV{0:X1},DT";
            this.usedX = true;
            this.v[x] = this.delayTimer;
        }

        ////

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

                Array.Copy(bytes, headerLength, this.memory, offset, size - headerLength);
            }
        }

        private void Draw(int x, int y, int width, int height)
        {
            var hits = this.display.Draw(this.memory, this.i, this.v[x], this.v[y], width, height);
            this.v[0xf] = (byte)hits;
            this.drawNeeded = true;
        }

        private void VerifyRunningHp48()
        {
            if (this.emulating < EmulationType.HP48)
            {
                throw new IllegalInstructionException(this.opcode, "Illegal when not running in HP-48 mode");
            }
        }
    }
}
