// <copyright file="IMemory.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

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
