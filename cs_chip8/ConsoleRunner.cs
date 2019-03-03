// <copyright file="ConsoleRunner.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Emulator
{
    using System;
    using Processor;

    internal class ConsoleRunner : IDisposable
    {
        private readonly Processor.Controller controller;
        private bool disposed = false;

        public ConsoleRunner(Chip8 processor, string game) => this.controller = new Processor.Controller(processor, game);

        public void Run() => this.controller.Run();

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.controller?.Dispose();
                }

                this.disposed = true;
            }
        }
    }
}
