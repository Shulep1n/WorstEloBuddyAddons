using System;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using Microsoft.Win32;

namespace Worst_KogMaw
{
    internal class Core
    {


        private Menu General, ConfigQ, ConfigW, ConfigE, ConfigR, Farmed, Draw, Summ, SkinChanger, AutoLevell, Itemss;
        public Spell.Skillshot Q, E, R;
        private Item Botrk, Bil, Youmu;
        public static int[] SpellLevels;
        public Spell.Active heal, barrier, cleanse;
        public Spell.Active W;
        public float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;

        public bool attackNow = true;

        public AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }

        public static bool Combo { get { return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo); } }
        public static bool Farm { get { return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass); } }
        public static bool LaneClear { get { return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear); } }


        public void Load()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 1000, SkillShotType.Linear, 250, 1650, 70);
            Q.AllowedCollisionCount = 0;
            W = new Spell.Active(SpellSlot.W, 720);
            E = new Spell.Skillshot(SpellSlot.E, 1200, SkillShotType.Linear, 500, 1400, 120);
            E.AllowedCollisionCount = int.MaxValue;
            R = new Spell.Skillshot(SpellSlot.R, 1800, SkillShotType.Circular, 1200, int.MaxValue, 120);
            R.AllowedCollisionCount = int.MaxValue;

            General = MainMenu.AddMenu("Worst KogMaw", "Worst KogMaw");

            SkinChanger = General.AddSubMenu("Skin Changer");
            SkinChanger.Add("skinEnable", new CheckBox("Enable"));
            SkinChanger.Add("skinID",
                new ComboBox("Current Skin", 8, "0", "1", "2", "3",
                    "4", "5", "6", "7", "8"));

            AutoLevell = General.AddSubMenu("Auto Level Up");
            AutoLevell.Add("ALEnable", new CheckBox("Enable"));
            AutoLevell.Add("ALBox", new ComboBox("Level Up Mode", 0, "R>W>Q>E", "R>W>E>Q"));
            AutoLevell.Add("Delay", new Slider("Max. delay value", 500, 0, 10000));

            ConfigQ = General.AddSubMenu("Q Config", "Qconfig");
            ConfigQ.Add("autoQ", new CheckBox("Auto Q"));
            ConfigQ.Add("harrasQ", new CheckBox("Harass Q"));

            ConfigW = General.AddSubMenu("W Config", "Wconfig");
            ConfigW.Add("autoW", new CheckBox("Auto W"));
            ConfigW.Add("harasW", new CheckBox("Harass W on max range"));

            ConfigE = General.AddSubMenu("E Config", "Econfig");
            ConfigE.Add("autoE", new CheckBox("Auto E"));
            ConfigE.Add("HarrasE", new CheckBox("Harass E"));
            ConfigE.Add("AGC", new CheckBox("AntiGapcloserE"));

            ConfigR = General.AddSubMenu("R Option");
            ConfigR.Add("autoR", new CheckBox("Auto R"));
            ConfigR.Add("RmaxHp", new Slider("Target max % HP", 50, 0, 100));
            ConfigR.Add("comboStack", new Slider("Max combo stack R", 2, 0, 10));
            ConfigR.Add("harasStack", new Slider("Max haras stack R", 1, 0, 10));
            ConfigR.Add("Rcc", new CheckBox("R cc"));
            ConfigR.Add("Rslow", new CheckBox("R slow"));
            ConfigR.Add("Raoe", new CheckBox("R aoe"));
            ConfigR.Add("Raa", new CheckBox("R only out off AA range"));

            Farmed = General.AddSubMenu("Farm");
            Farmed.Add("farmW", new CheckBox("LaneClear W"));
            Farmed.Add("farmE", new CheckBox("LaneClear E"));
            Farmed.Add("LCminions", new Slider("LaneClear minimum minions", 2, 0, 10));
            Farmed.Add("Mana", new Slider("LaneClear Mana", 80, 0, 100));
            Farmed.Add("jungleW", new CheckBox("Jungle Clear W"));
            Farmed.Add("jungleE", new CheckBox("Jungle Clear E"));

            Summ = General.AddSubMenu("Summ. spells");
            Summ.AddGroupLabel("Heal");
            Summ.Add("Heal", new CheckBox("Enable heal"));
            Summ.Add("AllyHeal", new CheckBox("Enable heal for Ally"));
            Summ.Add("healhp", new Slider("Use Heal if HP < (%) ", 25, 0, 100));

            Itemss = General.AddSubMenu("Items");
            Itemss.Add("BilCombo", new CheckBox("Use Bilgewater Cutlass"));
            Itemss.Add("YoumuCombo", new CheckBox("Use Youmuu's Ghostblade"));
            Itemss.Add("BotrkCombo", new CheckBox("Use BOTRK"));
            Itemss.Add("MyBotrkHp", new Slider("Min. HP for using BOTRK (%)", 50, 0, 100));
            Itemss.Add("EnBotrkHp", new Slider("Min. Enemy HP for using BOTRK (%)", 50, 0, 100));

            Draw = General.AddSubMenu("Draw");
            Draw.Add("ComboInfo", new CheckBox("R killable info"));
            Draw.Add("qRange", new CheckBox("Q range"));
            Draw.Add("wRange", new CheckBox("W range"));
            Draw.Add("eRange", new CheckBox("E range"));
            Draw.Add("rRange", new CheckBox("R range"));
            Draw.Add("onlyRdy", new CheckBox("Draw only ready spells"));


            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Orbwalker.OnPreAttack += PreAttack;
            Orbwalker.OnPostAttack += AfterAttack;
            Gapcloser.OnGapcloser += AntiGap;
        }

        private void OnDraw(EventArgs args)
        {
            if (Draw["ComboInfo"].Cast<CheckBox>().CurrentValue && R.IsLearned)
            {
                var combo = "haras";

                foreach (var enemy in EntityManager.Heroes.Enemies.Where(enemy => enemy.IsValidTarget()))
                {
                    var rDmg = EloBuddy.Player.Instance.GetSpellDamage(enemy, SpellSlot.R);
                    if (rDmg > enemy.Health)
                    {
                        combo = "Kill R";
                        drawText(combo, enemy, System.Drawing.Color.GreenYellow);
                    }
                    else
                    {
                        combo = (int)(enemy.Health / rDmg) + "R";
                        drawText(combo, enemy, System.Drawing.Color.Red);
                    }

                }
            }

            if (Draw["qRange"].Cast<CheckBox>().CurrentValue)
            {
                if (Draw["onlyRdy"].Cast<CheckBox>().CurrentValue)
                {
                    if (Q.IsReady())
                    {
                        new Circle()
                        {
                            BorderWidth = 2,
                            Color = Color.Cyan,
                            Radius = Q.Range
                        }.Draw(Player.Position);
                    }
                }
                else
                {
                    new Circle()
                    {
                        BorderWidth = 2,
                        Color = Color.Cyan,
                        Radius = Q.Range
                    }.Draw(Player.Position);
                }
            }

            if (Draw["eRange"].Cast<CheckBox>().CurrentValue)
            {
                if (Draw["onlyRdy"].Cast<CheckBox>().CurrentValue)
                {
                    if (E.IsReady())
                    {
                        new Circle()
                        {
                            BorderWidth = 2,
                            Color = Color.Yellow,
                            Radius = E.Range
                        }.Draw(Player.Position);
                    }
                }
                else
                {
                    new Circle()
                    {
                        BorderWidth = 2,
                        Color = Color.Yellow,
                        Radius = E.Range
                    }.Draw(Player.Position);
                }
            }

            if (Draw["rRange"].Cast<CheckBox>().CurrentValue)
            {
                if (Draw["onlyRdy"].Cast<CheckBox>().CurrentValue)
                {
                    if (R.IsReady())
                    {
                        if (R.Level == 1)
                        {
                            new Circle()
                            {
                                BorderWidth = 2,
                                Color = Color.Gray,
                                Radius = 1200
                            }.Draw(Player.Position);
                        }
                    }
                }
                else
                {
                    if (R.Level == 1)
                    {
                        new Circle()
                        {
                            BorderWidth = 2,
                            Color = Color.Gray,
                            Radius = 1200
                        }.Draw(Player.Position);
                    }
                }
            }

            if (Draw["rRange"].Cast<CheckBox>().CurrentValue)
            {
                if (Draw["onlyRdy"].Cast<CheckBox>().CurrentValue)
                {
                    if (R.IsReady())
                    {
                        if (R.Level == 2)
                        {
                            new Circle()
                            {
                                BorderWidth = 2,
                                Color = Color.Gray,
                                Radius = 1500
                            }.Draw(Player.Position);
                        }
                    }
                }
                else
                {
                    if (R.Level == 2)
                    {
                        new Circle()
                        {
                            BorderWidth = 2,
                            Color = Color.Gray,
                            Radius = 1500
                        }.Draw(Player.Position);
                    }
                }

                if (Draw["rRange"].Cast<CheckBox>().CurrentValue)
                {
                    if (Draw["onlyRdy"].Cast<CheckBox>().CurrentValue)
                    {
                        if (R.IsReady())
                        {
                            if (R.Level == 3)
                            {
                                new Circle()
                                {
                                    BorderWidth = 2,
                                    Color = Color.Gray,
                                    Radius = 1800
                                }.Draw(Player.Position);
                            }
                        }
                    }
                    else
                    {
                        if (R.Level == 3)
                        {
                            new Circle()
                            {
                                BorderWidth = 2,
                                Color = Color.Gray,
                                Radius = 1800
                            }.Draw(Player.Position);
                        }
                    }
                }
            }
        }

        private void drawText(string msg, AIHeroClient Hero, System.Drawing.Color color)
        {
            var wts = Drawing.WorldToScreen(Hero.Position);
            Drawing.DrawText(wts[0] - (msg.Length) * 5, wts[1], color, msg);
        }

        private void AntiGap(AIHeroClient sender, Gapcloser.GapcloserEventArgs gapcloserEventArgs)
        {
            if (ConfigE["AGC"].Cast<CheckBox>().CurrentValue && E.IsReady() && Player.Mana > RMANA + EMANA)
            {
                var target = (AIHeroClient)gapcloserEventArgs.Sender;

                if (target.IsValidTarget(E.Range))
                {
                    E.Cast(target);
                }
            }
        }

        private void AfterAttack(AttackableUnit target, EventArgs args)
        {
            if (LaneClear && W.IsReady() && Player.ManaPercent > Farmed["Mana"].Cast<Slider>().CurrentValue)
            {
                var minions = EntityManager.MinionsAndMonsters.EnemyMinions.Where(x => x.IsValidTarget(W.Range));

                if (minions.Count() >= Farmed["LCminions"].Cast<Slider>().CurrentValue)
                {
                    if (Farmed["farmW"].Cast<CheckBox>().CurrentValue && minions.Count() > 1)
                        W.Cast();
                }
            }
        }


        private void PreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            attackNow = false;
        }

        private void OnUpdate(EventArgs args)
        {
            SetMana();
            Jungle();
            //Sum();
            Skin();
            AutoLevel();
            Items();


            if (Q.IsReady() && ConfigQ["autoQ"].Cast<CheckBox>().CurrentValue)
            {
                LogicQ();
            }
            if (W.IsReady() && ConfigW["autoW"].Cast<CheckBox>().CurrentValue)
            {
                LogicW();
            }
            if (E.IsReady() && ConfigE["autoE"].Cast<CheckBox>().CurrentValue)
            {
                LogicE();
                LogicElane();
            }
            if (R.IsReady())
            {
                LogicR();
            }

        }

        private void Skin()
        {
            if (SkinChanger["skinEnable"].Cast<CheckBox>().CurrentValue)
            {
                Player.SetSkinId(SkinChanger["skinID"].Cast<ComboBox>().CurrentValue);
            }
        }

        private void LogicQ()
        {
            var t = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

            if (t.IsValidTarget())
            {
                var qDam = EloBuddy.Player.Instance.GetSpellDamage(t, SpellSlot.Q);
                var eDam = EloBuddy.Player.Instance.GetSpellDamage(t, SpellSlot.E);

                if (t.IsValidTarget(W.Range) && qDam + eDam > t.Health)
                    Q.Cast(t);
                else if (Combo && Player.Mana > RMANA + QMANA * 2 + EMANA)  //combo
                    Q.Cast(t);
                else if
                    ((Farm && Player.Mana > RMANA + EMANA + QMANA * 2 + WMANA) && ConfigQ["harrasQ"].Cast<CheckBox>().CurrentValue && !Player.IsUnderTurret())  //Farm
                    Q.Cast(t);
                else if ((Combo || Farm) && Player.Mana > RMANA + QMANA + EMANA)
                {
                    foreach (var enemy in EntityManager.Heroes.Enemies.Where(enemy => enemy.IsValidTarget(Q.Range) && !Orbwalker.CanMove))
                        Q.Cast(t);
                }
            }
        }

        private void LogicW()
        {
            if (Player.CountEnemiesInRange(W.Range) > 0)
            {
                if (Combo)
                    W.Cast();
                else if (Farm && ConfigW["harasW"].Cast<CheckBox>().CurrentValue &&
                         Player.CountEnemiesInRange(Player.AttackRange) > 0)
                    W.Cast();
            }
        }

        private void LogicE()
        {
            var t = TargetSelector.GetTarget(E.Range, DamageType.Physical);
            if (t.IsValidTarget())
            {
                var qDam = EloBuddy.Player.Instance.GetSpellDamage(t, SpellSlot.Q);
                var eDam = EloBuddy.Player.Instance.GetSpellDamage(t, SpellSlot.E);

                if (eDam > t.Health)
                    E.Cast(t);
                else if (eDam + qDam > t.Health && Q.IsReady())
                    E.Cast(t);
                else if (Combo && Player.Mana > RMANA + WMANA + EMANA + QMANA)
                    E.Cast(t);
                else if (Farm && ConfigE["HarrasE"].Cast<CheckBox>().CurrentValue &&
                         Player.Mana > RMANA + WMANA + EMANA + QMANA + EMANA)
                    E.Cast(t);
                else if ((Combo || Farm) && Player.Mana > RMANA + EMANA + WMANA)
                {
                    foreach (
                        var enemy in
                            EntityManager.Heroes.Enemies.Where(
                                enemy => enemy.IsValidTarget(E.Range) && !Orbwalker.CanMove))
                        E.Cast(enemy);
                }
            }
        }

        private void LogicElane()
        {
            var t = Orbwalker.GetTarget() as Obj_AI_Minion;
            if (t.IsValidTarget())
            {
                if (LaneClear && Player.ManaPercent > Farmed["Mana"].Cast<Slider>().CurrentValue &&
                         Farmed["farmE"].Cast<CheckBox>().CurrentValue && Player.Mana > RMANA + EMANA)
                {
                    var minion = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                        Player.Position, E.Range);
                    var farmPosition = EntityManager.MinionsAndMonsters.GetLineFarmLocation(minion, E.Width, 1200);

                    if (farmPosition.HitNumber >= Farmed["LCminions"].Cast<Slider>().CurrentValue)
                        E.Cast(farmPosition.CastPosition);
                }
            }
        }

        private void LogicR()
        {
            if (ConfigR["autoR"].Cast<CheckBox>().CurrentValue)
            {
                var target = TargetSelector.GetTarget(R.Range, DamageType.Magical);

                if (target.IsValidTarget(R.Range) &&
                    target.HealthPercent < ConfigR["RmaxHp"].Cast<Slider>().CurrentValue)
                {
                    if (ConfigR["Raa"].Cast<CheckBox>().CurrentValue && Player.IsInAutoAttackRange(target))
                        return;

                    var harasStack = ConfigR["harasStack"].Cast<Slider>().CurrentValue;
                    var comboStack = ConfigR["comboStack"].Cast<Slider>().CurrentValue;

                    var countR = GetRStacks();

                    var Rdmg = EloBuddy.Player.Instance.GetSpellDamage(target, SpellSlot.R);
                    Rdmg = Rdmg + target.CountAlliesInRange(500) * Rdmg;

                    if (Rdmg > target.Health)
                        R.Cast(target);
                    else if (Combo && Rdmg * 2 > target.Health && Player.Mana > RMANA * 3)
                        R.Cast(target);
                    else if (countR < comboStack + 2 && Player.Mana > RMANA * 3)
                    {
                        foreach (
                            var enemy in
                                EntityManager.Heroes.Enemies.Where(
                                    enemy => enemy.IsValidTarget(R.Range) && !Orbwalker.CanMove))
                        {
                            R.Cast(enemy);
                        }

                        if (target.HasBuffOfType(BuffType.Slow) && ConfigR["Rslow"].Cast<CheckBox>().CurrentValue &&
                            countR < comboStack + 1 && Player.Mana > RMANA + WMANA + EMANA + QMANA)
                            R.Cast(target);
                        else if (Combo && countR < comboStack && Player.Mana > RMANA + WMANA + EMANA + QMANA)
                            R.Cast(target);
                        else if (Farm && countR < harasStack && Player.Mana > RMANA + WMANA + EMANA + QMANA)
                            R.Cast(target);
                    }
                }
            }
        }

        private void Jungle()
        {
            if (LaneClear && Player.Mana > RMANA + QMANA)
            {
                var mobs = EntityManager.MinionsAndMonsters.Monsters.Where(x => x.IsMonster && x.IsValidTarget(650));

                if (mobs.Count() > 0)
                {
                    if (E.IsReady() && Farmed["jungleE"].Cast<CheckBox>().CurrentValue)
                    {
                        E.Cast(mobs.First());

                    }
                    else if (W.IsReady() && Farmed["jungleW"].Cast<CheckBox>().CurrentValue)
                    {
                        W.Cast();
                    }
                }
            }
        }

        private int GetRStacks()
        {
            foreach (var buff in Player.Buffs)
            {
                if (buff.Name == "kogmawlivingartillerycost")
                    return buff.Count;
            }
            return 0;
        }

        private void SetMana()
        {
            if (Player.HealthPercent < 20)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
                return;
            }

            QMANA = Q.Handle.SData.Mana;
            WMANA = W.Handle.SData.Mana;
            EMANA = E.Handle.SData.Mana;

            if (!R.IsReady())
                RMANA = QMANA - Player.PARRegenRate * Q.Handle.Cooldown;
            else
                RMANA = R.Handle.SData.Mana;
        }

        private void Sum()
        {
            var slotheal = Player.GetSpellSlotFromName("summonerheal");
            if (slotheal != SpellSlot.Unknown)
            {
                heal = new Spell.Active(slotheal, 600);
            }
            var slotbar = Player.GetSpellSlotFromName("summonerbarrier");
            if (slotbar != SpellSlot.Unknown)
            {
                barrier = new Spell.Active(slotbar, 0);
            }
            var slotboost = Player.GetSpellSlotFromName("summonerboost");
            if (slotboost != SpellSlot.Unknown)
            {
                cleanse = new Spell.Active(slotboost, 0);
            }

            if (Summ["Heal"].Cast<CheckBox>().CurrentValue)
            {
                if (heal.IsReady() && Player.CountEnemiesInRange(800) >= 1 &&
                    Player.HealthPercent <= Summ["healhp"].Cast<Slider>().CurrentValue)
                {
                    heal.Cast();
                }
            }
            if (Summ["allyHeal"].Cast<CheckBox>().CurrentValue)
            {
                foreach (var ally in EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsValidTarget(800) && x.HealthPercent <= Summ["healhp"].Cast<Slider>().CurrentValue))
                {
                    if (heal.IsReady() && ally.CountEnemiesInRange(800) >= 1)
                    {
                        heal.Cast();
                    }
                }              
            }
        }

        public void LevelUp(SpellSlot slot)
        {
            EloBuddy.SDK.Core.DelayAction(() =>
            {
                EloBuddy.Player.Instance.Spellbook.LevelSpell(slot);
            }, new Random().Next(0, AutoLevell["Delay"].Cast<Slider>().CurrentValue));
        }

        private void AutoLevel()
        {
            if (!AutoLevell["ALEnable"].Cast<CheckBox>().CurrentValue)
            {
                return;
            }

            if (Player.ChampionName == "KogMaw")
            {
                if (AutoLevell["ALBox"].Cast<ComboBox>().CurrentValue == 0)
                {
                    SpellLevels = new int[] { 2, 1, 3, 2, 2, 4, 2, 3, 1, 1, 4, 1, 1, 3, 3, 4, 3, 3 };
                }
                if (AutoLevell["ALBox"].Cast<ComboBox>().CurrentValue == 1)
                {
                    SpellLevels = new int[] { 2, 3, 2, 1, 2, 4, 3, 2, 3, 3, 4, 3, 3, 1, 1, 4, 1, 1 };
                }
            }
            var qLevel = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level;
            var wLevel = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level;
            var eLevel = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level;
            var rLevel = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level;

            if (qLevel + wLevel + eLevel + rLevel >= ObjectManager.Player.Level)
            {
                return;
            }

            var level = new int[] { 0, 0, 0, 0 };
            for (var i = 0; i < ObjectManager.Player.Level; i++)
            {
                level[SpellLevels[i] - 1] = level[SpellLevels[i] - 1] + 1;
            }

            if (qLevel < level[0])
            {
                LevelUp(SpellSlot.Q);
            }
            if (wLevel < level[1])
            {
                LevelUp(SpellSlot.W);
            }

            if (eLevel < level[2])
            {
                LevelUp(SpellSlot.E);
            }

            if (rLevel < level[3])
            {
                LevelUp(SpellSlot.R);
            }
        }
        private void Items()
        {
            Botrk = new Item(3153, 450f);
            Bil = new Item(3144, 450f);
            Youmu = new Item(3142);

            if (Combo)
            {
                var target = Orbwalker.GetTarget() as AIHeroClient;

                if (Itemss["BotrkCombo"].Cast<CheckBox>().CurrentValue && Botrk.IsReady() && Botrk.IsOwned() &&
                    target.IsValidTarget(450) &&
                    (Player.HealthPercent <= Itemss["MyBotrkHp"].Cast<Slider>().CurrentValue ||
                     target.HealthPercent < Itemss["EnBotrkHp"].Cast<Slider>().CurrentValue))
                {
                    Botrk.Cast(target);
                }

                if (Itemss["BilCombo"].Cast<CheckBox>().CurrentValue && Bil.IsOwned() && Bil.IsReady() &&
                    target.IsValidTarget(450))
                {
                    Bil.Cast(target);
                }

                if (Itemss["YoumuCombo"].Cast<CheckBox>().CurrentValue && Youmu.IsOwned() && Youmu.IsReady() &&
                    target.IsValidTarget())
                {
                    Youmu.Cast();
                }
            }
        }
    }
}
