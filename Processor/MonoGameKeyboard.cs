// <copyright file="MonoGameKeyboard.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace Processor
{
    using Microsoft.Xna.Framework.Input;

    public class MonoGameKeyboard : IKeyboardDevice
    {
        // CHIP-8 Keyboard layout
        //  1   2   3   C
        //  4   5   6   D
        //  7   8   9   E
        //  A   0   B   F
        private readonly Keys[] mapping = new Keys[]
        {
                        Keys.X,

            Keys.D1,    Keys.D2,    Keys.D3,
            Keys.Q,     Keys.W,     Keys.E,
            Keys.A,     Keys.S,     Keys.D,

            Keys.Z,                 Keys.C,

                                                Keys.D4,
                                                Keys.R,
                                                Keys.F,
                                                Keys.V,
        };

        public bool CheckKeyPress(out int key)
        {
            key = -1;
            var state = Keyboard.GetState();
            for (var idx = 0; idx < this.mapping.Length; idx++)
            {
                if (state.IsKeyDown(this.mapping[idx]))
                {
                    key = idx;
                    return true;
                }
            }

            return false;
        }

        public bool IsKeyPressed(int key) => Keyboard.GetState().IsKeyDown(this.mapping[key]);
    }
}
