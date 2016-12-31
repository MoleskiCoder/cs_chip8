namespace Processor
{
    using System;
    using System.IO;
    using System.Media;
    using Microsoft.Xna.Framework.Input;

    public class Chip8 : IDisposable
    {
        public static readonly int ScreenWidth = 64;
        public static readonly int ScreenHeight = 32;

        private byte[] memory = new byte[4096];
        private byte[] v = new byte[16];
        private short i;
        private short pc;
        private bool[,] graphics = new bool[ScreenWidth, ScreenHeight];
        private byte delayTimer;
        private byte soundTimer;
        private ushort[] stack = new ushort[16];
        private ushort sp;

        private bool drawNeeded;

        private bool soundPlaying = false;

        private byte[] chip8FontSet =
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

        private bool disposed = false;

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

        public void Initialise()
        {
            //// Initialize registers and memory once

            this.pc = 0x200;     // Program counter starts at 0x200
            this.i = 0;          // Reset index register
            this.sp = 0;         // Reset stack pointer

            // Clear display
            Array.Clear(this.graphics, 0, ScreenWidth * ScreenHeight);

            // Clear stack
            Array.Clear(this.stack, 0, 16);

            // Clear registers V0-VF
            Array.Clear(this.v, 0, 16);

            // Clear memory
            Array.Clear(this.memory, 0, 4096);

            // Load fontset
            Array.Copy(this.chip8FontSet, this.memory, 16 * 5);

            // Reset timers
            this.delayTimer = this.soundTimer = 0;

            // Sound
            this.soundPlayer.SoundLocation = @"..\..\..\Sounds\beep.wav";
        }

        public void SetKeys()
        {
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

            this.pc += 2;

            switch (opcode & 0xf000)
            {
                case 0x0000:    // Call
                    switch (low)
                    {
                        case 0xe0:  // 00E0     Display     disp_clear()
                            this.CLS();
                            break;

                        case 0xee:  // 00EE     Flow        return;
                            this.RET();
                            break;

                        default:
                            throw new IllegalInstructionException(opcode, "RCA1802 Call");
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
                    this.DRW(x, y, n);
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
                            throw new IllegalInstructionException(opcode);
                    }

                    break;

                default:
                    throw new IllegalInstructionException(opcode);
            }
        }

        ////

        private void CLS()
        {
            Array.Clear(this.graphics, 0, ScreenWidth * ScreenHeight);
        }

        private void RET()
        {
            this.pc = (short)this.stack[--this.sp & 0xF];
        }

        private void JP(short nnn)
        {
            this.pc = nnn;
        }

        private void CALL(short nnn)
        {
            this.stack[this.sp++] = (ushort)this.pc;
            this.pc = nnn;
        }

        private void SE(int x, byte nn)
        {
            if (this.v[x] == nn)
            {
                this.pc += 2;
            }
        }

        private void SNE(int x, byte nn)
        {
            if (this.v[x] != nn)
            {
                this.pc += 2;
            }
        }

        private void SE(int x, int y)
        {
            if (this.v[x] == this.v[y])
            {
                this.pc += 2;
            }
        }

        private void LD(int x, byte nn)
        {
            this.v[x] = nn;
        }

        private void ADD(int x, byte nn)
        {
            this.v[x] += nn;
        }

        private void LD(int x, int y)
        {
            this.v[x] = this.v[y];
        }

        private void OR(int x, int y)
        {
            this.v[x] |= this.v[y];
        }

        private void AND(int x, int y)
        {
            this.v[x] &= this.v[y];
        }

        private void XOR(int x, int y)
        {
            this.v[x] ^= this.v[y];
        }

        private void ADD(int x, int y)
        {
            this.v[0xf] = (byte)(this.v[y] > (0xff - this.v[x]) ? 1 : 0);
            this.v[x] += this.v[y];
        }

        private void SUB(int x, int y)
        {
            this.v[0xf] = (byte)(this.v[x] > this.v[y] ? 1 : 0);
            this.v[x] -= this.v[y];
        }

        private void SHR(int x)
        {
            this.v[x] >>= 1;
            this.v[0xf] = (byte)(this.v[x] & 0x1);
        }

        private void SUBN(int x, int y)
        {
            this.v[0xf] = (byte)(this.v[x] > this.v[y] ? 0 : 1);
            this.v[x] = (byte)(this.v[y] - this.v[x]);
        }

        private void SHL(int x)
        {
            this.v[0xf] = (byte)(this.v[x] & 0x80);
            this.v[x] <<= 1;
        }

        private void SNE(int x, int y)
        {
            if (this.v[x] != this.v[y])
            {
                this.pc += 2;
            }
        }

        private void LD_I(short nnn)
        {
            this.i = nnn;
        }

        private void JP_V0(short nnn)
        {
            this.pc = (short)(this.v[0] + nnn);
        }

        private void RND(int x, byte nn)
        {
            this.v[x] = (byte)(this.randomNumbers.Next(byte.MaxValue) & nn);
        }

        private void DRW(int x, int y, int n)
        {
            var drawX = this.v[x];
            var drawY = this.v[y];
            var height = n;

            this.v[0xf] = 0;

            for (int row = 0; row < height; ++row)
            {
                var cellY = drawY + row;
                var pixel = this.memory[this.i + row];
                for (int column = 0; column < 8; ++column)
                {
                    var cellX = drawX + column;
                    if ((pixel & (0x80 >> column)) != 0)
                    {
                        if ((cellX < ScreenWidth) && (cellY < ScreenHeight))
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

        private void SKP(int x)
        {
            if (Keyboard.GetState().IsKeyDown(this.key[this.v[x]]))
            {
                this.pc += 2;
            }
        }

        private void SKNP(int x)
        {
            if (!Keyboard.GetState().IsKeyDown(this.key[this.v[x]]))
            {
                this.pc += 2;
            }
        }

        private void LD_Vx_II(int x)
        {
            Array.Copy(this.memory, this.i, this.v, 0, x + 1);
        }

        private void LD_II_Vx(int x)
        {
            Array.Copy(this.v, 0, this.memory, this.i, x + 1);
        }

        private void LD_B_Vx(int x)
        {
            var content = this.v[x];
            this.memory[this.i] = (byte)(content / 100);
            this.memory[this.i + 1] = (byte)((content / 10) % 10);
            this.memory[this.i + 2] = (byte)((content % 100) % 10);
        }

        private void LD_F_Vx(int x)
        {
            this.i = (short)(5 * this.v[x]);
        }

        private void ADD_I_Vx(int x)
        {
            this.i += this.v[x];
        }

        private void LD_ST_Vx(int x)
        {
            this.soundTimer = this.v[x];
        }

        private void LD_DT_Vx(int x)
        {
            this.delayTimer = this.v[x];
        }

        private void LD_Vx_K(int x)
        {
            this.waitingForKeyPress = true;
            this.waitingForKeyPressRegister = x;
        }

        private void LD_Vx_DT(int x)
        {
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
                if (size > this.memory.Length)
                {
                    throw new InvalidOperationException("File is too large");
                }

                using (var reader = new BinaryReader(file, new System.Text.UTF8Encoding(), true))
                {
                    reader.Read(this.memory, offset, (int)size);
                }
            }
        }
    }
}
