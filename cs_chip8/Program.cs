namespace Emulator
{
    using Processor;

    internal class Program
    {
        public static void Main()
        {
            ////var processor = new Chip8(new Memory(4096), new MonoGameKeyboard(), new BitmappedGraphics(1));
            var processor = new Schip(new Memory(4096), new MonoGameKeyboard(), new BitmappedGraphics(1));
            ////var processor = new XoChip(new Memory(0x10000). new MonoGameKeyboard(), new BitmappedGraphics(2));

            ////var game = @"GAMES\PONG.ch8";

            ////var game = @"SGAMES\ALIEN";
            ////var game = @"SGAMES\ANT";
            ////var game = @"SGAMES\BLINKY";
            ////var game = @"SGAMES\CAR";
            ////var game = @"SGAMES\DRAGON1";
            ////var game = @"SGAMES\DRAGON2";
            ////var game = @"SGAMES\FIELD";
            ////var game = @"SGAMES\JOUST23";
            ////var game = @"SGAMES\MAZE";
            ////var game = @"SGAMES\MINES";
            ////var game = @"SGAMES\PIPER";
            ////var game = @"SGAMES\RACE";
            var game = @"SGAMES\SPACEFIG";
            ////var game = @"SGAMES\SQUARE";
            ////var game = @"SGAMES\TEST";
            ////var game = @"SGAMES\UBOAT";
            ////var game = @"SGAMES\WORM3";

            ////var game = @"XOGAMES\xotest.ch8";

            ////var game = @"Chip-8 Pack\SuperChip Test Programs\BMP Viewer - Flip-8 logo [Newsdee, 2006].ch8";
            ////var game = @"Chip-8 Pack\SuperChip Test Programs\BMP Viewer - Kyori (SC example) [Hap, 2005].ch8";
            ////var game = @"Chip-8 Pack\SuperChip Test Programs\BMP Viewer - Let's Chip-8! [Koppepan, 2005].ch8";
            ////var game = @"Chip-8 Pack\SuperChip Test Programs\BMP Viewer (16x16 tiles) (MAME) [IQ_132].ch8";
            ////var game = @"Chip-8 Pack\SuperChip Test Programs\BMP Viewer (Google) [IQ_132].ch8";
            ////var game = @"Chip-8 Pack\SuperChip Test Programs\Emutest [Hap, 2006].ch8";
            ////var game = @"Chip-8 Pack\SuperChip Test Programs\Font Test [Newsdee, 2006].ch8";
            ////var game = @"Chip-8 Pack\SuperChip Test Programs\Hex Mixt.ch8";
            ////var game = @"Chip-8 Pack\SuperChip Test Programs\Line Demo.ch8";
            ////var game = @"Chip-8 Pack\SuperChip Test Programs\SC Test.ch8";
            ////var game = @"Chip-8 Pack\SuperChip Test Programs\SCHIP Test [iq_132].ch8";
            ////var game = @"Chip-8 Pack\SuperChip Test Programs\Scroll Test (modified) [Garstyciuks].ch8";
            ////var game = @"Chip-8 Pack\SuperChip Test Programs\Scroll Test.ch8";
            ////var game = @"Chip-8 Pack\SuperChip Test Programs\SuperChip Test.ch8";
            ////var game = @"Chip-8 Pack\SuperChip Test Programs\Test128.ch8";

            ////var game = @"Chip-8 Pack\SuperChip Demos\Super Particle Demo [zeroZshadow, 2008].ch8";
            ////var game = @"Chip-8 Pack\SuperChip Demos\SCSerpinski [Sergey Naydenov, 2010].ch8";

            using (var runner = new ConsoleRunner(processor, game))
            {
                runner.Run();
            }
        }
    }
}
