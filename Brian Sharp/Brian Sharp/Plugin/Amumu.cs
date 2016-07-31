﻿using System;
using System.Collections.Generic;
using System.Linq;
using BrianSharp.Common;
using EloBuddy;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;
using Orbwalk = BrianSharp.Common._Orbwalker;

namespace BrianSharp.Plugin
{
    internal class Amumu : Helper
    {
        public Amumu()
        {
            Q = new Spell(SpellSlot.Q, 1100, TargetSelector.DamageType.Magical);
            W = new Spell(SpellSlot.W, 300, TargetSelector.DamageType.Magical);
            E = new Spell(SpellSlot.E, 350, TargetSelector.DamageType.Magical);
            R = new Spell(SpellSlot.R, 550, TargetSelector.DamageType.Magical);
            Q.SetSkillshot(0.25f, 90, 2000, true, SkillshotType.SkillshotLine);

            var champMenu = new Menu("Plugin", Player.Instance.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddBool(comboMenu, "Q", "Use Q");
                    AddBool(comboMenu, "QCol", "-> Smite Collision");
                    AddBool(comboMenu, "W", "Use W");
                    AddSlider(comboMenu, "WMpA", "-> If Mp >=", 20);
                    AddBool(comboMenu, "E", "Use E");
                    AddBool(comboMenu, "R", "Use R");
                    AddSlider(comboMenu, "RHpU", "-> If Enemy Hp <", 60);
                    AddSlider(comboMenu, "RCountA", "-> Or Enemy >=", 2, 1, 5);
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddBool(harassMenu, "W", "Use W");
                    AddSlider(harassMenu, "WMpA", "-> If Mp >=", 20);
                    AddBool(harassMenu, "E", "Use E");
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddSmiteMob(clearMenu);
                    AddBool(clearMenu, "Q", "Use Q");
                    AddBool(clearMenu, "W", "Use W");
                    AddSlider(clearMenu, "WMpA", "-> If Mp >=", 20);
                    AddBool(clearMenu, "E", "Use E");
                    champMenu.AddSubMenu(clearMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddBool(killStealMenu, "Q", "Use Q");
                        AddBool(killStealMenu, "E", "Use E");
                        AddBool(killStealMenu, "R", "Use R");
                        AddBool(killStealMenu, "Ignite", "Use Ignite");
                        AddBool(killStealMenu, "Smite", "Use Smite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    var antiGapMenu = new Menu("Anti Gap Closer", "AntiGap");
                    {
                        AddBool(antiGapMenu, "Q", "Use Q");
                        foreach (var spell in
                            AntiGapcloser.Spells.Where(
                                i => HeroManager.Enemies.Any(a => i.ChampionName == a.ChampionName)))
                        {
                            AddBool(
                                antiGapMenu, spell.ChampionName + "_" + spell.Slot,
                                "-> Skill " + spell.Slot + " Of " + spell.ChampionName);
                        }
                        miscMenu.AddSubMenu(antiGapMenu);
                    }
                    var interruptMenu = new Menu("Interrupt", "Interrupt");
                    {
                        AddBool(interruptMenu, "Q", "Use Q");
                        foreach (var spell in
                            Interrupter.Spells.Where(
                                i => HeroManager.Enemies.Any(a => i.ChampionName == a.ChampionName)))
                        {
                            AddBool(
                                interruptMenu, spell.ChampionName + "_" + spell.Slot,
                                "-> Skill " + spell.Slot + " Of " + spell.ChampionName);
                        }
                        miscMenu.AddSubMenu(interruptMenu);
                    }
                    AddSlider(miscMenu, "WExtraRange", "W Extra Range Before Cancel", 60, 0, 200);
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddBool(drawMenu, "Q", "Q Range", false);
                    AddBool(drawMenu, "W", "W Range", false);
                    AddBool(drawMenu, "E", "E Range", false);
                    AddBool(drawMenu, "R", "R Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
        }

        private static bool HaveW
        {
            get { return Player.HasBuff("AuraofDespair"); }
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
            if (GetValue<bool>("Draw", "Q") && Q.Level > 0)
            {
                Render.Circle.DrawCircle(player.Position, Q.Range, Q.LSIsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "W") && W.Level > 0)
            {
                Render.Circle.DrawCircle(player.Position, W.Range, W.LSIsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "E") && E.Level > 0)
            {
                Render.Circle.DrawCircle(player.Position, E.Range, E.LSIsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "R") && R.Level > 0)
            {
                Render.Circle.DrawCircle(player.Position, R.Range, R.LSIsReady() ? Color.Green : Color.Red);
            }
        }

        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (player.IsDead || !GetValue<bool>("AntiGap", "Q") ||
                !GetValue<bool>("AntiGap", gapcloser.Sender.ChampionName + "_" + gapcloser.Slot) || !Q.LSIsReady())
            {
                return;
            }
            Q.Cast(gapcloser.Sender, PacketCast);
        }

        private static void OnPossibleToInterrupt(AIHeroClient unit, InterruptableSpell spell)
        {
            if (player.IsDead || !GetValue<bool>("Interrupt", "Q") ||
                !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot) || !Q.CanCast(unit))
            {
                return;
            }
            Q.Cast(unit, PacketCast);
        }

        private static void Fight(string mode)
        {
            if (mode == "Combo")
            {
                if (GetValue<bool>(mode, "R") && R.LSIsReady() && !player.LSIsDashing())
                {
                    var obj = GetRTarget();
                    if (((obj.Count > 1 && obj.Any(i => R.IsKillable(i))) ||
                         obj.Any(i => i.HealthPercent < GetValue<Slider>(mode, "RHpU").Value) ||
                         obj.Count >= GetValue<Slider>(mode, "RCountA").Value) && R.Cast(PacketCast))
                    {
                        return;
                    }
                }
                if (GetValue<bool>(mode, "Q") && Q.LSIsReady())
                {
                    if (GetValue<bool>(mode, "R") && R.LSIsReady(100))
                    {
                        var nearObj = new List<Obj_AI_Base>();
                        nearObj.AddRange(GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly));
                        nearObj.AddRange(HeroManager.Enemies.Where(i => i.LSIsValidTarget(Q.Range)));
                        if ((from i in nearObj
                            let enemy = GetRTarget(i.ServerPosition)
                            where
                                (enemy.Count > 1 && enemy.Any(a => R.IsKillable(a))) ||
                                enemy.Any(a => a.HealthPercent < GetValue<Slider>(mode, "RHpU").Value) ||
                                enemy.Count >= GetValue<Slider>(mode, "RCountA").Value
                            orderby enemy.Count descending
                            where Q.GetPrediction(i).Hitchance >= Q.MinHitChance
                            select i).Any(i => Q.Cast(i, PacketCast).LSIsCasted()))
                        {
                            return;
                        }
                    }
                    var target = Q.GetTarget();
                    if (target != null && !Orbwalk.InAutoAttackRange(target))
                    {
                        var state = Q.Cast(target, PacketCast);
                        if (state.LSIsCasted())
                        {
                            return;
                        }
                        if (state == Spell.CastStates.Collision && GetValue<bool>(mode, "QCol"))
                        {
                            var pred = Q.GetPrediction(target);
                            if (
                                pred.CollisionObjects.Count(
                                    i => i.IsValid<Obj_AI_Minion>() && IsSmiteable((Obj_AI_Minion) i)) == 1 &&
                                CastSmite(pred.CollisionObjects.First()) && Q.Cast(pred.CastPosition, PacketCast))
                            {
                                return;
                            }
                        }
                    }
                }
            }
            if (GetValue<bool>(mode, "E") && E.LSIsReady() && E.GetTarget() != null && E.Cast(PacketCast))
            {
                return;
            }
            if (GetValue<bool>(mode, "W") && W.LSIsReady())
            {
                if (player.ManaPercent >= GetValue<Slider>(mode, "WMpA").Value &&
                    W.GetTarget(GetValue<Slider>("Misc", "WExtraRange").Value) != null)
                {
                    if (!HaveW)
                    {
                        W.Cast(PacketCast);
                    }
                }
                else if (HaveW)
                {
                    W.Cast(PacketCast);
                }
            }
        }

        private static void Clear()
        {
            SmiteMob();
            var minionObj = GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (!minionObj.Any())
            {
                if (GetValue<bool>("Clear", "W") && W.LSIsReady() && HaveW)
                {
                    W.Cast(PacketCast);
                }
                return;
            }
            if (GetValue<bool>("Clear", "E") && E.LSIsReady() && minionObj.Any(i => E.IsInRange(i)) && E.Cast(PacketCast))
            {
                return;
            }
            if (GetValue<bool>("Clear", "W") && W.LSIsReady())
            {
                if (player.ManaPercent >= GetValue<Slider>("Clear", "WMpA").Value &&
                    (minionObj.Count(i => W.IsInRange(i, W.Range + GetValue<Slider>("Misc", "WExtraRange").Value)) > 1 ||
                     minionObj.Any(
                         i =>
                             i.MaxHealth >= 1200 &&
                             W.IsInRange(i, W.Range + GetValue<Slider>("Misc", "WExtraRange").Value))))
                {
                    if (!HaveW && W.Cast(PacketCast))
                    {
                        return;
                    }
                }
                else if (HaveW && W.Cast(PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "Q") && Q.LSIsReady())
            {
                var obj =
                    minionObj.Where(i => Q.IsKillable(i) || !Orbwalk.InAutoAttackRange(i))
                        .FirstOrDefault(i => Q.GetPrediction(i).Hitchance >= Q.MinHitChance);
                if (obj != null)
                {
                    Q.Cast(obj, PacketCast);
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
            if (GetValue<bool>("KillSteal", "Q") && Q.LSIsReady())
            {
                var target = Q.GetTarget();
                if (target != null && Q.IsKillable(target) && Q.Cast(target, PacketCast).LSIsCasted())
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "E") && E.LSIsReady())
            {
                var target = E.GetTarget();
                if (target != null && E.IsKillable(target) && E.Cast(PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "R") && R.LSIsReady())
            {
                var target = GetRTarget().FirstOrDefault(i => R.IsKillable(i));
                if (target != null)
                {
                    R.Cast(PacketCast);
                }
            }
        }

        private static List<AIHeroClient> GetRTarget(Vector3 from = new Vector3())
        {
            return
                HeroManager.Enemies.Where(
                    i =>
                        i.LSIsValidTarget() &&
                        (from.LSIsValid() ? from : player.ServerPosition).LSDistance(
                            Prediction.GetPrediction(i, 0.25f).UnitPosition) < R.Range).ToList();
        }
    }
}