// <copyright file="IGraphicsDevice.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Processor
{
    public interface IGraphicsDevice
    {
        bool[][] Graphics
        {
            get;
        }

        int NumberOfPlanes
        {
            get;
        }

        int PlaneMask
        {
            get;
            set;
        }

        int NumberOfColours
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

        void Initialise();

        int Draw(IMemory memory, int address, int drawX, int drawY, int width, int height);

        void CopyRow(int source, int destination);

        void CopyColumn(int source, int destination);

        void ClearRow(int row);

        void ClearColumn(int column);

        void Clear();

        void AllocateMemory();
    }
}
