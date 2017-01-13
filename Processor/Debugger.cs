namespace Processor
{
    using System;
    using System.Collections.Generic;

    public class Debugger : Controller
    {
        private readonly Dictionary<short, bool> breakpoints = new Dictionary<short, bool>();
        private bool stepping = true;
        private bool cycle = false;
        private bool framed = false;

        public Debugger(Chip8 processor, string game)
        : base(processor, game)
        {
        }

        public event EventHandler<EventArgs> Loading;

        public event EventHandler<EventArgs> Loaded;

        public event EventHandler<BreakpointHitEventArgs> BreakpointHit;

        public bool Stepping
        {
            get
            {
                return this.stepping;
            }
        }

        public bool Cycle
        {
            get
            {
                return this.cycle;
            }
        }

        public bool Framed
        {
            get
            {
                return this.framed;
            }
        }

        public void Break()
        {
            this.stepping = true;
        }

        public void Continue()
        {
            this.stepping = false;
        }

        public void Step()
        {
            this.cycle = true;
        }

        public void AddBreakpoint(short address)
        {
            this.breakpoints[address] = false;
        }

        public void AddTemporaryBreakpoint(short address)
        {
            this.breakpoints[address] = true;
        }

        public void RemoveBreakpoint(short address)
        {
            this.breakpoints.Remove(address);
        }

        public void RemoveBreakpoints()
        {
            this.breakpoints.Clear();
        }

        public byte GetContents(short address)
        {
            return this.Processor.Memory[address];
        }

        public byte GetRegisterContents(int register)
        {
            return this.Processor.V[register];
        }

        protected void OnLoading()
        {
            var handler = this.Loading;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
       }

        protected void OnLoaded()
        {
            var handler = this.Loaded;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected void OnBreakpointHit()
        {
            var handler = this.BreakpointHit;
            if (handler != null)
            {
                handler(this, new BreakpointHitEventArgs());
            }
        }

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
            this.framed = true;
            try
            {
                if (this.stepping)
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
                this.framed = false;
            }
        }

        private void RunNormalFrame()
        {
            for (int i = 0; !this.stepping && (i < this.Processor.CyclesPerFrame); ++i)
            {
                this.stepping = this.CheckBreakpoint(this.Processor.PC);
                if (!this.stepping)
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
            if (this.cycle)
            {
                try
                {
                    this.RunCycle();
                    this.Processor.Step();
                }
                finally
                {
                    this.cycle = false;
                }
            }
        }

        private bool CheckBreakpoint(short address)
        {
            bool temporary;
            if (this.breakpoints.TryGetValue(address, out temporary))
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
