using EloBuddy;
using Worst_Ashe;


namespace Worst_Ase
{
    using System;
    using EloBuddy.SDK.Events;
    using color = System.Drawing;
    

    class Program
    {
        static void Main()
        {
            Loading.OnLoadingComplete += LoadingComplete;
        }

        private static void LoadingComplete(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName == "Ashe")
            {
                new Core().Load();
                Chat.Print("Worst Ashe loaded_1.0.0.2", color.Color.Red);
            }
        }
    }
}
