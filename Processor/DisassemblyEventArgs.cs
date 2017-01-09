namespace Processor
{
    using System;

    public class DisassemblyEventArgs : EventArgs
    {
        private string output;

        public DisassemblyEventArgs(string output)
        {
            this.output = output;
        }

        public string Output
        {
            get
            {
                return this.output;
            }
        }
    }
}
