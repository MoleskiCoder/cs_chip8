namespace Processor
{
    public class Configuration
    {
        private ProcessorLevel type = ProcessorLevel.Chip8;
        private bool allowMisalignedOpcodes = false;
        private int cyclesPerFrame = 13;
        private ushort startAddress = 0x200;
        private ushort loadAddress = 0x200;
        private int memorySize = 4096;
        private int graphicPlanes = 1;

        public Configuration()
        {
        }

        public ProcessorLevel Type
        {
            get
            {
                return this.type;
            }

            set
            {
                this.type = value;
            }
        }

        public bool AllowMisalignedOpcodes
        {
            get
            {
                return this.allowMisalignedOpcodes;
            }

            set
            {
                this.allowMisalignedOpcodes = value;
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
        public int CyclesPerFrame
        {
            get
            {
                return this.cyclesPerFrame;
            }

            set
            {
                this.cyclesPerFrame = value;
            }
        }

        public ushort StartAddress
        {
            get
            {
                return this.startAddress;
            }

            set
            {
                this.startAddress = value;
            }
        }

        public ushort LoadAddress
        {
            get
            {
                return this.loadAddress;
            }

            set
            {
                this.loadAddress = value;
            }
        }

        public int MemorySize
        {
            get
            {
                return this.memorySize;
            }

            set
            {
                this.memorySize = value;
            }
        }

        public int GraphicPlanes
        {
            get
            {
                return this.graphicPlanes;
            }

            set
            {
                this.graphicPlanes = value;
            }
        }

        public static Configuration BuildSuperChipConfiguration()
        {
            var configuration = new Configuration();
            configuration.Type = ProcessorLevel.SuperChip;
            configuration.CyclesPerFrame = 22;
            return configuration;
        }

        public static Configuration BuildXoChipConfiguration()
        {
            var configuration = BuildSuperChipConfiguration();
            configuration.Type = ProcessorLevel.XoChip;
            configuration.MemorySize = 0x10000;
            configuration.GraphicPlanes = 2;
            return configuration;
        }

        public Chip8 BuildProcessor()
        {
            switch (this.Type)
            {
                case ProcessorLevel.Chip8:
                    return new Chip8(new Memory(this.MemorySize), new MonoGameKeyboard(), new BitmappedGraphics(this.GraphicPlanes), this);

                case ProcessorLevel.SuperChip:
                    return new Schip(new Memory(this.MemorySize), new MonoGameKeyboard(), new BitmappedGraphics(this.GraphicPlanes), this);

                case ProcessorLevel.XoChip:
                    return new XoChip(new Memory(this.MemorySize), new MonoGameKeyboard(), new BitmappedGraphics(this.GraphicPlanes), this);

                default:
                    return null;
            }
        }
    }
}
