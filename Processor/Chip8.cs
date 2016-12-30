namespace Processor
{
    using System;
    using System.IO;
    using System.Media;

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

        private byte[] key = new byte[16];

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

        public void EmulateCycle()
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
                            Array.Clear(this.graphics, 0, ScreenWidth * ScreenHeight);
                            break;

                        case 0xee:  // 00EE     Flow        return;
                            this.pc = (short)this.stack[--this.sp & 0xF];
                            break;

                        default:
                            throw new IllegalInstructionException(opcode, "RCA1802 Call");
                    }

                    break;

                // Jump
                case 0x1000:        // 1NNN     Flow        goto NNN;
                    this.pc = nnn;
                    break;

                // Call
                case 0x2000:        // 2NNN     Flow        *(0xNNN)()
                    this.stack[this.sp++] = (ushort)this.pc;
                    this.pc = nnn;
                    break;

                // Conditional
                case 0x3000:        // 3XNN     Cond        if(Vx==NN)
                    if (this.v[x] == nn)
                    {
                        this.pc += 2;
                    }

                    break;

                // Conditional
                case 0x4000:        // 4XNN     Cond        if(Vx!=NN)
                    if (this.v[x] != nn)
                    {
                        this.pc += 2;
                    }

                    break;

                // Conditional
                case 0x5000:        // 5XNN     Cond        if(Vx==Vy)
                    if (this.v[x] == this.v[y])
                    {
                        this.pc += 2;
                    }

                    break;

                case 0x6000:        // 6XNN     Const       Vx = NN
                    this.v[x] = nn;
                    break;

                case 0x7000:        // 7XNN     Const       Vx += NN
                    this.v[x] += nn;
                    break;

                case 0x8000:
                    switch (n)
                    {
                        case 0x0:   // 8XY0     Assign      Vx=Vy
                            this.v[x] = this.v[y];
                            break;

                        case 0x1:   // 8XY1     BitOp       Vx=Vx|Vy
                            this.v[x] |= this.v[y];
                            break;

                        case 0x2:   // 8XY2     BitOp       Vx=Vx&Vy
                            this.v[x] &= this.v[y];
                            break;

                        case 0x3:   // 8XY3     BitOp       Vx=Vx^Vy
                            this.v[x] ^= this.v[y];
                            break;

                        case 0x4:   // 8XY4     Math        Vx += Vy
                            this.v[0xf] = (byte)(this.v[y] > (0xff - this.v[x]) ? 1 : 0);
                            this.v[x] += this.v[y];
                            break;

                        case 0x5:   // 8XY5     Math        Vx -= Vy
                            this.v[0xf] = (byte)(this.v[x] > this.v[y] ? 1 : 0);
                            this.v[x] -= this.v[y];
                            break;

                        case 0x6:   // 8XY6     Math        Vx >> 1
                            this.v[x] >>= 1;
                            this.v[0xf] = (byte)(this.v[x] & 0x1);
                            break;

                        case 0x7:   // 8XY7     Math        Vx=Vy-Vx
                            this.v[0xf] = (byte)(this.v[x] > this.v[y] ? 0 : 1);
                            this.v[x] = (byte)(this.v[y] - this.v[x]);
                            break;

                        case 0xe:   // 8XYE     Math        Vx << 1
                            this.v[0xf] = (byte)(this.v[x] & 0x80);
                            this.v[x] <<= 1;
                            break;

                        default:
                            throw new IllegalInstructionException(opcode);
                    }

                    break;

                case 0x9000:
                    switch (n)
                    {
                        case 0:     // 9XY0     Cond        if(Vx!=Vy)
                            if (this.v[x] != this.v[y])
                            {
                                this.pc += 2;
                            }

                            break;

                        default:
                            throw new IllegalInstructionException(opcode);
                    }

                    break;

                case 0xa000:        // ANNN     MEM         I = NNN
                    this.i = nnn;
                    break;

                case 0xB000:        // BNNN     Flow        PC=V0+NNN
                    this.pc = (short)(this.v[0] + nnn);
                    break;

                case 0xc000:        // CXNN     Rand        Vx=rand()&NN
                    this.v[x] = (byte)(this.randomNumbers.Next(byte.MaxValue) & nn);
                    break;

                case 0xd000:        // DXYN     Disp        draw(Vx,Vy,N)
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
                    }

                    this.drawNeeded = true;
                    break;

                case 0xe000:
                    switch (nn)
                    {
                        case 0x9E:  // EX9E     KeyOp       if(key()==Vx)
                            if (this.key[this.v[x]] != 0)
                            {
                                this.pc += 2;
                            }

                            break;

                        case 0xa1:  // EXA1     KeyOp       if(key()!=Vx)
                            if (this.key[this.v[x]] == 0)
                            {
                                this.pc += 2;
                            }

                            break;

                        default:
                            throw new IllegalInstructionException(opcode);
                    }

                    break;

                case 0xf000:
                    switch (nn)
                    {
                        case 0x07:  // FX07     Timer       Vx = get_delay()
                            this.v[x] = this.delayTimer;
                            break;

                        case 0x0a:  // FX0A     KeyOp       Vx = get_key()
                            break;

                        case 0x15:  // FX15     Timer       delay_timer(Vx)
                            this.delayTimer = this.v[x];
                            break;

                        case 0x18:  // FX18     Sound       sound_timer(Vx)
                            this.soundTimer = this.v[x];
                            break;

                        case 0x1e:  // FX1E     Mem         I +=Vx
                            this.i += this.v[x];
                            break;

                        case 0x29:  // FX29     Mem         I=sprite_addr[Vx]
                            this.i = (short)(5 * this.v[x]);
                            break;

                        case 0x33:  // FX33     BCD
                                    //                      set_BCD(Vx);
                                    //                      *(I+0)=BCD(3);
                                    //                      *(I+1)=BCD(2);
                                    //                      *(I+2)=BCD(1);
                            {
                                var content = this.v[x];
                                this.memory[this.i] = (byte)(content / 100);
                                this.memory[this.i + 1] = (byte)((content / 10) % 10);
                                this.memory[this.i + 2] = (byte)((content % 100) % 10);
                            }

                            break;

                        case 0x55:  // FX55     MEM         reg_dump(Vx,&I)
                            Array.Copy(this.v, 0, this.memory, this.i, x);
                            break;

                        case 0x65:  // FX65     MEM         reg_load(Vx,&I)
                            Array.Copy(this.memory, this.i, this.v, 0, x);
                            break;

                        default:
                            throw new IllegalInstructionException(opcode);
                    }

                    break;

                default:
                    throw new IllegalInstructionException(opcode);
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
