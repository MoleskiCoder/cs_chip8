﻿namespace Emulator
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Processor;

    internal class ConsoleDebugger : IDisposable
    {
        private readonly Processor.Debugger debugger;

        private readonly ManualResetEvent debuggerAvailable;
        private readonly ManualResetEvent stepping;

        private bool finished = false;

        private bool disposed = false;

        public ConsoleDebugger(EmulationType machine, string game)
        {
            this.debugger = new Debugger(machine, game);
            this.debuggerAvailable = new ManualResetEvent(false);
            this.stepping = new ManualResetEvent(true);
        }

        public void Run()
        {
            Console.CancelKeyPress += this.Console_CancelKeyPress;
            this.debugger.Loaded += this.Debugger_Loaded;
            this.debugger.Exiting += this.Debugger_Exiting;
            this.debugger.BreakpointHit += this.Debugger_BreakpointHit;

            var task = Task.Run(() =>
            {
                this.debugger.Run();
            });

            this.debuggerAvailable.WaitOne();

            while (!this.finished)
            {
                Console.Write("debug: ");
                var input = Console.ReadLine();
                switch (input)
                {
                    case "continue":
                        this.stepping.Reset();
                        this.debugger.Continue();
                        break;

                    case "step":
                        this.debugger.Step();
                        break;

                    default:
                        Console.WriteLine("Unknown command.");
                        break;
                }

                this.stepping.WaitOne();
            }
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
                    if (this.debuggerAvailable != null)
                    {
                        this.debuggerAvailable.Dispose();
                    }

                    if (this.debugger != null)
                    {
                        this.debugger.Dispose();
                    }
                }

                this.disposed = true;
            }
        }

        private void Debugger_Loaded(object sender, EventArgs e)
        {
            this.debuggerAvailable.Set();
        }

        private void Debugger_Exiting(object sender, EventArgs e)
        {
            this.finished = true;
            this.stepping.Set();
        }

        private void Debugger_BreakpointHit(object sender, BreakpointHitEventArgs e)
        {
            this.stepping.Set();
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            this.debugger.Break();
            this.stepping.Set();
        }
    }
}
