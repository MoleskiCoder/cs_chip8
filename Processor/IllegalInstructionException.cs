// <copyright file="IllegalInstructionException.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Processor
{
    using System;

    [Serializable]
    public class IllegalInstructionException : Exception
    {
        public IllegalInstructionException(ushort instructionCode)
        : this(instructionCode, "Illegal Chip8 instruction")
        {
        }

        public IllegalInstructionException(ushort instructionCode, string message)
        : base(message) => this.InstructionCode = instructionCode;

        public IllegalInstructionException()
        {
        }

        public IllegalInstructionException(string message)
        : base(message)
        {
        }

        public IllegalInstructionException(string message, Exception innerException)
        : base(message, innerException)
        {
        }

        protected IllegalInstructionException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) => throw new NotImplementedException();

        public ushort InstructionCode { get; }
    }
}
