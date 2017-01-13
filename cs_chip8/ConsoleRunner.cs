namespace Emulator
{
    using System;
    using Processor;

    internal class ConsoleRunner : IDisposable
    {
        private readonly Processor.Controller controller;
        private bool disposed = false;

        public ConsoleRunner(Chip8 processor, string game)
        {
            this.controller = new Processor.Controller(processor, game);
        }

        public void Run()
        {
            this.controller.Run();
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (this.controller != null)
                    {
                        this.controller.Dispose();
                    }
                }

                this.disposed = true;
            }
        }
    }
}
