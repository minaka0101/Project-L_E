﻿using System;
using System.Drawing;
using System.Linq;
using BrianSharp.Common;
using EloBuddy;
using Utility = LeagueSharp.Common.Utility;
using LeagueSharp.Common;
using Orbwalk = BrianSharp.Common._Orbwalker;

namespace BrianSharp.Plugin
{
    internal class Shen : Helper
    {
        private static AIHeroClient _alertAlly;
        private static bool _alertCasted;

        public Shen()
        {
            Q = new Spell(SpellSlot.Q, 485, TargetSelector.DamageType.Magical);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 650, TargetSelector.DamageType.Magical);
            R = new Spell(SpellSlot.R);
            E.SetSkillshot(0, 50, 1600, false, SkillshotType.SkillshotLine);

            var champMenu = new Menu("Plugin", Player.Instance.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddBool(comboMenu, "Q", "Use Q");
                    AddBool(comboMenu, "W", "Use W");
                    AddSlider(comboMenu, "WHpU", "-> If Hp <", 20);
                    AddBool(comboMenu, "E", "Use E");
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddKeybind(harassMenu, "AutoQ", "Auto Q", "H", KeyBindType.Toggle);
                    AddSlider(harassMenu, "AutoQMpA", "-> If Mp >=", 50);
                    AddBool(harassMenu, "Q", "Use Q");
                    AddBool(harassMenu, "E", "Use E");
                    AddSlider(harassMenu, "EHpA", "-> If Hp >=", 20);
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddBool(clearMenu, "Q", "Use Q");
                    AddBool(clearMenu, "W", "Use W");
                    champMenu.AddSubMenu(clearMenu);
                }
                var lastHitMenu = new Menu("Last Hit", "LastHit");
                {
                    AddBool(lastHitMenu, "Q", "Use Q");
                    champMenu.AddSubMenu(lastHitMenu);
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
                        AddBool(killStealMenu, "Q", "Use Q");
                        AddBool(killStealMenu, "E", "Use E");
                        AddBool(killStealMenu, "Ignite", "Use Ignite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    var antiGapMenu = new Menu("Anti Gap Closer", "AntiGap");
                    {
                        AddBool(antiGapMenu, "E", "Use E");
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
                        AddBool(interruptMenu, "E", "Use E");
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
                    var ultiMenu = new Menu("Ultimate", "Ultimate");
                    {
                        var saveMenu = new Menu("Ally", "Ally");
                        {
                            foreach (var obj in HeroManager.Allies.Where(i => !i.IsMe))
                            {
                                AddBool(saveMenu, obj.ChampionName, obj.ChampionName, false);
                            }
                            ultiMenu.AddSubMenu(saveMenu);
                        }
                        AddBool(ultiMenu, "Alert", "Alert Ally");
                        AddSlider(ultiMenu, "AlertHpU", "-> If Hp <", 30);
                        AddBool(ultiMenu, "Save", "-> Save Ally");
                        AddKeybind(ultiMenu, "SaveKey", "--> Key", "T");
                        miscMenu.AddSubMenu(ultiMenu);
                    }
                    AddBool(miscMenu, "ETower", "Auto E If Enemy Under Tower");
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddBool(drawMenu, "Q", "Q Range", false);
                    AddBool(drawMenu, "E", "E Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
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
                case _Orbwalker.Mode.Flee:
                    if (GetValue<bool>("Flee", "E") && E.LSIsReady() && E.Cast(Game.CursorPos, PacketCast))
                    {
                        return;
                    }
                    break;
            }
            AutoQ();
            KillSteal();
            UltimateAlert();
            AutoEUnderTower();
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
            if (GetValue<bool>("Draw", "E") && E.Level > 0)
            {
                Render.Circle.DrawCircle(player.Position, E.Range, E.LSIsReady() ? Color.Green : Color.Red);
            }
        }

        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (player.IsDead || !GetValue<bool>("AntiGap", "E") ||
                !GetValue<bool>("AntiGap", gapcloser.Sender.ChampionName + "_" + gapcloser.Slot) ||
                !E.CanCast(gapcloser.Sender))
            {
                return;
            }
            var predE = E.GetPrediction(gapcloser.Sender, true);
            if (predE.Hitchance >= E.MinHitChance)
            {
                E.Cast(predE.CastPosition.LSExtend(player.ServerPosition, -100), PacketCast);
            }
        }

        private static void OnPossibleToInterrupt(AIHeroClient unit, InterruptableSpell spell)
        {
            if (player.IsDead || !GetValue<bool>("Interrupt", "Q") ||
                !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot) || !E.CanCast(unit))
            {
                return;
            }
            var predE = E.GetPrediction(unit, true);
            if (predE.Hitchance >= E.MinHitChance)
            {
                E.Cast(predE.CastPosition.LSExtend(player.ServerPosition, -100), PacketCast);
            }
        }

