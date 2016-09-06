namespace Worst_Nasus
{
    using System;
    using EloBuddy;
    using static Core;
    using EloBuddy.SDK.Events;
    using color = System.Drawing;

    class Program
    {
        public static readonly string ChampName = "Nasus";
        public static readonly string Creator = "Shulepin";
        public static readonly string Ver = "v1.0.0.0";       

        static void Main() => Loading.OnLoadingComplete += LoadingComplete;


        private static void LoadingComplete(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName == ChampName)
            {
                new Core().Load();
                DamageIndicator.Init(DmgCalc);
                Chat.Print("Worst " + ChampName + " loaded, made by " + Creator + " " + Ver, color.Color.Aqua);
            }
        }
    }
}