using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace TMFModMenu.Menu
{

    public class MenuRenderer
    {

        public void Draw(
            SpriteBatch spriteBatch,
            SpriteFont font,
            MenuManager menu)
        {


            Vector2 position =
                new Vector2(100, 100);



            spriteBatch.DrawString(
                font,
                "TMF MOD MENU",
                position,
                Color.Red);



            position.Y += 40;



            string[] options =
                menu.GetOptions();


            bool[] states =
                menu.GetStates();



            for (int i = 0; i < options.Length; i++)
            {

                string text =
                    options[i];


                if (states[i])
                    text += " [ON]";
                else
                    text += " [OFF]";



                if (i == menu.GetSelected())
                {
                    text =
                    "> " + text;
                }



                spriteBatch.DrawString(
                    font,
                    text,
                    position,
                    Color.White);



                position.Y += 30;

            }

        }

    }

}