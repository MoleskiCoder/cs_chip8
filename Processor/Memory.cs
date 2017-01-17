namespace Processor
{
    using System;

    public class Memory : IMemory
    {
        private readonly byte[] bus;

        public Memory(int size)
        {
            this.bus = new byte[size];
        }

        public byte[] Bus
        {
            get
            {
                return this.bus;
            }
        }

        public byte Get(int address)
        {
            return this.bus[address];
        }

        public ushort GetWord(int address)
        {
            var high = this.Get(address);
            var low = this.Get(address + 1);
            return (ushort)((high << 8) + low);
        }

        public void Set(int address, byte value)
        {
            this.bus[address] = value;
        }

        public void Clear()
        {
            Array.Clear(this.bus, 0, this.bus.Length);
        }
    }
}
