namespace Processor
{
    public interface IMemory
    {
        byte[] Bus
        {
            get;
        }

        byte Get(int address);

        ushort GetWord(int address);

        void Set(int address, byte value);

        void Clear();
    }
}
