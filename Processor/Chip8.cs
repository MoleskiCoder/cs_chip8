namespace Processor
{
    using System;
    using System.IO;
    using System.Media;
    using System.Text;
    using Microsoft.Xna.Framework.Input;

    public class Chip8 : IDisposable
    {
        public static readonly int ScreenWidthLow = 64;
        public static readonly int ScreenHeightLow = 32;

        public static readonly int ScreenWidthHigh = 128;
        public static readonly int ScreenHeightHigh = 64;

        private static readonly int StandardFontOffset = 0x1b0;
        private static readonly int HighFontOffset = 0x110;

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

        private byte[] memory = new byte[4096];
        private byte[] v = new byte[16];
        private short i;
        private short pc;
        private bool[,] graphics;
        private byte delayTimer;
        private byte soundTimer;
        private ushort[] stack = new ushort[16];
        private ushort sp;

        private byte[] r = new byte[8]; // HP48 flags

        private bool drawNeeded;

        private bool soundPlaying = false;

        // CHIP-8 Keyboard layout
        //  1   2   3   C
        //  4   5   6   D
        //  7   8   9   E
        //  A   0   B   F
        private Keys[] key = new Keys[]
        {
                        Keys.X,
                
            Keys.D1,    Keys.D2,    Keys.D3,
            Keys.Q,     Keys.W,     Keys.E,
            Keys.A,     Keys.S,     Keys.D,

            Keys.Z,                 Keys.C,

                                                Keys.D4,
                                                Keys.R,
                                                Keys.F,
                                                Keys.V
        };

        private bool waitingForKeyPress;
        private int waitingForKeyPressRegister;

        private System.Random randomNumbers = new Random();

        private SoundPlayer soundPlayer = new SoundPlayer();

        private bool highResolution = false;
        private bool finished = false;

        private bool disposed = false;

        public event EventHandler<EventArgs> HighResolutionConfigured;

        public event EventHandler<EventArgs> LowResolutionConfigured;

        public bool Finished
        {
            get
            {
                return this.finished;
            }

            private set
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

        public bool[,] Graphics
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

            private set
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

        public int ScreenWidth
        {
            get
            {
                return this.HighResolution ? ScreenWidthHigh : ScreenWidthLow;
            }
        }

        public int ScreenHeight
        {
            get
            {
                return this.HighResolution ? ScreenHeightHigh : ScreenHeightLow;
            }
        }

        public int PixelSize
        {
            get
            {
                return this.HighResolution ? 5 : 10;
            }
        }

        public void Initialise()
        {
            this.Finished = false;
            this.DrawNeeded = false;
            this.HighResolution = false;

            this.pc = 0x200;     // Program counter starts at 0x200
            this.i = 0;          // Reset index register
            this.sp = 0;         // Reset stack pointer

            this.AllocateGraphicsMemory();

            // Clear display
            this.CLS();

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

            // Sound
            this.soundPlayer.SoundLocation = @"..\..\..\Sounds\beep.wav";
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

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.soundPlayer.Dispose();
                }

                this.disposed = true;
            }
        }

        protected void OnHighResolution()
        {
            this.HighResolutionConfigured?.Invoke(this, EventArgs.Empty);
        }

        protected void OnLowResolution()
        {
            this.LowResolutionConfigured?.Invoke(this, EventArgs.Empty);
        }

        private void WaitForKeyPress()
        {
            var state = Keyboard.GetState();
            for (int i = 0; i < this.key.Length; i++)
            {
                if (state.IsKeyDown(this.key[i]))
                {
                    this.waitingForKeyPress = false;
                    this.v[this.waitingForKeyPressRegister] = (byte)i;
                    break;
                }
            }
        }

        private void EmulateCycle()
        {
            var high = this.memory[this.pc];
            var low = this.memory[this.pc + 1];
            var opcode = (ushort)((high << 8) + low);
            var nnn = (short)(opcode & 0xfff);
            var nn = low;
            var n = low & 0xf;
            var x = high & 0xf;
            var y = (low & 0xf0) >> 4;

            System.Diagnostics.Debug.Write(string.Format("PC={0:x4}\t{1:x4}\t", this.pc, opcode));

            this.pc += 2;

            switch (opcode & 0xf000)
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
                                    throw new IllegalInstructionException(opcode, "RCA1802 Call");
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
                            this.SHR(x);
                            break;

                        case 0x7:   // 8XY7     Math        Vx=Vy-Vx
                            this.SUBN(x, y);
                            break;

                        case 0xe:   // 8XYE     Math        Vx << 1
                            this.SHL(x);
                            break;

                        default:
                            throw new IllegalInstructionException(opcode);
                    }

                    break;

                case 0x9000:
                    switch (n)
                    {
                        case 0:     // 9XY0     Cond        if(Vx!=Vy)
                            this.SNE(x, y);
                            break;

                        default:
                            throw new IllegalInstructionException(opcode);
                    }

                    break;

                case 0xa000:        // ANNN     MEM         I = NNN
                    this.LD_I(nnn);
                    break;

                case 0xB000:        // BNNN     Flow        PC=V0+NNN
                    this.JP_V0(nnn);
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
                            throw new IllegalInstructionException(opcode);
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
                            throw new IllegalInstructionException(opcode);
                    }

                    break;

                default:
                    throw new IllegalInstructionException(opcode);
            }

            System.Diagnostics.Debug.WriteLine(string.Empty);
        }

        ////

        // scdown n
        // Scroll the screen down n pixels. [Super-Chip]
        // This opcode delays until the start of a 60Hz clock cycle before drawing in low resolution mode.
        // (Use the delay timer to pace your games in high resolution mode.)
        // Code generated: 0x00Cn
        private void SCDOWN(int n)
        {
            System.Diagnostics.Debug.Write(string.Format("SCDOWN\t{0:X1}", n));
            for (int y = this.ScreenHeight - n - 1; y > 0; --y)
            {
                this.CopyGraphicsRow(y, y + n);
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
            System.Diagnostics.Debug.Write("* COMPATIBILITY");
            throw new InvalidOperationException("COMPATIBILITY unimplemented");
        }

        // scright
        // Scroll the screen right 4 pixels. [Super-Chip]
        // This opcode delays until the start of a 60Hz clock cycle before drawing in low resolution mode.
        // (Use the delay timer to pace your games in high resolution mode.)
        // Code generated: 0x00FB
        private void SCRIGHT()
        {
            System.Diagnostics.Debug.Write("SCRIGHT");

            var n = 4;
            for (int x = this.ScreenWidth - n - 1; x > 0; --x)
            {
                this.CopyGraphicsColumn(x, x + n);
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
            System.Diagnostics.Debug.Write("SCLEFT");

            var n = 4;
            for (int x = 0; x < this.ScreenWidth - n - 1; ++x)
            {
                this.CopyGraphicsColumn(x, x + n);
            }

            this.DrawNeeded = true;
        }

        // low
        // Low resolution (64×32) graphics mode (this is the default). [Super-Chip]
        // Code generated: 0x00FE
        private void LOW()
        {
            System.Diagnostics.Debug.Write("LOW");
            this.HighResolution = false;
            this.AllocateGraphicsMemory();
            this.OnLowResolution();
        }

        // high
        // High resolution (128×64) graphics mode. [Super-Chip]
        // Code generated: 0x00FF
        private void HIGH()
        {
            System.Diagnostics.Debug.Write("HIGH");
            this.HighResolution = true;
            this.AllocateGraphicsMemory();
            this.OnHighResolution();
        }

        // flags.save vX
        // Store the values of registers v0 to vX into the "flags" registers (this means something in the
        // HP48 implementation). (X < 8) [Super-Chip]
        // Code generated: 0xFX75
        private void LD_R_Vx(int x)
        {
            System.Diagnostics.Debug.Write(string.Format("LD\tR,V{0:X1}", x));
            for (int i = 0; i <= x; ++i)
            {
                this.r[i] = this.v[i];
            }
        }

        // flags.restore vX
        // Read the values of registers v0 to vX from the "flags" registers (this means something in the
        // HP48 implementation). (X < 8) [Super-Chip]
        // Code generated: 0xFX85
        private void LD_Vx_R(int x)
        {
            System.Diagnostics.Debug.Write(string.Format("LD\tV{0:X1},R", x));
            for (int i = 0; i <= x; ++i)
            {
                this.v[i] = this.r[i];
            }
        }

        // exit
        // This opcode is used to terminate the chip8run program. It causes the chip8run program to exit
        // with a successful exit status. [Super-Chip]
        // Code generated: 0x00FD.
        private void EXIT()
        {
            System.Diagnostics.Debug.Write("EXIT");
            this.Finished = true;
        }

        ////

        private void CLS()
        {
            System.Diagnostics.Debug.Write("CLS");
            Array.Clear(this.graphics, 0, this.ScreenWidth * this.ScreenHeight);
        }

        private void RET()
        {
            System.Diagnostics.Debug.Write("RET");
            this.pc = (short)this.stack[--this.sp & 0xF];
        }

        private void JP(short nnn)
        {
            System.Diagnostics.Debug.Write(string.Format("JP\t{0:X3}", nnn));
            this.pc = nnn;
        }

        private void CALL(short nnn)
        {
            System.Diagnostics.Debug.Write(string.Format("CALL\t{0:X3}", nnn));
            this.stack[this.sp++] = (ushort)this.pc;
            this.pc = nnn;
        }

        private void SE(int x, byte nn)
        {
            System.Diagnostics.Debug.Write(string.Format("SE\tV{0:X1},#{1:X2}", x, nn));
            if (this.v[x] == nn)
            {
                this.pc += 2;
            }
        }

        private void SNE(int x, byte nn)
        {
            System.Diagnostics.Debug.Write(string.Format("SNE\tV{0:X1},#{1:X2}", x, nn));
            if (this.v[x] != nn)
            {
                this.pc += 2;
            }
        }

        private void SE(int x, int y)
        {
            System.Diagnostics.Debug.Write(string.Format("SE\tV{0:X1},V{1:X1}", x, y));
            if (this.v[x] == this.v[y])
            {
                this.pc += 2;
            }
        }

        private void LD(int x, byte nn)
        {
            System.Diagnostics.Debug.Write(string.Format("LD\tV{0:X1},#{1:X2}", x, nn));
            this.v[x] = nn;
        }

        private void ADD(int x, byte nn)
        {
            System.Diagnostics.Debug.Write(string.Format("ADD\tV{0:X1},#{1:X2}", x, nn));
            this.v[x] += nn;
        }

        private void LD(int x, int y)
        {
            System.Diagnostics.Debug.Write(string.Format("LD\tV{0:X1},V{1:X1}", x, y));
            this.v[x] = this.v[y];
        }

        private void OR(int x, int y)
        {
            System.Diagnostics.Debug.Write(string.Format("OR\tV{0:X1},V{1:X1}", x, y));
            this.v[x] |= this.v[y];
        }

        private void AND(int x, int y)
        {
            System.Diagnostics.Debug.Write(string.Format("AND\tV{0:X1},V{1:X1}", x, y));
            this.v[x] &= this.v[y];
        }

        private void XOR(int x, int y)
        {
            System.Diagnostics.Debug.Write(string.Format("XOR\tV{0:X1},V{1:X1}", x, y));
            this.v[x] ^= this.v[y];
        }

        private void ADD(int x, int y)
        {
            System.Diagnostics.Debug.Write(string.Format("ADD\tV{0:X1},V{1:X1}", x, y));
            this.v[0xf] = (byte)(this.v[y] > (0xff - this.v[x]) ? 1 : 0);
            this.v[x] += this.v[y];
        }

        private void SUB(int x, int y)
        {
            System.Diagnostics.Debug.Write(string.Format("SUB\tV{0:X1},V{1:X1}", x, y));
            this.v[0xf] = (byte)(this.v[y] > (0xff - this.v[x]) ? 1 : 0);
            this.v[0xf] = (byte)(this.v[x] > this.v[y] ? 1 : 0);
            this.v[x] -= this.v[y];
        }

        private void SHR(int x)
        {
            System.Diagnostics.Debug.Write(string.Format("SHR\tV{0:X1}", x));
            this.v[x] >>= 1;
            this.v[0xf] = (byte)(this.v[x] & 0x1);
        }

        private void SUBN(int x, int y)
        {
            System.Diagnostics.Debug.Write(string.Format("SUBN\tV{0:X1},V{1:X1}", x, y));
            this.v[0xf] = (byte)(this.v[x] > this.v[y] ? 0 : 1);
            this.v[x] = (byte)(this.v[y] - this.v[x]);
        }

        private void SHL(int x)
        {
            System.Diagnostics.Debug.Write(string.Format("SHL\tV{0:X1}", x));
            this.v[0xf] = (byte)(this.v[x] & 0x80);
            this.v[x] <<= 1;
        }

        private void SNE(int x, int y)
        {
            System.Diagnostics.Debug.Write(string.Format("SNE\tV{0:X1},V{1:X1}", x, y));
            if (this.v[x] != this.v[y])
            {
                this.pc += 2;
            }
        }

        private void LD_I(short nnn)
        {
            System.Diagnostics.Debug.Write(string.Format("LD\tI,#{0:X3}", nnn));
            this.i = nnn;
        }

        private void JP_V0(short nnn)
        {
            System.Diagnostics.Debug.Write(string.Format("JP\t[V0],#{0:X3}", nnn));
            this.pc = (short)(this.v[0] + nnn);
        }

        private void RND(int x, byte nn)
        {
            System.Diagnostics.Debug.Write(string.Format("RND\tV{0:X1},#{1:X2}", x, nn));
            this.v[x] = (byte)(this.randomNumbers.Next(byte.MaxValue) & nn);
        }

        private void XDRW(int x, int y)
        {
            System.Diagnostics.Debug.Write(string.Format("XDRW V{0:X1},V{1:X1}", x, y));
            this.Draw(x, y, 16, 16, 2);
        }

        private void DRW(int x, int y, int n)
        {
            System.Diagnostics.Debug.Write(string.Format("DRW\tV{0:X1},V{1:X1},#{2:X1}", x, y, n));
            this.Draw(x, y, 8, n, 1);
        }

        private void SKP(int x)
        {
            System.Diagnostics.Debug.Write(string.Format("SKP\tV{0:X1}", x));
            if (Keyboard.GetState().IsKeyDown(this.key[this.v[x]]))
            {
                this.pc += 2;
            }
        }

        private void SKNP(int x)
        {
            System.Diagnostics.Debug.Write(string.Format("SKNP\tV{0:X1}", x));
            if (!Keyboard.GetState().IsKeyDown(this.key[this.v[x]]))
            {
                this.pc += 2;
            }
        }

        private void LD_Vx_II(int x)
        {
            System.Diagnostics.Debug.Write(string.Format("LD\tV{0:X1},[I]", x));
            Array.Copy(this.memory, this.i, this.v, 0, x + 1);
        }

        private void LD_II_Vx(int x)
        {
            System.Diagnostics.Debug.Write(string.Format("LD\t[I],V{0:X1}", x));
            Array.Copy(this.v, 0, this.memory, this.i, x + 1);
        }

        private void LD_B_Vx(int x)
        {
            System.Diagnostics.Debug.Write(string.Format("LD\tB,V{0:X1}", x));
            var content = this.v[x];
            this.memory[this.i] = (byte)(content / 100);
            this.memory[this.i + 1] = (byte)((content / 10) % 10);
            this.memory[this.i + 2] = (byte)((content % 100) % 10);
        }

        private void LD_F_Vx(int x)
        {
            System.Diagnostics.Debug.Write(string.Format("LD\tF,V{0:X1}", x));
            this.i = (short)(StandardFontOffset + (5 * this.v[x]));
        }

        private void LD_HF_Vx(int x)
        {
            System.Diagnostics.Debug.Write(string.Format("LD\tHF,V{0:X1}", x));
            this.i = (short)(HighFontOffset + (10 * this.v[x]));
        }

        private void ADD_I_Vx(int x)
        {
            System.Diagnostics.Debug.Write(string.Format("ADD\tI,V{0:X1}", x));

            // From wikipedia entry on CHIP-8:
            // VF is set to 1 when there is a range overflow (I+VX>0xFFF), and to 0
            // when there isn't. This is an undocumented feature of the CHIP-8 and used by the Spacefight 2091! game
            var sum = this.i + this.v[x];
            this.v[0xf] = (byte)(sum > 0xfff ? 1 : 0);

            this.i += this.v[x];
        }

        private void LD_ST_Vx(int x)
        {
            System.Diagnostics.Debug.Write(string.Format("LD\tST,V{0:X1}", x));
            this.soundTimer = this.v[x];
        }

        private void LD_DT_Vx(int x)
        {
            System.Diagnostics.Debug.Write(string.Format("LD\tDT,V{0:X1}", x));
            this.delayTimer = this.v[x];
        }

        private void LD_Vx_K(int x)
        {
            System.Diagnostics.Debug.Write(string.Format("LD\tV{0:X1},K", x));
            this.waitingForKeyPress = true;
            this.waitingForKeyPressRegister = x;
        }

        private void LD_Vx_DT(int x)
        {
            System.Diagnostics.Debug.Write(string.Format("LD\tV{0:X1},DT", x));
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
                if (!this.soundPlaying)
                {
                    this.soundPlayer.PlayLooping();
                    this.soundPlaying = true;
                }

                --this.soundTimer;
            }
            else
            {
                if (this.soundPlaying)
                {
                    this.soundPlayer.Stop();
                    this.soundPlaying = false;
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

        private void AllocateGraphicsMemory()
        {
            this.graphics = new bool[this.ScreenWidth, this.ScreenHeight];
        }

        private void Draw(int x, int y, int width, int height, int bytesPerRow)
        {
            var drawX = this.v[x];
            var drawY = this.v[y];

            this.v[0xf] = 0;

            for (var row = 0; row < height; row++)
            {
                var cellY = drawY + row;
                var pixelAddress = this.i + (row * bytesPerRow);
                for (var column = 0; column < width; column++)
                {
                    var high = column > 7;
                    var pixelMemory = this.memory[pixelAddress + (high ? 1 : 0)];
                    var pixel = (pixelMemory & (0x80 >> (column & 0x7))) != 0;
                    if (pixel)
                    {
                        var cellX = drawX + column;
                        if ((cellX < this.ScreenWidth) && (cellY < this.ScreenHeight))
                        {
                            if (this.graphics[cellX, cellY])
                            {
                                this.v[0xf] = 1;
                            }

                            this.graphics[cellX, cellY] ^= true;
                        }
                    }
                }
            }

            this.drawNeeded = true;
        }

        private void CopyGraphicsRow(int from, int to)
        {
            for (int x = 0; x < this.ScreenWidth; ++x)
            {
                this.graphics[x, from] = this.graphics[x, to];
            }
        }

        private void CopyGraphicsColumn(int from, int to)
        {
            for (int y = 0; y < this.ScreenHeight; ++y)
            {
                this.graphics[from, y] = this.graphics[to, y];
            }
        }
    }
}
