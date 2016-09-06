using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using Color = System.Drawing.Color;


namespace Worst_Nasus
{
    internal class Core
    {
        public static int Sheen = 3057, Iceborn = 3025;

        private static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }

        private static readonly Dictionary<Spells, Spell.SpellBase> spells = new Dictionary<Spells, Spell.SpellBase>
        {
            {Spells.Q, new Spell.Active(SpellSlot.Q, 225)},
            {Spells.W, new Spell.Targeted(SpellSlot.W, 600)},
            {
                Spells.E,
                new Spell.Skillshot(SpellSlot.E, 650, SkillShotType.Circular, 500, 20, 380)
                {
                    AllowedCollisionCount = int.MaxValue
                }
            },
            {Spells.R, new Spell.Active(SpellSlot.R)}

        };

        private static SpellSlot Ignite;

        private Menu Config;

        public enum Spells
        {
            Q,

            W,

            E,

            R
        }

        public void Load()
        {
            //Menu
            Config = MainMenu.AddMenu("Worst Nasus", "NasusMenu");
            Config.AddGroupLabel("Combo Settings");
            Config.Add("WorstNasus.Combo.Q", new CheckBox("Use Q"));
            Config.Add("WorstNasus.Combo.W", new CheckBox("Use W"));
            Config.Add("WorstNasus.Combo.E", new CheckBox("Use E"));
            Config.Add("WorstNasus.Combo.R", new CheckBox("Use R"));
            Config.Add("WorstNasus.Combo.R.Count", new Slider("Minimum champions in range for R", 2, 1, 5));
            Config.Add("WorstNasus.Combo.R.HP", new Slider("Minimum HP(%) for R", 30, 0, 100));
            Config.AddGroupLabel("Harass Settings");
            Config.Add("WorstNasus.Harass.E", new CheckBox("Use E"));
            Config.Add("WorstNasus.Harass.Mana", new Slider("Minimum Mana(%)", 55, 0, 100));
            Config.AddGroupLabel("Lane Clear Settings");
            Config.Add("WorstNasus.LaneClear.Q", new CheckBox("Use Q"));
            Config.Add("WorstNasus.LaneClear.E", new CheckBox("Use E"));
            Config.AddGroupLabel("Jungle Clear Settings");
            Config.Add("WorstNasus.JungleClear.Q", new CheckBox("Use Q"));
            Config.Add("WorstNasus.JungleClear.E", new CheckBox("Use E"));
            Config.AddGroupLabel("Miscellaneous");
            Config.Add("healthbar", new CheckBox("Damage Indicator"));
            Config.Add("WorstNasus.Draw.Off", new CheckBox("Turn drawings off", false));
            Config.Add("WorstNasus.Draw.W", new CheckBox("Draw W"));
            Config.Add("WorstNasus.Draw.E", new CheckBox("Draw E"));
            Config.Add("WorstNasus.Draw.MinionHelper", new CheckBox("Draw killable (With Q) minions"));


            //Events
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
        }

