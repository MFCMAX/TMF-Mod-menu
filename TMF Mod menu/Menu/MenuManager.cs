using Microsoft.Xna.Framework.Input;

namespace TMFModMenu.Menu
{
    public class MenuManager
    {

        public bool Open = false;


        private int selected = 0;


        private string[] options =
        {
            "God Mode",
            "Speed",
            "Jump Boost",
            "Teleport",
            "Give Items"
        };


        private bool[] enabled =
        {
            false,
            false,
            false,
            false,
            false
        };



        public void HandleInput()
        {

            KeyboardState key =
                Keyboard.GetState();



            if (key.IsKeyDown(Keys.L))
            {
                Open = !Open;
            }



            if (!Open)
                return;



            if (key.IsKeyDown(Keys.Down))
            {
                selected++;

                if (selected >= options.Length)
                {
                    selected = 0;
                }
            }



            if (key.IsKeyDown(Keys.Up))
            {
                selected--;

                if (selected < 0)
                {
                    selected = options.Length - 1;
                }
            }



            if (key.IsKeyDown(Keys.Enter))
            {
                enabled[selected] =
                    !enabled[selected];
            }

        }



        public string[] GetOptions()
        {
            return options;
        }



        public bool[] GetStates()
        {
            return enabled;
        }



        public int GetSelected()
        {
            return selected;
        }


        public bool IsOpen()
        {
            return Open;
        }

    }
}