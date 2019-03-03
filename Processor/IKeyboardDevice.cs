// <copyright file="IKeyboardDevice.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Processor
{
    public interface IKeyboardDevice
    {
        bool CheckKeyPress(out int key);

        bool IsKeyPressed(int key);
    }
}
