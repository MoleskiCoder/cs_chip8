namespace Processor
{
    using System;

    [Serializable]
    public class IllegalInstructionException : Exception
    {
        private ushort opcode;

        public IllegalInstructionException(ushort opcode)
        : this(opcode, "Illegal Chip8 instruction")
        {
            this.opcode = opcode;
        }

        public IllegalInstructionException(ushort opcode, string message)
        : base(message)
        {
            this.opcode = opcode;
        }

        public ushort OpCode
        {
            get
            {
                return this.opcode;
            }
        }
    }
}
