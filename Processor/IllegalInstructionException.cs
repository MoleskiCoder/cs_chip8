namespace Processor
{
    using System;

    [Serializable]
    public class IllegalInstructionException : Exception
    {
        private ushort instructionCode;

        public IllegalInstructionException(ushort instructionCode)
        : this(instructionCode, "Illegal Chip8 instruction")
        {
        }

        public IllegalInstructionException(ushort instructionCode, string message)
        : base(message)
        {
            this.instructionCode = instructionCode;
        }

        public ushort InstructionCode
        {
            get
            {
                return this.instructionCode;
            }
        }
    }
}
