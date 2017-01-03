namespace Emulator
{
    internal class Program
    {
        public static void Main()
        {
            using (var controller = new Controller())
            {
                controller.Run();
            }
        }
    }
}
