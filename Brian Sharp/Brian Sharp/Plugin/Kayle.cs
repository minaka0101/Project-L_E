﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BrianSharp.Common;
using EloBuddy;
using LeagueSharp.Common;
using Orbwalk = BrianSharp.Common._Orbwalker;

namespace BrianSharp.Plugin
{
    internal class Kayle : Helper
    {
        private static readonly Dictionary<int, RAntiItem> RAntiDetected = new Dictionary<int, RAntiItem>();

        public Kayle()
        {
            Q = new Spell(SpellSlot.Q, 650, TargetSelector.DamageType.Magical);
            W = new Spell(SpellSlot.W, 900);
            E = new Spell(SpellSlot.E, 525);
            R = new Spell(SpellSlot.R, 900);

            var champMenu = new Menu("Plugin", Player.Instance.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    var healMenu = new Menu("Heal (W)", "Heal");
                    {
                        foreach (var name in HeroManager.Allies.Select(MenuName))
                        {
                            AddBool(healMenu, name, name, false);
                            AddSlider(healMenu, name + "HpU", "-> If Hp <", 40);
                        }
                        comboMenu.AddSubMenu(healMenu);
                    }
                    var saveMenu = new Menu("Save (R)", "Save");
                    {
                        foreach (var name in HeroManager.Allies.Select(MenuName))
                        {
                            AddBool(saveMenu, name, name, false);
                            AddSlider(saveMenu, name + "HpU", "-> If Hp <", 30);
                        }
                        comboMenu.AddSubMenu(saveMenu);
                    }
                    var antiMenu = new Menu("Anti (R)", "Anti");
                    {
                        AddBool(antiMenu, "Fizz", "Fizz", false);
                        AddBool(antiMenu, "Karthus", "Karthus", false);
                        AddBool(antiMenu, "Vlad", "Vladimir", false);
                        AddBool(antiMenu, "Zed", "Zed", false);
                        comboMenu.AddSubMenu(antiMenu);
                    }
                    AddBool(comboMenu, "Q", "Use Q");
                    AddBool(comboMenu, "W", "Use W");
                    AddBool(comboMenu, "WSpeed", "-> Speed");
                    AddBool(comboMenu, "WHeal", "-> Heal");
                    AddBool(comboMenu, "E", "Use E");
                    AddBool(comboMenu, "EAoE", "-> Focus Most AoE Target");
                    AddBool(comboMenu, "R", "Use R");
                    AddBool(comboMenu, "RSave", "-> Save");
                    AddList(
                        comboMenu, "RAnti", "-> Anti Dangerous Ultimate", new[] { "Off", "Self", "Ally", "Both" }, 3);
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddKeybind(harassMenu, "AutoQ", "Auto Q", "H", KeyBindType.Toggle);
                    AddSlider(harassMenu, "AutoQMpA", "-> If Mp >=", 50);
                    AddBool(harassMenu, "Q", "Use Q");
                    AddBool(harassMenu, "E", "Use E");
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddSmiteMob(clearMenu);
                    AddBool(clearMenu, "Q", "Use Q");
                    AddBool(clearMenu, "E", "Use E");
                    champMenu.AddSubMenu(clearMenu);
                }
                var lastHitMenu = new Menu("Last Hit", "LastHit");
                {
                    AddBool(lastHitMenu, "Q", "Use Q");
                    champMenu.AddSubMenu(lastHitMenu);
                }
                var fleeMenu = new Menu("Flee", "Flee");
                {
                    AddBool(fleeMenu, "Q", "Use Q To Slow Enemy");
                    AddBool(fleeMenu, "W", "Use W");
                    champMenu.AddSubMenu(fleeMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddBool(killStealMenu, "Q", "Use Q");
                        AddBool(killStealMenu, "Ignite", "Use Ignite");
                        AddBool(killStealMenu, "Smite", "Use Smite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddBool(drawMenu, "Q", "Q Range", false);
                    AddBool(drawMenu, "W", "W Range", false);
                    AddBool(drawMenu, "R", "R Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            AttackableUnit.OnDamage += OnDamage;
        }

        private static bool HaveE
        {
            get { return Player.HasBuff("JudicatorRighteousFury"); }
        }

        private static void OnUpdate(EventArgs args)
        {
            AntiDetect();
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
                case _Orbwalker.Mode.Flee:
                    Flee();
                    break;
            }
            if (GetValue<bool>("SmiteMob", "Auto") && Orbwalk.CurrentMode != _Orbwalker.Mode.Clear)
            {
                SmiteMob();
            }
            AutoQ();
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
            if (GetValue<bool>("Draw", "R") && R.Level > 0)
            {
                Render.Circle.DrawCircle(player.Position, R.Range, R.LSIsReady() ? Color.Green : Color.Red);
            }
        }

        private static void OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            if (Orbwalk.CurrentMode != _Orbwalker.Mode.Combo)
            {
                return;
            }
            if (GetValue<bool>("Combo", "W") && GetValue<bool>("Combo", "WHeal") && W.LSIsReady())
            {
                var obj = ObjectManager.GetUnitByNetworkId<AIHeroClient>((uint)args.Target.NetworkId);
                if (obj.LSIsValidTarget(W.Range, false) && obj.IsAlly && GetValue<bool>("Heal", MenuName(obj)) &&
                    obj.HealthPercent < GetValue<Slider>("Heal", MenuName(obj) + "HpU").Value && !obj.LSInFountain() &&
                    !obj.HasBuff("JudicatorIntervention") && !obj.HasBuff("UndyingRage") &&
                    W.CastOnUnit(obj, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Combo", "R") && GetValue<bool>("Combo", "RSave") && R.LSIsReady())
            {
                var obj = ObjectManager.GetUnitByNetworkId<AIHeroClient>((uint) args.Target.NetworkId);
                if (obj.LSIsValidTarget(R.Range, false) && obj.IsAlly && GetValue<bool>("Save", MenuName(obj)) &&
                    obj.HealthPercent < GetValue<Slider>("Save", MenuName(obj) + "HpU").Value && !obj.LSInFountain() &&
                    !obj.HasBuff("UndyingRage"))
                {
                    R.CastOnUnit(obj, PacketCast);
                }
            }
        }

        private static void Fight(string mode)
        {
            if (mode == "Combo" && GetValue<bool>(mode, "E") && GetValue<bool>(mode, "EAoE") && HaveE)
            {
                var target =
                    HeroManager.Enemies.Where(i => Orbwalk.InAutoAttackRange(i))
                        .MaxOrDefault(i => i.LSCountEnemiesInRange(150));
                if (target != null)
                {
                    Orbwalk.ForcedTarget = target;
                }
            }
            else
            {
                Orbwalk.ForcedTarget = null;
            }
            if (GetValue<bool>(mode, "Q"))
            {
                var target = Q.GetTarget();
                if (target != null &&
                    ((player.LSDistance(target) > Q.Range - 100 && !target.LSIsFacing(player) && player.LSIsFacing(target)) ||
                     target.HealthPercent > 60 || player.LSCountEnemiesInRange(Q.Range) == 1) &&
                    Q.CastOnUnit(target, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>(mode, "E") && E.LSIsReady() && E.GetTarget() != null && E.Cast(PacketCast))
            {
                return;
            }
            if (mode != "Combo")
            {
                return;
            }
            if (GetValue<bool>(mode, "W") && GetValue<bool>(mode, "WSpeed") && W.LSIsReady())
            {
                var target = Q.GetTarget(200);
                if (target != null && !target.LSIsFacing(player) && (!HaveE || !Orbwalk.InAutoAttackRange(target)) &&
                    (!GetValue<bool>(mode, "Q") || (Q.LSIsReady() && !Q.IsInRange(target))) && W.Cast(PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>(mode, "R") && GetValue<StringList>(mode, "RAnti").SelectedIndex > 0 && R.LSIsReady())
            {
                var obj =
                    HeroManager.Allies.Where(
                        i =>
                            i.LSIsValidTarget(R.Range, false) && RAntiDetected.ContainsKey(i.NetworkId) &&
                            Game.Time > RAntiDetected[i.NetworkId].StartTick && !i.HasBuff("UndyingRage"))
                        .MinOrDefault(i => i.Health);
                if (obj != null)
                {
                    R.CastOnUnit(obj, PacketCast);
                }
            }
        }

        private static void Clear()
        {
            SmiteMob();
            var minionObj = GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (!minionObj.Any())
            {
                return;
            }
            if (GetValue<bool>("Clear", "Q") && Q.LSIsReady())
            {
                var obj = minionObj.FirstOrDefault(i => Q.IsKillable(i)) ??
                          minionObj.FirstOrDefault(i => i.MaxHealth >= 1200);
                if (obj != null && Q.CastOnUnit(obj, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "E") && E.LSIsReady() &&
                (minionObj.Count > 1 || minionObj.Any(i => i.MaxHealth >= 1200)))
            {
                E.Cast(PacketCast);
            }
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

        private static void Flee()
        {
            if (GetValue<bool>("Flee", "W") && W.LSIsReady() && W.Cast(PacketCast))
            {
                return;
            }
            if (GetValue<bool>("Flee", "Q"))
            {
                Q.CastOnBestTarget(0, PacketCast);
            }
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
                if (target != null && Q.IsKillable(target))
                {
                    Q.CastOnUnit(target, PacketCast);
                }
            }
        }

        private static void AntiDetect()
        {
            if (player.IsDead || GetValue<StringList>("Combo", "RAnti").SelectedIndex == 0 || R.Level == 0)
            {
                return;
            }
            var key =
                HeroManager.Allies.FirstOrDefault(
                    i => RAntiDetected.ContainsKey(i.NetworkId) && Game.Time > RAntiDetected[i.NetworkId].EndTick);
            if (key != null)
            {
                RAntiDetected.Remove(key.NetworkId);
            }
            foreach (var obj in
                HeroManager.Allies.Where(i => !i.IsDead && !RAntiDetected.ContainsKey(i.NetworkId)))
            {
                if ((GetValue<StringList>("Combo", "RAnti").SelectedIndex != 1 || !obj.IsMe) &&
                    (GetValue<StringList>("Combo", "RAnti").SelectedIndex != 2 || obj.IsMe) &&
                    GetValue<StringList>("Combo", "RAnti").SelectedIndex != 3)
                {
                    continue;
                }
                foreach (var buff in obj.Buffs)
                {
                    if ((buff.DisplayName == "ZedUltExecute" && GetValue<bool>("Anti", "Zed")) ||
                        (buff.DisplayName == "FizzChurnTheWatersCling" && GetValue<bool>("Anti", "Fizz")) ||
                        (buff.DisplayName == "VladimirHemoplagueDebuff" && GetValue<bool>("Anti", "Vlad")))
                    {
                        RAntiDetected.Add(obj.NetworkId, new RAntiItem(buff));
                    }
                    else if (buff.DisplayName == "KarthusFallenOne" && GetValue<bool>("Anti", "Karthus") &&
                             obj.Health <=
                             ((AIHeroClient) buff.Caster).LSGetSpellDamage(obj, SpellSlot.R) + obj.Health * 0.2f &&
                             obj.LSCountEnemiesInRange(R.Range) > 0)
                    {
                        RAntiDetected.Add(obj.NetworkId, new RAntiItem(buff));
                    }
                }
            }
        }

        private static string MenuName(AIHeroClient obj)
        {
            return obj.IsMe ? "Self" : obj.ChampionName;
        }

        private class RAntiItem
        {
            public readonly float EndTick;
            public readonly float StartTick;

            public RAntiItem(BuffInstance buff)
            {
                StartTick = Game.Time + (buff.EndTime - buff.StartTime) - (R.Level * 0.5f + 1);
                EndTick = Game.Time + (buff.EndTime - buff.StartTime);
            }
        }
    }
}