        private void OnDraw(EventArgs args)
        {
            DamageIndicator.HealthbarEnabled = Config["healthbar"].Cast<CheckBox>().CurrentValue;

            var DrawOff = Config["WorstNasus.Draw.Off"].Cast<CheckBox>().CurrentValue;
            var DrawW = Config["WorstNasus.Draw.W"].Cast<CheckBox>().CurrentValue;
            var DrawE = Config["WorstNasus.Draw.E"].Cast<CheckBox>().CurrentValue;
            var helper = Config["WorstNasus.Draw.MinionHelper"].Cast<CheckBox>().CurrentValue;

            var playerPos = Drawing.WorldToScreen(Player.Position);

            if (DrawOff) { return;}

            if (DrawW)
            {
                if (spells[Spells.W].Level > 0)
                {
                    new Circle()
                    {
                        BorderWidth = 2,
                        Color = Color.Orange,
                        Radius = spells[Spells.W].Range,
                    }.Draw(Player.Position);
                }
            }
            if (DrawE)
            {
                if (spells[Spells.E].Level > 0)
                {
                    new Circle()
                    {
                        BorderWidth = 2,
                        Color = Color.Aqua,
                        Radius = spells[Spells.E].Range,
                    }.Draw(Player.Position);
                }
            }
            if (helper)
            {
                var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                    Player.ServerPosition, spells[Spells.E].Range);
                foreach (var minion in minions)
                {
                    if (minion != null)
                    {
                        if (GetBonusDmg(minion) > minion.Health)
                        {
                            new Circle()
                            {
                                BorderWidth = 3,
                                Color = Color.Green,
                                Radius = minion.BoundingRadius,
                            }.Draw(minion.ServerPosition);
                        }
                    }
                }
            }
        }

        

        private void OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                OnCombo();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {
                OnLastHit();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                OnHarass();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                OnLaneClear();             
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                OnJungleclear();
            }
        }

        private void OnCombo()
        {
            var target = TargetSelector.GetTarget(spells[Spells.W].Range, DamageType.Physical);

            if (!target.IsValidTarget())
            {
                return;
            }

            var useQ = Config["WorstNasus.Combo.Q"].Cast<CheckBox>().CurrentValue;
            var useW = Config["WorstNasus.Combo.W"].Cast<CheckBox>().CurrentValue;
            var useE = Config["WorstNasus.Combo.E"].Cast<CheckBox>().CurrentValue;
            var useR = Config["WorstNasus.Combo.R"].Cast<CheckBox>().CurrentValue;
            var count = Config["WorstNasus.Combo.R.Count"].Cast<Slider>().CurrentValue;
            var HP = Config["WorstNasus.Combo.R.HP"].Cast<Slider>().CurrentValue;

            if (target != null)
            {
                if (useW && spells[Spells.W].IsReady())
                {
                    spells[Spells.W].Cast(target);
                }

                if (useQ && spells[Spells.Q].IsReady())
                {
                    spells[Spells.Q].Cast();
                }

                if (useE && spells[Spells.E].IsReady() && target.IsValidTarget())
                {
                    spells[Spells.E].Cast(target);
                }

                if (useR && spells[Spells.R].IsReady() && (Player.Health/Player.MaxHealth)*100 <= HP && Player.CountEnemiesInRange(spells[Spells.W].Range) >= count)
                {
                    spells[Spells.R].Cast();
                }
            }
        }

        private void OnLastHit()
        {
            var minion =
                EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Position,
                    225).OrderByDescending(m => GetBonusDmg(m) > m.Health).FirstOrDefault();


            if (minion != null)
            {
                if (GetBonusDmg(minion) > minion.Health && spells[Spells.Q].IsReady())
                {
                    spells[Spells.Q].Cast();
                    Orbwalker.ForcedTarget = minion;
                }
            }
        }

        private void OnHarass()
        {
            var eTarget = TargetSelector.GetTarget(
                spells[Spells.E].Range + spells[Spells.E].Handle.SData.LineWidth,
                DamageType.Magical);           

            var useE = Config["WorstNasus.Harass.E"].Cast<CheckBox>().CurrentValue;
            var mana = Config["WorstNasus.Harass.Mana"].Cast<Slider>().CurrentValue;

            if(Player.Mana <= mana) { return;}

            if (eTarget != null)
            {
                if (useE && spells[Spells.E].IsReady() && eTarget.IsValidTarget() && spells[Spells.E].IsInRange(eTarget) && Player.Mana >= mana)
                {
                    spells[Spells.E].Cast(eTarget);
                }
            }
        }

        private void OnLaneClear()
        {
            var useQ = Config["WorstNasus.LaneClear.Q"].Cast<CheckBox>().CurrentValue;
            var useE = Config["WorstNasus.LaneClear.E"].Cast<CheckBox>().CurrentValue;

            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                Player.ServerPosition, spells[Spells.E].Range);
            if (minions.Count() <= 0)
            {
                return;
            }

            if (minions != null)
            {
                if (spells[Spells.Q].IsReady() && useQ)
                {
                    var allMinions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                        Player.ServerPosition, spells[Spells.E].Range);
                    {
                        foreach (var minion in
                            allMinions.Where(
                                minion => minion.Health <= GetBonusDmg(minion)))
                        {
                            if (minion.IsValidTarget())
                            {
                                spells[Spells.Q].Cast();
                                return;
                            }
                        }
                    }
                }
            }

            if (useE && spells[Spells.E].IsReady())
            {
                if (minions.Count() > 1)
                {
                    var farmLocation = EntityManager.MinionsAndMonsters.GetCircularFarmLocation(minions, 400, 650);
                    spells[Spells.E].Cast(farmLocation.CastPosition);
                }
            }
        }

        private void OnJungleclear()
        {
            var useQ = Config["WorstNasus.JungleClear.Q"].Cast<CheckBox>().CurrentValue;
            var useE = Config["WorstNasus.JungleClear.E"].Cast<CheckBox>().CurrentValue;

            var minions = EntityManager.MinionsAndMonsters.GetJungleMonsters(
                Player.ServerPosition, spells[Spells.E].Range);

            if (minions.Count() <= 0)
            {
                return;
            }

            if (minions != null)
            {
                if (useQ && spells[Spells.Q].IsReady())
                {
                    if (minions.FirstOrDefault(x => x.Health >= GetBonusDmg(x) && x.IsValidTarget()) != null)
                    {
                        spells[Spells.Q].Cast();
                    }
                }

                if (useE && spells[Spells.E].IsReady())
                {
                    if (minions.Count() > 1)
                    {
                        var farmLocation = EntityManager.MinionsAndMonsters.GetCircularFarmLocation(minions, 400, 650);
                        spells[Spells.E].Cast(farmLocation.CastPosition);
                    }
                }
            }
        }

        private static float GetBonusDmg(Obj_AI_Base target)
        {
            float dmgItem = 0;

            if (Item.HasItem(Sheen) && (Item.CanUseItem(Sheen) || Player.HasBuff("sheen"))
                && Player.BaseAttackDamage > dmgItem)
            {
                dmgItem = Player.BaseAttackDamage;
            }

            if (Item.HasItem(Iceborn) && (Item.CanUseItem(Iceborn) || Player.HasBuff("itemfrozenfist"))
                && Player.BaseAttackDamage*1.25 > dmgItem)
            {
                dmgItem = Player.BaseAttackDamage*1.25f;
            }

            return
                Player.CalculateDamageOnUnit(target, DamageType.Physical,
                (new float[] {0, 30, 50, 70, 90, 110}[spells[Spells.Q].Level] + Player.FlatPhysicalDamageMod +
                 Player.GetBuffCount("NasusQStacks")) + dmgItem) +
                Player.GetAutoAttackDamage(target);
        }

        private static float EDamage(Obj_AI_Base target)
        {
            return target.CalculateDamageOnUnit(target, DamageType.Magical,
                new[] { 0, 55, 95, 135, 175, 215 }[spells[Spells.E].Level] + (Player.TotalMagicalDamage * 0.6f));
        }

        public static float DmgCalc(AIHeroClient target)
        {
            var damage = 0f;
            if (spells[Spells.Q].IsReady() && target.IsValidTarget())
                damage += GetBonusDmg(target);
            if (spells[Spells.E].IsReady() && target.IsValidTarget())
                damage += EDamage(target);
            
            damage += Player.GetAutoAttackDamage(target);
            return damage;
        }
    }
}