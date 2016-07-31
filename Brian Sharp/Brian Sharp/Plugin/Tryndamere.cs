using System;
using System.Drawing;
using System.Linq;
using BrianSharp.Common;
using EloBuddy;
using LeagueSharp.Common;
using Utility = LeagueSharp.Common.Utility;
using Orbwalk = BrianSharp.Common._Orbwalker;

namespace BrianSharp.Plugin
{
    internal class Tryndamere : Helper
    {
        public Tryndamere()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 830);
            E = new Spell(SpellSlot.E, 660);
            R = new Spell(SpellSlot.R);
            E.SetSkillshot(0, 93, 1300, false, SkillshotType.SkillshotLine);

            var champMenu = new Menu("Plugin", Player.Instance.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddBool(comboMenu, "Q", "Use Q");
                    AddSlider(comboMenu, "QHpU", "-> If Hp <", 40);
                    AddBool(comboMenu, "W", "Use W");
                    AddBool(comboMenu, "WSolo", "-> Both Facing", false);
                    AddBool(comboMenu, "E", "Use E");
                    AddBool(comboMenu, "R", "Use R");
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddBool(harassMenu, "E", "Use E");
                    AddSlider(harassMenu, "EHpA", "-> If Hp >=", 20);
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddSmiteMob(clearMenu);
                    AddBool(clearMenu, "E", "Use E");
                    AddBool(clearMenu, "Item", "Use Tiamat/Hydra");
                    champMenu.AddSubMenu(clearMenu);
                }
                var fleeMenu = new Menu("Flee", "Flee");
                {
                    AddBool(fleeMenu, "E", "Use E");
                    champMenu.AddSubMenu(fleeMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddBool(killStealMenu, "E", "Use E");
                        AddBool(killStealMenu, "Ignite", "Use Ignite");
                        AddBool(killStealMenu, "Smite", "Use Smite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddBool(drawMenu, "W", "W Range", false);
                    AddBool(drawMenu, "E", "E Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            AttackableUnit.OnDamage += OnDamage;
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
                case _Orbwalker.Mode.Flee:
                    if (GetValue<bool>("Flee", "E") && E.LSIsReady() && E.Cast(Game.CursorPos, PacketCast))
                    {
                        return;
                    }
                    break;
            }
            if (GetValue<bool>("SmiteMob", "Auto") && Orbwalk.CurrentMode != _Orbwalker.Mode.Clear)
            {
                SmiteMob();
            }
            KillSteal();
        }

        private static void OnDraw(EventArgs args)
        {
            if (player.IsDead)
            {
                return;
            }
            if (GetValue<bool>("Draw", "W") && W.Level > 0)
            {
                Render.Circle.DrawCircle(player.Position, W.Range, W.LSIsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "E") && E.Level > 0)
            {
                Render.Circle.DrawCircle(player.Position, E.Range, E.LSIsReady() ? Color.Green : Color.Red);
            }
        }

        private static void OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            if (args.Target.NetworkId != player.NetworkId || Orbwalk.CurrentMode != _Orbwalker.Mode.Combo)
            {
                return;
            }
            if (GetValue<bool>("Combo", "R") && R.LSIsReady() && player.HealthPercent < 10 && R.Cast(PacketCast))
            {
                return;
            }
            if (GetValue<bool>("Combo", "Q") && Q.LSIsReady() && !Player.HasBuff("UndyingRage") &&
                player.HealthPercent < GetValue<Slider>("Survive", "QHpU").Value)
            {
                Q.Cast(PacketCast);
            }
        }

        private static void Fight(string mode)
        {
            if (mode == "Combo" && GetValue<bool>(mode, "W") && W.LSIsReady() && !player.LSIsDashing())
            {
                var target = W.GetTarget();
                if (target != null)
                {
                    if (GetValue<bool>(mode, "WSolo") && Utility.LSIsBothFacing(player, target) &&
                        Orbwalk.InAutoAttackRange(target) &&
                        player.LSGetAutoAttackDamage(target, true) < target.LSGetAutoAttackDamage(player, true))
                    {
                        return;
                    }
                    if (player.LSIsFacing(target) && !target.LSIsFacing(player) && !Orbwalk.InAutoAttackRange(target, 30) &&
                        W.Cast(PacketCast))
                    {
                        return;
                    }
                }
            }
            if (GetValue<bool>(mode, "E") && E.LSIsReady() &&
                (mode == "Combo" || player.HealthPercent >= GetValue<Slider>(mode, "EHpA").Value))
            {
                var target = E.GetTarget(E.Width);
                if (target != null)
                {
                    var predE = E.GetPrediction(target, true);
                    if (predE.Hitchance >= E.MinHitChance &&
                        ((mode == "Combo" && !Orbwalk.InAutoAttackRange(target, 20)) ||
                         (mode == "Harass" && Orbwalk.InAutoAttackRange(target, 50))))
                    {
                        E.Cast(predE.CastPosition.LSExtend(player.ServerPosition, -100), PacketCast);
                    }
                }
                else
                {
                    var sub = E.GetTarget(Orbwalk.GetAutoAttackRange());
                    if (sub != null &&
                        Orbwalk.InAutoAttackRange(sub, 20, player.ServerPosition.LSExtend(sub.ServerPosition, E.Range)))
                    {
                        E.Cast(player.ServerPosition.LSExtend(sub.ServerPosition, E.Range), PacketCast);
                    }
                }
            }
        }

        private static void Clear()
        {
            SmiteMob();
            var minionObj = GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (!minionObj.Any())
            {
                return;
            }
            if (GetValue<bool>("Clear", "E") && E.LSIsReady())
            {
                var pos = E.GetLineFarmLocation(minionObj.Cast<Obj_AI_Base>().ToList(), 200);
                if (pos.MinionsHit > 0 && E.Cast(pos.Position.LSExtend(player.ServerPosition.LSTo2D(), -100), PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "Item"))
            {
                var item = Hydra.IsReady() ? Hydra : Tiamat;
                if (item.IsReady() &&
                    (minionObj.Count(i => item.IsInRange(i)) > 2 ||
                     minionObj.Any(i => i.MaxHealth >= 1200 && i.LSDistance(player) < item.Range - 80)))
                {
                    item.Cast();
                }
            }
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
            if (GetValue<bool>("KillSteal", "E") && E.LSIsReady())
            {
                var target = E.GetTarget(E.Width);
                if (target != null && E.IsKillable(target))
                {
                    var predE = E.GetPrediction(target, true);
                    if (predE.Hitchance >= E.MinHitChance)
                    {
                        E.Cast(predE.CastPosition.LSExtend(player.ServerPosition, -100), PacketCast);
                    }
                }
            }
        }
    }
}