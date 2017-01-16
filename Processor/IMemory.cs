namespace Processor
{
    public interface IMemory
    {
        byte[] Bus
        {
            get;
        }

        byte Get(int address);

        void Set(int address, byte value);

        void Clear();
    }
}