        private static void Fight(string mode)
        {
            if (GetValue<bool>(mode, "E") &&
                (mode == "Combo" || player.HealthPercent >= GetValue<Slider>(mode, "EHpA").Value))
            {
                var target = E.GetTarget(E.Width);
                if (target != null)
                {
                    var predE = E.GetPrediction(target, true);
                    if (predE.Hitchance >= E.MinHitChance &&
                        E.Cast(predE.CastPosition.LSExtend(player.ServerPosition, -100), PacketCast))
                    {
                        if (mode == "Combo" && GetValue<bool>(mode, "W") && W.LSIsReady())
                        {
                            W.Cast(PacketCast);
                        }
                        return;
                    }
                }
            }
            if (GetValue<bool>(mode, "Q") && Q.CastOnBestTarget(0, PacketCast).LSIsCasted())
            {
                return;
            }
            if (mode == "Combo" && GetValue<bool>(mode, "W") && W.LSIsReady() && Q.GetTarget() != null &&
                player.HealthPercent < GetValue<Slider>(mode, "WHpU").Value)
            {
                W.Cast(PacketCast);
            }
        }

        private static void Clear()
        {
            var minionObj = GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (!minionObj.Any())
            {
                return;
            }
            if (GetValue<bool>("Clear", "Q") && Q.LSIsReady())
            {
                var obj = minionObj.FirstOrDefault(i => Q.IsKillable(i)) ?? minionObj.MinOrDefault(i => i.Health);
                if (obj != null && Q.CastOnUnit(obj, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "W") && W.LSIsReady() &&
                (minionObj.Count > 1 || minionObj.Any(i => i.MaxHealth >= 1200)))
            {
                W.Cast(PacketCast);
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
            if (GetValue<bool>("KillSteal", "Q") && Q.LSIsReady())
            {
                var target = Q.GetTarget();
                if (target != null && Q.IsKillable(target) && Q.CastOnUnit(target, PacketCast))
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

        private static void UltimateAlert()
        {
            if (!GetValue<bool>("Ultimate", "Alert") || !R.LSIsReady())
            {
                _alertAlly = null;
                return;
            }
            if (!_alertCasted)
            {
                var obj =
                    HeroManager.Allies.Where(
                        i =>
                            !i.IsMe && i.LSIsValidTarget(R.Range, false) && GetValue<bool>("Ally", i.ChampionName) &&
                            i.HealthPercent < GetValue<Slider>("Ultimate", "AlertHpU").Value &&
                            i.LSCountEnemiesInRange(E.Range) > 0).MinOrDefault(i => i.Health);
                if (obj != null)
                {
                    AddNotif(string.Format("[Brian Sharp] - {0}: In Dangerous", obj.ChampionName), 5000);
                    _alertAlly = obj;
                    _alertCasted = true;
                    Utility.DelayAction.Add(
                        5000, () =>
                        {
                            _alertAlly = null;
                            _alertCasted = false;
                        });
                    return;
                }
            }
            if (GetValue<bool>("Ultimate", "Save") && GetValue<KeyBind>("Ultimate", "SaveKey").Active &&
                _alertAlly.LSIsValidTarget(R.Range, false))
            {
                R.CastOnUnit(_alertAlly, PacketCast);
            }
        }

        private static void AutoEUnderTower()
        {
            if (!GetValue<bool>("Misc", "ETower") || !E.LSIsReady())
            {
                return;
            }
            var target = HeroManager.Enemies.Where(i => i.LSIsValidTarget(E.Range)).MinOrDefault(i => i.LSDistance(player));
            var tower =
                ObjectManager.Get<Obj_AI_Turret>()
                    .FirstOrDefault(i => i.IsAlly && !i.IsDead && i.LSDistance(player) <= 850);
            if (target != null && tower != null && target.LSDistance(tower) <= 850)
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