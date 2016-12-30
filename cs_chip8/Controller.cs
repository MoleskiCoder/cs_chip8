namespace cs_chip8
{
    using System;
    using System.Timers;
    using Processor;

    class Controller
    {
        private Chip8 myChip8;
        private Timer jiffyTimer;

        public Controller()
        {
            this.myChip8 = new Chip8();
            this.jiffyTimer = new Timer();
        }

        public void Emulate()
        {
            this.jiffyTimer.Elapsed += this.JiffyTimer_Elapsed;
            this.jiffyTimer.Interval = 1000.0 / 60.0;
            this.jiffyTimer.Start();

            // Set up render system and register input callbacks
            SetupGraphics();
            SetupInput();
 
            // Initialize the Chip8 system and load the game into the memory  
            myChip8.Initialize();
            myChip8.LoadGame("PONG");
 
            // Emulation loop
            for (;;)
            {
                // Emulate one cycle
                myChip8.EmulateCycle();

                // If the draw flag is set, update the screen
                if (myChip8.DrawNeeded)
                {
                    DrawGraphics();
                }
 
                // Store key press state (Press and Release)
                myChip8.SetKeys();	
            }
        }

        private void JiffyTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.myChip8.UpdateTimers();
        }

        private void SetupGraphics()
        {
        }

        private void SetupInput()
        {
        }

        private void DrawGraphics()
        {
            this.myChip8.DrawNeeded = false;
        }
    }
}
