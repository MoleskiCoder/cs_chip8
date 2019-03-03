// <copyright file="Debugger.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Processor
{
    using System;
    using System.Collections.Generic;

    public class Debugger : Controller
    {
        private readonly Dictionary<ushort, bool> breakpoints = new Dictionary<ushort, bool>();

        public Debugger(Chip8 processor, string game)
        : base(processor, game)
        {
        }

        public event EventHandler<EventArgs> Loading;

        public event EventHandler<EventArgs> Loaded;

        public event EventHandler<BreakpointHitEventArgs> BreakpointHit;

        public bool Stepping { get; private set; } = true;

        public bool Cycle { get; private set; } = false;

        public bool Framed { get; private set; } = false;

        public void Break() => this.Stepping = true;

        public void Continue() => this.Stepping = false;

        public void Step() => this.Cycle = true;

        public void AddBreakpoint(ushort address) => this.breakpoints[address] = false;

        public void AddTemporaryBreakpoint(ushort address) => this.breakpoints[address] = true;

        public void RemoveBreakpoint(ushort address) => this.breakpoints.Remove(address);

        public void RemoveBreakpoints() => this.breakpoints.Clear();

        public byte GetContents(ushort address) => this.Processor.Memory.Get(address);

        public byte GetRegisterContents(int register) => this.Processor.V[register];

        protected void OnLoading() => this.Loading?.Invoke(this, EventArgs.Empty);

        protected void OnLoaded() => this.Loaded?.Invoke(this, EventArgs.Empty);

        protected void OnBreakpointHit() => this.BreakpointHit?.Invoke(this, new BreakpointHitEventArgs());

        protected override void LoadContent()
        {
            this.OnLoading();
            try
            {
                base.LoadContent();
            }
            finally
            {
                this.OnLoaded();
            }
        }

        protected override void RunFrame()
        {
            this.Framed = true;
            try
            {
                if (this.Stepping)
                {
                    this.RunSingleStepFrame();
                }
                else
                {
                    this.RunNormalFrame();
                }
            }
            finally
            {
                this.Framed = false;
            }
        }

        private void RunNormalFrame()
        {
            for (var i = 0; !this.Stepping && (i < this.Processor.RuntimeConfiguration.CyclesPerFrame); ++i)
            {
                this.Stepping = this.CheckBreakpoint(this.Processor.PC);
                if (!this.Stepping)
                {
                    if (this.RunCycle())
                    {
                        break;
                    }

                    this.Processor.Step();
                }
            }
        }

        private void RunSingleStepFrame()
        {
            if (this.Cycle)
            {
                try
                {
                    this.RunCycle();
                    this.Processor.Step();
                }
                finally
                {
                    this.Cycle = false;
                }
            }
        }

        private bool CheckBreakpoint(ushort address)
        {
            if (this.breakpoints.TryGetValue(address, out var temporary))
            {
                if (temporary)
                {
                    this.breakpoints.Remove(address);
                }

                return true;
            }

            return false;
        }
    }
}
