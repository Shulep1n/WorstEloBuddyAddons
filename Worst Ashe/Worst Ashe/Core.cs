using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;


namespace Worst_Ashe
{
    internal class Core
    {
        private Menu General, ConfigQ, ConfigE, ConfigR, Farmed, Harass, Draw, SkinChanger, Itemss, Summ, AutoLevell;
        public static int[] SpellLevels;
        private Spell.Active heal, barrier, cleanse;
        private Item Botrk, Bil, Youmu;
        private bool CastR = false, CastR2 = false;
        private Obj_AI_Base RTarget = null;
        public Spell.Active Q;
        public Spell.Skillshot W, E, R;
        private float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;

        public AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }

        public static bool Combo
        {
            get { return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo); }
        }

        public static bool Farm
        {
            get { return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass); }
        }

        public static bool LaneClear
        {
            get { return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear); }
        }

        

        public void Load()
        {
            General = MainMenu.AddMenu("Worst Ashe", "Worst Ashe");

            SkinChanger = General.AddSubMenu("Skin Changer");
            SkinChanger.Add("skinEnable", new CheckBox("Enable"));
            SkinChanger.Add("skinID",
                new ComboBox("Current Skin", 8, "Default Ashe", "Freljord Ashe", "Sherwood Forest Ashe", "Woad Ashe",
                    "Queen Ashe", "Amethyst Ashe", "Heartseeker Ashe", "Marauder Ashe", "PROJECT: Ashe"));

            AutoLevell = General.AddSubMenu("Auto Level Up");
            AutoLevell.Add("ALEnable", new CheckBox("Enable"));
            AutoLevell.Add("ALBox", new ComboBox("Level Up Mode", 1, "R>Q>W>E", "R>W>Q>E"));
            AutoLevell.Add("Delay", new Slider("Max. delay value", 500, 0, 10000));

            ConfigQ = General.AddSubMenu("Q Config");
            ConfigQ.Add("harasQ", new CheckBox("Harass Q"));

            ConfigE = General.AddSubMenu("E Config");
            ConfigE.Add("autoE", new CheckBox("Auto E"));
            ConfigE.Add("Eflash", new CheckBox("Use E against Flashes"));
            ConfigE.Add("EDragon", new KeyBind("Cast E to Dragon", false, KeyBind.BindTypes.HoldActive, 'U'));
            ConfigE.Add("EBaron", new KeyBind("Cast E to Baron", false, KeyBind.BindTypes.HoldActive, 'I'));

            ConfigR = General.AddSubMenu("R Config");
            ConfigR.Add("autoR", new CheckBox("Auto R"));
            ConfigR.Add("Rkscombo", new CheckBox("R KS combo R + W + AA"));
            ConfigR.Add("autoRaoe", new CheckBox("Auto R aoe"));
            ConfigR.Add("autoRinter", new CheckBox("Auto R OnPossibleToInterrupt"));
            ConfigR.Add("useR2", new KeyBind("R key target cast", false, KeyBind.BindTypes.HoldActive, 'Y'));
            ConfigR.Add("useR", new KeyBind("Semi-manual cast R key", false, KeyBind.BindTypes.HoldActive, 'T'));
            ConfigR.Add("Semi-manual", new ComboBox("Semi-manual MODE", 1, "LOW HP", "CLOSEST"));
            ConfigR.Add("GapCloser", new CheckBox("R GapCloser"));

            Harass = General.AddSubMenu("Harass");
            Harass.Add("haras", new CheckBox("Enable"));

            Farmed = General.AddSubMenu("Farm");
            Farmed.Add("farmQ", new CheckBox("Lane Clear Q"));
            Farmed.Add("farmW", new CheckBox("Lane Clear W"));
            Farmed.Add("Mana", new Slider("LaneClear Mana", 80, 0, 100));
            Farmed.Add("LCminions", new Slider("LaneClear minimum minions", 3, 0, 10));
            Farmed.Add("jungleQ", new CheckBox("Jungle Clear Q"));
            Farmed.Add("jungleW", new CheckBox("Jungle Clear W"));

            Itemss = General.AddSubMenu("Items");
            Itemss.Add("BilCombo", new CheckBox("Use Bilgewater Cutlass"));
            Itemss.Add("YoumuCombo", new CheckBox("Use Youmuu's Ghostblade"));
            Itemss.Add("BotrkCombo", new CheckBox("Use BOTRK"));
            Itemss.Add("MyBotrkHp", new Slider("Min. HP for using BOTRK (%)", 50, 0, 100));
            Itemss.Add("EnBotrkHp", new Slider("Min. Enemy HP for using BOTRK (%)", 50, 0, 100));

            /*Summ = General.AddSubMenu("Summ. spells");
            Summ.AddGroupLabel("Heal");
            Summ.Add("Heal", new CheckBox("Enable heal"));
            Summ.Add("AllyHeal", new CheckBox("Enable heal for Ally"));
            Summ.Add("healhp", new Slider("Use Heal if HP < (%) ", 25, 0, 100)); */


            Draw = General.AddSubMenu("Draw");
            Draw.Add("onlyRdy", new CheckBox("Draw only ready spells"));
            Draw.Add("wRange", new CheckBox("W range"));
            Draw.Add("rNot", new CheckBox("R key info"));

            Q = new Spell.Active(SpellSlot.Q);
            W = new Spell.Skillshot(SpellSlot.W, 1200, SkillShotType.Linear, 0, int.MaxValue, 60)
            {
                AllowedCollisionCount = 0
            };
            E = new Spell.Skillshot(SpellSlot.E, 15000, SkillShotType.Linear, 0, int.MaxValue, 0);
            R = new Spell.Skillshot(SpellSlot.R, 15000, SkillShotType.Linear, 500, 1000, 250);

            Game.OnUpdate += OnUpdate;
            Orbwalker.OnPostAttack += AfterAttack;
            Game.OnWndProc += Game_OnWndProc;
            Interrupter.OnInterruptableSpell += InterrupterOnInterruptableSpell;
            Gapcloser.OnGapcloser += GapcloserOnGapcloser;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Drawing.OnDraw += OnDraw;
        }

        private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!ConfigE["Eflash"].Cast<CheckBox>().CurrentValue || sender.Team == ObjectManager.Player.Team)
            {
                return;
            }

            if (args.SData.Name.ToLower() == "summonerflash" && sender.Distance(ObjectManager.Player.Position) < 2000)
            {
                E.Cast(args.End);
            }
        }

        private void OnDraw(EventArgs args)
        {
            if (Draw["rNot"].Cast<CheckBox>().CurrentValue && R.IsReady() && R.IsLearned)
            {
                if (RTarget != null)
                {
                    drawText("R KEY TARGET: " + RTarget.BaseSkinName, Player.Position, System.Drawing.Color.YellowGreen,
                        150);
                }
                else
                {
                    drawText("PLS CLICK LEFT ON R TARGET", Player.Position, System.Drawing.Color.YellowGreen, 150);
                }

            }
            if (Draw["wRange"].Cast<CheckBox>().CurrentValue)
            {
                if (Draw["onlyRdy"].Cast<CheckBox>().CurrentValue)
                {
                    new Circle()
                    {
                        BorderWidth = 2,
                        Color = Color.Orange,
                        Radius = W.Range,
                    }.Draw(Player.Position);
                }
                else
                {
                    new Circle()
                    {
                        BorderWidth = 2,
                        Color = Color.Orange,
                        Radius = W.Range,
                    }.Draw(Player.Position);
                }
            }
        }

        private void GapcloserOnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs gapcloserEventArgs)
        {
            if (R.IsReady())
            {
                var target = gapcloserEventArgs.Sender;
                if (target.IsValidTarget(800) && ConfigR["GapCloser"].Cast<CheckBox>().CurrentValue)
                {
                    R.Cast(target.ServerPosition);
                }
            }
        }

        private void InterrupterOnInterruptableSpell(Obj_AI_Base sender,
            Interrupter.InterruptableSpellEventArgs interruptableSpellEventArgs)
        {
            if (ConfigR["autoRinter"].Cast<CheckBox>().CurrentValue && R.IsReady() && sender.IsValidTarget(2500))
                R.Cast(sender);
        }

        private void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg == 513 && EntityManager.Heroes.Enemies.Exists(x => Game.CursorPos.Distance(x.Position) < 300))
            {
                RTarget = EntityManager.Heroes.Enemies.First(x => Game.CursorPos.Distance(x.Position) < 300);
            }
        }

        private void AfterAttack(AttackableUnit target, EventArgs args)
        {
            //
        }

        private void OnUpdate(EventArgs args)
        {            
            SetMana();
            Jungle();
            Skin();
            Items();
            //Sum();
            AutoLevel();
            Hawk();
            

            if (R.IsReady())
            {
                if (ConfigR["useR"].Cast<KeyBind>().CurrentValue)
                {
                    CastR = true;
                }
                if (ConfigR["useR2"].Cast<KeyBind>().CurrentValue)
                {
                    CastR2 = true;
                }

                if (CastR2)
                {
                    if (RTarget.IsValidTarget())
                        R.Cast(RTarget);
                }

                if (CastR)
                {
                    if (ConfigR["Semi-manual"].Cast<ComboBox>().CurrentValue == 0)
                    {
                        var t = TargetSelector.GetTarget(1800, DamageType.Physical);
                        if (t.IsValidTarget())
                            R.Cast(t);
                    }
                    else if (ConfigR["Semi-manual"].Cast<ComboBox>().CurrentValue == 1)
                    {
                        var t = EntityManager.Heroes.Enemies.OrderBy(x => x.Distance(Player)).FirstOrDefault();
                        if (t.IsValidTarget())
                            R.Cast(t);
                    }
                }
            }
            else
            {
                CastR = false;
                CastR2 = false;
            }

            if (W.IsReady())
            {
                LogicWcombo();
                LogicWlane();
            }
            if (Q.IsReady())
            {
                LogicQ();
            }
            if (R.IsReady())
            {
                LogicR();
            }
        }

        private void LogicQ()
        {
            var t = Orbwalker.GetTarget() as AIHeroClient;
            if (t != null && t.IsValidTarget())
            {
                if (Combo && (Player.Mana > RMANA + QMANA || t.Health < 5*Player.GetAutoAttackDamage(Player)))
                    Q.Cast();
                else if (Farm && Player.Mana > RMANA + QMANA + WMANA && ConfigQ["harasQ"].Cast<CheckBox>().CurrentValue)
                    Q.Cast();
            }
            else if (LaneClear)
            {
                var minion = Orbwalker.GetTarget() as Obj_AI_Minion;
                if (minion != null && Player.ManaPercent > Farmed["Mana"].Cast<Slider>().CurrentValue &&
                    Farmed["farmQ"].Cast<CheckBox>().CurrentValue && Player.Mana > RMANA + QMANA)
                {
                    if (EntityManager.MinionsAndMonsters.GetLaneMinions().Count() >=
                        Farmed["LCminions"].Cast<Slider>().CurrentValue)
                        Q.Cast();
                }
            }
        }

        private void LogicWcombo()
        {
            var t = Orbwalker.GetTarget() as AIHeroClient;

            if (t == null)
                t = TargetSelector.GetTarget(W.Range, DamageType.Physical);

            if (t.IsValidTarget())
            {
                if (Combo && Player.Mana > RMANA + WMANA)
                    W.Cast(t);
                else if (Farm && Player.Mana > RMANA + WMANA + QMANA + WMANA)
                {
                    foreach (
                        var enemy in
                            EntityManager.Heroes.Enemies.Where(
                                enemy => enemy.IsValidTarget(W.Range) && !Orbwalker.CanMove))
                        W.Cast(t);
                }
            }
        }

        private void LogicWlane()
        {
            var t = Orbwalker.GetTarget() as Obj_AI_Minion;
            if (t != null)
            {
                if (t.IsValidTarget())
                {
                    if (LaneClear && Player.ManaPercent > Farmed["Mana"].Cast<Slider>().CurrentValue &&
                        Farmed["farmW"].Cast<CheckBox>().CurrentValue && Player.Mana > RMANA + WMANA)
                    {
                        var minionList = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                            Player.ServerPosition, W.Range);
                        var farmPos = EntityManager.MinionsAndMonsters.GetCircularFarmLocation(minionList, 300, 800);

                        if (farmPos.HitNumber >= Farmed["LCminions"].Cast<Slider>().CurrentValue)
                            W.Cast(farmPos.CastPosition);
                    }
                }
            }
        }

        private void LogicR()
        {
            if (ConfigR["autoR"].Cast<CheckBox>().CurrentValue)
                foreach (var target in EntityManager.Heroes.Enemies.Where(target => target.IsValidTarget(2000)))
                {
                    var rDmg = Player.GetSpellDamage(target, SpellSlot.R);
                    var wDmg = Player.GetSpellDamage(target, SpellSlot.W);
                    if (Combo && target.CountEnemiesInRange(250) > 2 &&
                        ConfigR["autoRaoe"].Cast<CheckBox>().CurrentValue &&
                        target.IsValidTarget(1500))
                        R.Cast(target);
                    if (Combo && target.IsValidTarget(W.Range) && ConfigR["Rkscombo"].Cast<CheckBox>().CurrentValue &&
                        Player.GetAutoAttackDamage(target)*5 + rDmg + wDmg > target.Health &&
                        target.HasBuffOfType(BuffType.Slow))
                        R.Cast(target);
                    if (rDmg > target.Health && target.CountAlliesInRange(600) == 0 &&
                        target.Distance(Player.Position) > 1000)
                        R.Cast(target);
                }
            if (Player.HealthPercent < 50)
            {
                foreach (
                    var enemy in
                        EntityManager.Heroes.Enemies.Where(
                            enemy =>
                                enemy.IsValidTarget(300) && enemy.IsMelee &&
                                ConfigR["GapCloser"].Cast<CheckBox>().CurrentValue))
                {
                    R.Cast(enemy);
                }
            }
        }

        private void Jungle()
        {
            if (LaneClear)
            {
                var mobs = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.ServerPosition, 600);
                if (mobs.Count() > 0)
                {
                    if (W.IsReady() && Farmed["jungleW"].Cast<CheckBox>().CurrentValue)
                    {
                        W.Cast(mobs.First());
                    }
                    if (Q.IsReady() && Farmed["jungleQ"].Cast<CheckBox>().CurrentValue)
                    {
                        Q.Cast();
                    }
                }
            }
        }

        private void Skin()
        {
            if (SkinChanger["skinEnable"].Cast<CheckBox>().CurrentValue)
            {
                Player.SetSkinId(SkinChanger["skinID"].Cast<ComboBox>().CurrentValue);
            }
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
                RMANA = WMANA - Player.PARRegenRate*W.Handle.Cooldown;
            else
                RMANA = R.Handle.SData.Mana;
        }

        public void drawText(string msg, Vector3 Hero, System.Drawing.Color color, int weight = 0)
        {
            var wts = Drawing.WorldToScreen(Hero);
            Drawing.DrawText(wts[0] - (msg.Length)*5, wts[1] + weight, color, msg);
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

        /* private void Sum()
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
            if (Summ["AllyHeal"].Cast<CheckBox>().CurrentValue)
            {
                foreach (var ally in EntityManager.Heroes.Allies.Where(a => !a.IsDead && a.IsValidTarget(800) && a.HealthPercent <= Summ["healhp"].Cast<Slider>().CurrentValue))
                {
                    if (heal.IsReady() && ally.CountEnemiesInRange(800) >= 1)
                    {
                        heal.Cast();
                    }
                }
            }
        } */

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

            if (Player.ChampionName == "Ashe")
            {
                if (AutoLevell["ALBox"].Cast<ComboBox>().CurrentValue == 0)
                {
                    SpellLevels = new int[] { 2, 1, 1, 3, 1, 4, 1, 2, 1, 2, 4, 2, 2, 3, 3, 4, 3, 3 };
                }
                if (AutoLevell["ALBox"].Cast<ComboBox>().CurrentValue == 1)
                {
                    SpellLevels = new int[] { 2, 1, 2, 3, 2, 4, 2, 1, 2, 1, 4, 1, 1, 3, 3, 4, 3, 3 };
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

        public void CastE(SharpDX.Vector3 target)
        {
            if (target == null)
            {
                return;
            }
            if (E.IsReady())
                E.Cast(target);
        }

        public void Hawk()
        {
            if (!E.IsReady())
            {
                return;
            }

            if (ConfigE["EDragon"].Cast<KeyBind>().CurrentValue)
            {
                CastE(new Vector3(9865, 4415, 0));
            }
            if (ConfigE["EBaron"].Cast<KeyBind>().CurrentValue)
            {
                CastE(new Vector3(5005, 10470, 0));
            }
        }
    }
}

