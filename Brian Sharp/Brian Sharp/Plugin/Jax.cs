﻿using System;
using System.Collections.Generic;
using System.Linq;
using BrianSharp.Common;
using EloBuddy;
using LeagueSharp.Common;
using SharpDX;
using Utility = LeagueSharp.Common.Utility;
using Color = System.Drawing.Color;
using Orbwalk = BrianSharp.Common._Orbwalker;

namespace BrianSharp.Plugin
{
    internal class Jax : Helper
    {
        private static int _limitWard;

        public Jax()
        {
            Q = new Spell(SpellSlot.Q, 700);
            W = new Spell(SpellSlot.W, Orbwalk.GetAutoAttackRange(), TargetSelector.DamageType.Magical);
            E = new Spell(SpellSlot.E, 375);
            R = new Spell(SpellSlot.R);

            var champMenu = new Menu("Plugin", Player.Instance.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddBool(comboMenu, "Q", "Use Q");
                    AddBool(comboMenu, "W", "Use W");
                    AddBool(comboMenu, "E", "Use E");
                    AddSlider(comboMenu, "ECountA", "-> Cancel If Enemy >=", 2, 1, 5);
                    AddBool(comboMenu, "R", "Use R");
                    AddSlider(comboMenu, "RHpU", "-> If Player Hp <", 60);
                    AddSlider(comboMenu, "RCountA", "-> Or Enemy >=", 2, 1, 5);
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddBool(harassMenu, "Q", "Use Q");
                    AddSlider(harassMenu, "QHpA", "-> If Hp >=", 20);
                    AddBool(harassMenu, "W", "Use W");
                    AddBool(harassMenu, "E", "Use E");
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddSmiteMob(clearMenu);
                    AddBool(clearMenu, "Q", "Use Q");
                    AddBool(clearMenu, "W", "Use W");
                    AddBool(clearMenu, "E", "Use E");
                    AddBool(clearMenu, "Item", "Use Tiamat/Hydra");
                    champMenu.AddSubMenu(clearMenu);
                }
                var lastHitMenu = new Menu("Last Hit", "LastHit");
                {
                    AddBool(lastHitMenu, "W", "Use W");
                    champMenu.AddSubMenu(lastHitMenu);
                }
                var fleeMenu = new Menu("Flee", "Flee");
                {
                    AddBool(fleeMenu, "Q", "Use Q");
                    AddBool(fleeMenu, "PinkWard", "-> Ward Jump Use Pink Ward", false);
                    champMenu.AddSubMenu(fleeMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddBool(killStealMenu, "Q", "Use Q");
                        AddBool(killStealMenu, "W", "Use W");
                        AddBool(killStealMenu, "Ignite", "Use Ignite");
                        AddBool(killStealMenu, "Smite", "Use Smite");
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
            Orbwalk.AfterAttack += AfterAttack;
            GameObject.OnCreate += OnCreateWardForFlee;
        }

        private static bool HaveW
        {
            get { return Player.HasBuff("JaxEmpowerTwo"); }
        }

        private static bool HaveE
        {
            get { return Player.HasBuff("JaxCounterStrike"); }
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
                    Flee(Game.CursorPos);
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
            E.Cast(PacketCast);
        }

        private static void OnPossibleToInterrupt(AIHeroClient unit, InterruptableSpell spell)
        {
            if (player.IsDead || !GetValue<bool>("Interrupt", "E") ||
                !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot) || !E.LSIsReady())
            {
                return;
            }
            if (E.IsInRange(unit))
            {
                E.Cast(PacketCast);
            }
            else if (Q.CanCast(unit) && player.Mana >= Q.ManaCost + (HaveE ? 0 : E.ManaCost))
            {
                Q.CastOnUnit(unit, PacketCast);
            }
        }

        private static void AfterAttack(AttackableUnit target)
        {
            if (!W.LSIsReady())
            {
                return;
            }
            if ((((Orbwalk.CurrentMode == _Orbwalker.Mode.Combo || Orbwalk.CurrentMode == _Orbwalker.Mode.Harass) &&
                  target is AIHeroClient) || (Orbwalk.CurrentMode == _Orbwalker.Mode.Clear && target is Obj_AI_Minion)) &&
                GetValue<bool>(Orbwalk.CurrentMode.ToString(), "W") && W.Cast(PacketCast))
            {
                Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }
        }

        private static void OnCreateWardForFlee(GameObject sender, EventArgs args)
        {
            if (Orbwalk.CurrentMode != _Orbwalker.Mode.Flee || !Q.LSIsReady() || !sender.IsValid<Obj_AI_Minion>())
            {
                return;
            }
            var ward = (Obj_AI_Minion) sender;
            if (!ward.IsAlly || !IsWard(ward) || !Q.IsInRange(ward) || Utils.GameTimeTickCount - _limitWard > 1000)
            {
                return;
            }
            Utility.DelayAction.Add(
                50, () =>
                {
                    var buff = ward.GetBuff("sharedstealthwardbuff") ?? ward.GetBuff("sharedvisionwardbuff");
                    if (buff != null && buff.Caster.IsMe)
                    {
                        Q.CastOnUnit(ward, PacketCast);
                    }
                });
        }

        private static void Fight(string mode)
        {
            if (GetValue<bool>(mode, "E") && E.LSIsReady())
            {
                if (!HaveE)
                {
                    if (GetValue<bool>(mode, "Q") && Q.LSIsReady() && E.GetTarget() == null)
                    {
                        var target = Q.GetTarget();
                        if (target != null && E.Cast(PacketCast) && Q.CastOnUnit(target, PacketCast))
                        {
                            return;
                        }
                    }
                    else if (E.GetTarget() != null && E.Cast(PacketCast))
                    {
                        return;
                    }
                }
                else if ((player.LSCountEnemiesInRange(E.Range) >= GetValue<Slider>(mode, "ECountA").Value ||
                          player.LSGetEnemiesInRange(E.Range).Any(i => i.LSIsValidTarget() && !E.IsInRange(i, E.Range - 50))) &&
                         E.Cast(PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>(mode, "W") && W.LSIsReady() && GetValue<bool>(mode, "Q") && Q.LSIsReady() &&
                player.Mana >= W.ManaCost + Q.ManaCost)
            {
                var target = Q.GetTarget();
                if (target != null && CanKill(target, GetBonusDmg(target) + Q.GetDamage(target)) && W.Cast(PacketCast) &&
                    Q.CastOnUnit(target, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>(mode, "Q") && Q.LSIsReady())
            {
                var target = Q.GetTarget();
                if (target != null)
                {
                    if (CanKill(target, Q.GetDamage(target) + (HaveW ? GetBonusDmg(target) : 0)) &&
                        Q.CastOnUnit(target, PacketCast))
                    {
                        return;
                    }
                    if (mode == "Combo" || player.HealthPercent >= GetValue<Slider>(mode, "QHpA").Value)
                    {
                        if ((!Orbwalk.InAutoAttackRange(target, 30) ||
                             (GetValue<bool>(mode, "E") && E.LSIsReady() && HaveE && !E.IsInRange(target))) &&
                            Q.CastOnUnit(target, PacketCast))
                        {
                            return;
                        }
                    }
                }
            }
            if (mode == "Combo" && GetValue<bool>(mode, "R") && R.LSIsReady() && !player.LSInFountain() &&
                (player.HealthPercent < GetValue<Slider>(mode, "RHpU").Value ||
                 player.LSCountEnemiesInRange(Q.Range) >= GetValue<Slider>(mode, "RCountA").Value))
            {
                R.Cast(PacketCast);
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
            if (GetValue<bool>("Clear", "E") && E.LSIsReady() && !HaveE)
            {
                if (GetValue<bool>("Clear", "Q") && Q.LSIsReady() && !minionObj.Any(i => E.IsInRange(i)))
                {
                    var obj =
                        minionObj.MaxOrDefault(
                            i => GetMinions(i.ServerPosition, E.Range, MinionTypes.All, MinionTeam.NotAlly).Count > 1);
                    if (obj != null && E.Cast(PacketCast) && Q.CastOnUnit(obj, PacketCast))
                    {
                        return;
                    }
                }
                else if ((minionObj.Any(i => i.MaxHealth >= 1200 && E.IsInRange(i)) ||
                          minionObj.Count(i => E.IsInRange(i)) > 2) && E.Cast(PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "W"))
            {
                if (W.LSIsReady() && GetValue<bool>("Clear", "Q") && Q.LSIsReady() &&
                    player.Mana >= W.ManaCost + Q.ManaCost)
                {
                    var obj =
                        minionObj.FirstOrDefault(
                            i => i.MaxHealth >= 1200 && CanKill(i, GetBonusDmg(i) + Q.GetDamage(i)));
                    if (obj != null && W.Cast(PacketCast) && Q.CastOnUnit(obj, PacketCast))
                    {
                        return;
                    }
                }
                if (W.LSIsReady() || HaveW)
                {
                    var obj =
                        minionObj.Where(i => Orbwalk.InAutoAttackRange(i))
                            .FirstOrDefault(i => CanKill(i, GetBonusDmg(i)));
                    if (obj != null)
                    {
                        if (!HaveW)
                        {
                            W.Cast(PacketCast);
                        }
                        Orbwalk.Move = false;
                        Orbwalk.Attack = false;
                        Player.IssueOrder(GameObjectOrder.AttackUnit, obj);
                        Orbwalk.Move = true;
                        Orbwalk.Attack = true;
                    }
                }
            }
            if (GetValue<bool>("Clear", "Q") && Q.LSIsReady())
            {
                var obj =
                    minionObj.FirstOrDefault(
                        i => i.MaxHealth >= 1200 && CanKill(i, Q.GetDamage(i) + (HaveW ? GetBonusDmg(i) : 0)));
                if (obj == null &&
                    (!minionObj.Any(i => Orbwalk.InAutoAttackRange(i, 40)) ||
                     (GetValue<bool>("Clear", "E") && E.LSIsReady() && HaveE && !minionObj.Any(i => E.IsInRange(i)))))
                {
                    obj = minionObj.MinOrDefault(i => i.Health);
                }
                if (obj != null && Q.CastOnUnit(obj, PacketCast))
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

        private static void LastHit()
        {
            if (!GetValue<bool>("LastHit", "W") || (!W.LSIsReady() && !HaveW))
            {
                return;
            }
            var obj =
                GetMinions(W.Range + 100, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                    .Where(i => Orbwalk.InAutoAttackRange(i))
                    .FirstOrDefault(i => CanKill(i, GetBonusDmg(i)));
            if (obj == null)
            {
                return;
            }
            if (!HaveW)
            {
                W.Cast(PacketCast);
            }
            Orbwalk.Move = false;
            Orbwalk.Attack = false;
            Player.IssueOrder(GameObjectOrder.AttackUnit, obj);
            Orbwalk.Move = true;
            Orbwalk.Attack = true;
        }

        private static void Flee(Vector3 pos)
        {
            if (!GetValue<bool>("Flee", "Q") || !Q.LSIsReady() || Utils.GameTimeTickCount - _limitWard <= 1000)
            {
                return;
            }
            var posJump = player.ServerPosition.LSExtend(pos, Math.Min(Q.Range, player.LSDistance(pos)));
            var objNear = new List<Obj_AI_Base>();
            objNear.AddRange(HeroManager.Allies.Where(i => i.LSIsValidTarget(Q.Range, false) && !i.IsMe));
            objNear.AddRange(GetMinions(Q.Range, MinionTypes.All, MinionTeam.Ally));
            objNear.AddRange(
                ObjectManager.Get<Obj_AI_Minion>().Where(i => i.LSIsValidTarget(Q.Range, false) && i.IsAlly && IsWard(i)));
            var objJump = objNear.Where(i => i.LSDistance(posJump) < 200).MinOrDefault(i => i.LSDistance(posJump));
            if (objJump != null)
            {
                Q.CastOnUnit(objJump, PacketCast);
            }
            else if (GetWardSlot != null)
            {
                var posPlace = player.ServerPosition.LSExtend(pos, Math.Min(GetWardRange - 10, player.LSDistance(pos)));
                if (player.Spellbook.CastSpell(GetWardSlot.SpellSlot, posPlace))
                {
                    _limitWard = Utils.GameTimeTickCount;
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
            if (GetValue<bool>("KillSteal", "W") && (W.LSIsReady() || HaveW))
            {
                var target = W.GetTarget();
                if (target != null && CanKill(target, GetBonusDmg(target)))
                {
                    if (!HaveW)
                    {
                        W.Cast(PacketCast);
                    }
                    Orbwalk.Move = false;
                    Orbwalk.Attack = false;
                    Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                    Orbwalk.Move = true;
                    Orbwalk.Attack = true;
                }
            }
            if (GetValue<bool>("KillSteal", "Q") && Q.LSIsReady())
            {
                var target = Q.GetTarget();
                if (target != null)
                {
                    if (W.LSIsReady() && player.Mana >= W.ManaCost + Q.ManaCost &&
                        CanKill(target, GetBonusDmg(target) + Q.GetDamage(target)) && W.Cast(PacketCast) &&
                        Q.CastOnUnit(target, PacketCast))
                    {
                        return;
                    }
                    if (CanKill(target, Q.GetDamage(target) + (HaveW ? GetBonusDmg(target) : 0)))
                    {
                        Q.CastOnUnit(target, PacketCast);
                    }
                }
            }
        }

        private static double GetBonusDmg(Obj_AI_Base target)
        {
            var dmgItem = 0d;
            if (Sheen.IsOwned() && (Sheen.IsReady() || Player.HasBuff("Sheen")))
            {
                dmgItem = player.BaseAttackDamage;
            }
            if (Trinity.IsOwned() && (Trinity.IsReady() || Player.HasBuff("Sheen")))
            {
                dmgItem = player.BaseAttackDamage * 2;
            }
            var dmgR = 0d;
            var pBuff = player.GetBuffCount("JaxRelentlessAssaultAS");
            if (R.Level > 0 && !(target is Obj_AI_Turret) && (pBuff == 2 || pBuff >= 5))
            {
                dmgR = R.GetDamage(target);
            }
            return dmgR + (W.LSIsReady() || HaveW ? W.GetDamage(target) : 0) + player.LSGetAutoAttackDamage(target, true) +
                   (dmgItem > 0 ? player.CalcDamage(target, Damage.DamageType.Physical, dmgItem) : 0);
        }
    }
}