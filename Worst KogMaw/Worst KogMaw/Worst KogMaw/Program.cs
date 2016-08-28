using EloBuddy;

namespace Worst_KogMaw
{
    using System;
    using EloBuddy.SDK.Events;
    using color = System.Drawing;
    using static Core;

    class Program
    {
        static void Main()
        {
            Loading.OnLoadingComplete += LoadingComplete;
        }

        private static void LoadingComplete(EventArgs args)
        {
            if (EloBuddy.Player.Instance.ChampionName == "KogMaw")
            {
                new Core().Load();
                Chat.Print("Worst KogMaw loaded 1.0.0.3", color.Color.Red);
            }




        }
    }
}
