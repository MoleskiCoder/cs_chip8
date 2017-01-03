namespace Processor
{
    using System;

    [Serializable]
    public class IllegalInstructionException : Exception
    {
        private ushort opCode;

        public IllegalInstructionException(ushort opCode)
        : this(opCode, "Illegal Chip8 instruction")
        {
            this.opCode = opCode;
        }

        public IllegalInstructionException(ushort opCode, string message)
        : base(message)
        {
            this.opCode = opCode;
        }

        public ushort OpCode
        {
            get
            {
                return this.opCode;
            }
        }
    }
}
