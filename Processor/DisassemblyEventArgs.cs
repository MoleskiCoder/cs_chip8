// <copyright file="DisassemblyEventArgs.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Processor
{
    using System;

    public class DisassemblyEventArgs : EventArgs
    {
        public DisassemblyEventArgs(string output) => this.Output = output;

        public string Output { get; }
    }
}
