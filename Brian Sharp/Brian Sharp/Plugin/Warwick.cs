﻿using System;
using System.Drawing;
using System.Linq;
using BrianSharp.Common;
using EloBuddy;
using LeagueSharp.Common;
using Orbwalk = BrianSharp.Common._Orbwalker;

namespace BrianSharp.Plugin
{
    internal class Warwick : Helper
    {
        public Warwick()
        {
            Q = new Spell(SpellSlot.Q, 400, TargetSelector.DamageType.Magical);
            W = new Spell(SpellSlot.W, 1250);
            R = new Spell(SpellSlot.R, 700, TargetSelector.DamageType.Magical);

            var champMenu = new Menu("Plugin", Player.Instance.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    var lockMenu = new Menu("Lock (R)", "Lock");
                    {
                        foreach (var obj in HeroManager.Enemies)
                        {
                            AddBool(lockMenu, obj.ChampionName, obj.ChampionName);
                        }
                        comboMenu.AddSubMenu(lockMenu);
                    }
                    AddBool(comboMenu, "Q", "Use Q");
                    AddBool(comboMenu, "W", "Use W");
                    AddBool(comboMenu, "R", "Use R");
                    AddBool(comboMenu, "RSmite", "-> Use Red Smite");
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddKeybind(harassMenu, "AutoQ", "Auto Q", "H", KeyBindType.Toggle);
                    AddSlider(harassMenu, "AutoQMpA", "-> If Mp >=", 50);
                    AddBool(harassMenu, "Q", "Use Q");
                    AddBool(harassMenu, "W", "Use W");
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddSmiteMob(clearMenu);
                    AddBool(clearMenu, "Q", "Use Q");
                    AddBool(clearMenu, "W", "Use W");
                    champMenu.AddSubMenu(clearMenu);
                }
                var lastHitMenu = new Menu("Last Hit", "LastHit");
                {
                    AddBool(lastHitMenu, "Q", "Use Q");
                    champMenu.AddSubMenu(lastHitMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddBool(killStealMenu, "Q", "Use Q");
                        AddBool(killStealMenu, "R", "Use R");
                        AddBool(killStealMenu, "Ignite", "Use Ignite");
                        AddBool(killStealMenu, "Smite", "Use Smite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    AddBool(miscMenu, "RTower", "Auto R If Enemy Under Tower");
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddBool(drawMenu, "Q", "Q Range", false);
                    AddBool(drawMenu, "R", "R Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Orbwalk.OnAttack += OnAttack;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (player.IsDead || MenuGUI.IsChatOpen || player.LSIsRecalling())
            {
                return;
            }
            switch (Orbwalk.CurrentMode)
            {
                case _Orbwalker.Mode.Combo:
                    Fight("Combo");
                    break;
                case _Orbwalker.Mode.Harass:
                    Fight("Harass");
                    break;
                case _Orbwalker.Mode.Clear:
                    Clear();
                    break;
                case _Orbwalker.Mode.LastHit:
                    LastHit();
                    break;
            }
            if (GetValue<bool>("SmiteMob", "Auto") && Orbwalk.CurrentMode != _Orbwalker.Mode.Clear)
            {
                SmiteMob();
            }
            AutoQ();
            KillSteal();
            AutoRUnderTower();
        }

        private static void OnDraw(EventArgs args)
        {
            if (player.IsDead)
            {
                return;
            }
            if (GetValue<bool>("Draw", "Q") && Q.Level > 0)
            {
                Render.Circle.DrawCircle(player.Position, Q.Range, Q.LSIsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "R") && R.Level > 0)
            {
                Render.Circle.DrawCircle(player.Position, R.Range, R.LSIsReady() ? Color.Green : Color.Red);
            }
        }

        private static void OnAttack(AttackableUnit target)
        {
            if (!W.LSIsReady())
            {
                return;
            }
            if (((Orbwalk.CurrentMode == _Orbwalker.Mode.Combo || Orbwalk.CurrentMode == _Orbwalker.Mode.Harass) &&
                 GetValue<bool>(Orbwalk.CurrentMode.ToString(), "W") && target is AIHeroClient) ||
                (Orbwalk.CurrentMode == _Orbwalker.Mode.Clear && GetValue<bool>("Clear", "W") && target is Obj_AI_Minion))
            {
                W.Cast(PacketCast);
            }
        }

        private static void Fight(string mode)
        {
            if (GetValue<bool>(mode, "Q") && Q.CastOnBestTarget(0, PacketCast).LSIsCasted())
            {
                return;
            }
            if (mode != "Combo")
            {
                return;
            }
            if (GetValue<bool>(mode, "R") && R.LSIsReady())
            {
                var target = R.GetTarget(0, HeroManager.Enemies.Where(i => !GetValue<bool>("Lock", i.ChampionName)));
                if (target != null)
                {
                    if (GetValue<bool>(mode, "RSmite") && CurrentSmiteType == SmiteType.Red)
                    {
                        CastSmite(target, false);
                    }
                    if ((!GetValue<bool>(mode, "RSmite") || CurrentSmiteType != SmiteType.Red) &&
                        R.CastOnUnit(target, PacketCast))
                    {
                        return;
                    }
                }
            }
            if (GetValue<bool>(mode, "W") && W.LSIsReady() &&
                HeroManager.Allies.Any(
                    i => !i.IsMe && i.LSIsValidTarget(W.Range, false) && Orbwalking.IsAutoAttack(i.LastCastedSpellName())))
            {
                W.Cast(PacketCast);
            }
        }

        private static void Clear()
        {
            SmiteMob();
            if (!GetValue<bool>("Clear", "Q") || !Q.LSIsReady())
            {
                return;
            }
            var minionObj = GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            var obj = minionObj.FirstOrDefault(i => Q.IsKillable(i)) ?? minionObj.MinOrDefault(i => i.Health);
            if (obj == null)
            {
                return;
            }
            Q.CastOnUnit(obj, PacketCast);
        }

        private static void LastHit()
        {
            if (!GetValue<bool>("LastHit", "Q") || !Q.LSIsReady())
            {
                return;
            }
            var obj =
                GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                    .FirstOrDefault(i => Q.IsKillable(i));
            if (obj == null)
            {
                return;
            }
            Q.CastOnUnit(obj, PacketCast);
        }

        private static void AutoQ()
        {
            if (!GetValue<KeyBind>("Harass", "AutoQ").Active ||
                player.ManaPercent < GetValue<Slider>("Harass", "AutoQMpA").Value)
            {
                return;
            }
            Q.CastOnBestTarget(0, PacketCast);
        }

        private static void KillSteal()
        {
            if (GetValue<bool>("KillSteal", "Ignite") && Ignite.LSIsReady())
            {
                var target = TargetSelector.GetTarget(600, TargetSelector.DamageType.True);
                if (target != null && CastIgnite(target))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "Smite") &&
                (CurrentSmiteType == SmiteType.Blue || CurrentSmiteType == SmiteType.Red))
            {
                var target = TargetSelector.GetTarget(760, TargetSelector.DamageType.True);
                if (target != null && CastSmite(target))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "Q") && Q.LSIsReady())
            {
                var target = Q.GetTarget();
                if (target != null && Q.IsKillable(target) && Q.CastOnUnit(target, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "R") && R.LSIsReady())
            {
                var target = R.GetTarget();
                if (target != null && R.IsKillable(target))
                {
                    R.CastOnUnit(target, PacketCast);
                }
            }
        }

        private static void AutoRUnderTower()
        {
            if (!GetValue<bool>("Misc", "RTower") || !R.LSIsReady())
            {
                return;
            }
            var target = HeroManager.Enemies.Where(i => i.LSIsValidTarget(R.Range)).MinOrDefault(i => i.LSDistance(player));
            var tower =
                ObjectManager.Get<Obj_AI_Turret>()
                    .FirstOrDefault(i => i.IsAlly && !i.IsDead && i.LSDistance(player) <= 850);
            if (target != null && tower != null && target.LSDistance(tower) <= 850)
            {
                R.CastOnUnit(target, PacketCast);
            }
        }
    }
}