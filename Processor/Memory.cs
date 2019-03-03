// <copyright file="Memory.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Processor
{
    using System;

    public class Memory : IMemory
    {
        public Memory(int size) => this.Bus = new byte[size];

        public byte[] Bus { get; }

        public byte Get(int address) => this.Bus[address];

        public ushort GetWord(int address)
        {
            var high = this.Get(address);
            var low = this.Get(address + 1);
            return (ushort)((high << 8) + low);
        }

        public void Set(int address, byte value) => this.Bus[address] = value;

        public void Clear() => Array.Clear(this.Bus, 0, this.Bus.Length);
    }
}
