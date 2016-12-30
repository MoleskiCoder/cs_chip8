using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cs_chip8
{
    class Program
    {
        static void Main(string[] args)
        {
            var controller = new Controller();
            controller.Emulate();
        }
    }
}
