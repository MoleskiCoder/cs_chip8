// <copyright file="Configuration.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Processor
{
    public class Configuration
    {
        public Configuration()
        {
        }

        public ProcessorLevel Type { get; set; } = ProcessorLevel.Chip8;

        public bool AllowMisalignedOpcodes { get; set; } = false;

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
        public int CyclesPerFrame { get; set; } = 13;

        public ushort StartAddress { get; set; } = 0x200;

        public ushort LoadAddress { get; set; } = 0x200;

        public int MemorySize { get; set; } = 4096;

        public int GraphicPlanes { get; set; } = 1;

        public static Configuration BuildSuperChipConfiguration()
        {
            var configuration = new Configuration
            {
                Type = ProcessorLevel.SuperChip,
                CyclesPerFrame = 22,
            };
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
