﻿namespace Processor
{
    public interface IGraphicsDevice
    {
        bool[] Graphics
        {
            get;
        }

        bool HighResolution
        {
            get;
            set;
        }

        bool LowResolution
        {
            get;
        }

        int Width
        {
            get;
        }

        int Height
        {
            get;
        }

        int Draw(byte[] memory, int address, int drawX, int drawY, int width, int height);

        void CopyRow(int source, int destination);

        void CopyColumn(int source, int destination);

        void ClearRow(int row);

        void ClearColumn(int column);

        void Clear();

        void AllocateMemory();
    }
}
