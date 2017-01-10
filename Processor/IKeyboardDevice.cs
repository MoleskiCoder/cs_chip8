namespace Processor
{
    public interface IKeyboardDevice
    {
        bool CheckKeyPress(out int key);

        bool IsKeyPressed(int key);
    }
}
