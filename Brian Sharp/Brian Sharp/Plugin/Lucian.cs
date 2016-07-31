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
    internal class Lucian : Helper
    {
        private static bool _qCasted, _wCasted, _eCasted;

        public Lucian()
        {
            Q = new Spell(SpellSlot.Q, 675);
            Q2 = new Spell(SpellSlot.Q, 1100);
            W = new Spell(SpellSlot.W, 1000, TargetSelector.DamageType.Magical);
            W2 = new Spell(SpellSlot.W, 1000, TargetSelector.DamageType.Magical);
            E = new Spell(SpellSlot.E, 425);
            R = new Spell(SpellSlot.R, 1400);
            Q2.SetSkillshot(0.5f, 65, float.MaxValue, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 150, 1600, true, SkillshotType.SkillshotCircle);
            W2.SetSkillshot(0.25f, 150, 1600, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.5f, 110, 2800, true, SkillshotType.SkillshotLine);

            var champMenu = new Menu("Plugin", Player.Instance.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddBool(comboMenu, "P", "Use Passive");
                    AddBool(comboMenu, "PSave", "-> Always Save", false);
                    AddBool(comboMenu, "Q", "Use Q");
                    AddBool(comboMenu, "QExtend", "-> Extend");
                    AddBool(comboMenu, "W", "Use W");
                    AddBool(comboMenu, "E", "Use E");
                    AddBool(comboMenu, "EGap", "-> Gap Closer");
                    AddSlider(comboMenu, "EDelay", "-> Stop Q/W If E Will Ready In (ms)", 500, 100, 1000);
                    AddList(comboMenu, "EMode", "-> Mode", new[] { "Safe", "Mouse", "Chase" });
                    AddKeybind(comboMenu, "EModeKey", "--> Key Switch", "Z", KeyBindType.Toggle).ValueChanged +=
                        ComboEModeChanged;
                    AddBool(comboMenu, "EModeDraw", "--> Draw Text", false);
                    AddBool(comboMenu, "R", "Use R If Killable");
                    AddBool(comboMenu, "RItem", "-> Use Youmuu For More Damage");
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddKeybind(harassMenu, "AutoQ", "Auto Q (Only Extend)", "H", KeyBindType.Toggle);
                    AddSlider(harassMenu, "AutoQMpA", "-> If Mp >=", 50);
                    AddBool(harassMenu, "P", "Use Passive");
                    AddBool(harassMenu, "PSave", "-> Always Save", false);
                    AddBool(harassMenu, "Q", "Use Q");
                    AddBool(harassMenu, "W", "Use W");
                    AddBool(harassMenu, "E", "Use E");
                    AddSlider(harassMenu, "EHpA", "-> If Hp >=", 20);
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddBool(clearMenu, "Q", "Use Q");
                    AddBool(clearMenu, "W", "Use W");
                    AddBool(clearMenu, "E", "Use E");
                    AddSlider(clearMenu, "EDelay", "-> Stop Q/W If E Will Ready In (ms)", 500, 100, 1000);
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
                        AddBool(killStealMenu, "RStop", "Stop R For Kill Steal");
                        AddBool(killStealMenu, "Q", "Use Q");
                        AddBool(killStealMenu, "W", "Use W");
                        AddBool(killStealMenu, "Ignite", "Use Ignite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    AddBool(miscMenu, "LockR", "Lock R On Target");
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
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Orbwalk.AfterAttack += AfterAttack;
        }

        private static bool HavePassive
        {
            get
            {
                return (Orbwalk.CurrentMode == _Orbwalker.Mode.Clear ||
                        GetValue<bool>(Orbwalk.CurrentMode.ToString(), "P")) &&
                       (_qCasted || _wCasted || _eCasted || Player.HasBuff("LucianPassiveBuff"));
            }
        }

        private static void ComboEModeChanged(object sender, OnValueChangeEventArgs e)
        {
            var mode = GetValue<StringList>("Combo", "EMode").SelectedIndex;
            GetItem("Combo", "EMode")
                .SetValue(new StringList(GetValue<StringList>("Combo", "EMode").SList, mode == 2 ? 0 : mode + 1));
        }

        private static void OnUpdate(EventArgs args)
        {
            if (player.IsDead || MenuGUI.IsChatOpen || player.LSIsRecalling())
            {
                return;
            }
            KillSteal();
            if (player.IsCastingInterruptableSpell(true))
            {
                if (GetValue<bool>("Misc", "LockR"))
                {
                    LockROnTarget();
                }
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
            AutoQ();
        }

        private static void OnDraw(EventArgs args)
        {
            if (player.IsDead)
            {
                return;
            }
            if (GetValue<bool>("Combo", "E") && GetValue<bool>("Combo", "EModeDraw"))
            {
                var pos = Drawing.WorldToScreen(player.Position);
                Drawing.DrawText(pos.X, pos.Y, Color.Orange, GetValue<StringList>("Combo", "EMode").SelectedValue);
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

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (args.SData.Name == "LucianQ")
            {
                _qCasted = true;
                Utility.DelayAction.Add((int) (Q2.Delay * 1000) + 50, () => _qCasted = false);
            }
            if (args.SData.Name == "LucianW")
            {
                _wCasted = true;
                Utility.DelayAction.Add((int) (W.Delay * 1000) + 50, () => _wCasted = false);
            }
            if (args.SData.Name == "LucianE")
            {
                _eCasted = true;
                Utility.DelayAction.Add(100, () => _eCasted = false);
            }
        }

        private static void AfterAttack(AttackableUnit target)
        {
            if (!E.LSIsReady())
            {
                return;
            }
            if (((Orbwalk.CurrentMode == _Orbwalker.Mode.Clear && target is Obj_AI_Minion) ||
                 ((Orbwalk.CurrentMode == _Orbwalker.Mode.Combo ||
                   (Orbwalk.CurrentMode == _Orbwalker.Mode.Harass &&
                    player.HealthPercent >= GetValue<Slider>("Harass", "EHpA").Value)) && target is AIHeroClient)) &&
                GetValue<bool>(Orbwalk.CurrentMode.ToString(), "E") && !HavePassive)
            {
                var obj = (Obj_AI_Base) target;
                if (Orbwalk.CurrentMode == _Orbwalker.Mode.Clear || Orbwalk.CurrentMode == _Orbwalker.Mode.Harass ||
                    (Orbwalk.CurrentMode == _Orbwalker.Mode.Combo &&
                     GetValue<StringList>("Combo", "EMode").SelectedIndex == 0))
                {
                    var pos = Geometry.LSCircleCircleIntersection(
                        player.ServerPosition.LSTo2D(), Prediction.GetPrediction(obj, 0.25f).UnitPosition.LSTo2D(), E.Range,
                        Orbwalk.GetAutoAttackRange(obj));
                    if (pos.Count() > 0)
                    {
                        E.Cast(pos.MinOrDefault(i => i.LSDistance(Game.CursorPos)), PacketCast);
                    }
                    else
                    {
                        E.Cast(player.ServerPosition.LSExtend(obj.ServerPosition, -E.Range), PacketCast);
                    }
                }
                else if (Orbwalk.CurrentMode == _Orbwalker.Mode.Combo)
                {
                    switch (GetValue<StringList>("Combo", "EMode").SelectedIndex)
                    {
                        case 1:
                            E.Cast(player.ServerPosition.LSExtend(Game.CursorPos, E.Range), PacketCast);
                            break;
                        case 2:
                            E.Cast(obj.ServerPosition, PacketCast);
                            break;
                    }
                }
            }
        }

        private static void Fight(string mode)
        {
            if (mode == "Combo" && GetValue<bool>(mode, "R") && R.LSIsReady())
            {
                var target = R.GetTarget();
                if (target != null && CanKill(target, GetRDmg(target)))
                {
                    if (player.LSDistance(target) > 550 ||
                        (!Orbwalk.InAutoAttackRange(target) && (!GetValue<bool>(mode, "Q") || !Q.LSIsReady()) &&
                         (!GetValue<bool>(mode, "W") || !W.LSIsReady()) && (!GetValue<bool>(mode, "E") || !E.LSIsReady())))
                    {
                        if (R.Cast(target, PacketCast).LSIsCasted())
                        {
                            if (GetValue<bool>(mode, "RItem") && Youmuu.IsReady())
                            {
                                Utility.DelayAction.Add(10, () => Youmuu.Cast());
                            }
                            return;
                        }
                    }
                }
            }
            if (mode == "Combo" && GetValue<bool>(mode, "E") && GetValue<bool>(mode, "EGap") && E.LSIsReady())
            {
                var target = E.GetTarget(Orbwalk.GetAutoAttackRange() - 30);
                if (target != null && !Orbwalk.InAutoAttackRange(target) &&
                    Orbwalk.InAutoAttackRange(target, 20, player.ServerPosition.LSExtend(Game.CursorPos, E.Range)) &&
                    E.Cast(player.ServerPosition.LSExtend(Game.CursorPos, E.Range), PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>(mode, "PSave") && HavePassive)
            {
                return;
            }
            if (GetValue<bool>(mode, "E") &&
                (E.LSIsReady() || (mode == "Combo" && E.LSIsReady(GetValue<Slider>(mode, "EDelay").Value))))
            {
                return;
            }
            if (GetValue<bool>(mode, "Q") && Q.LSIsReady())
            {
                var target = Q.GetTarget() ?? Q2.GetTarget();
                if (target != null)
                {
                    if (((Orbwalk.InAutoAttackRange(target) && !HavePassive) ||
                         (!Orbwalk.InAutoAttackRange(target, 20) && Q.IsInRange(target))) &&
                        Q.CastOnUnit(target, PacketCast))
                    {
                        return;
                    }
                    if ((mode == "Harass" || GetValue<bool>(mode, "QExtend")) && !Q.IsInRange(target) &&
                        CastExtendQ(target))
                    {
                        return;
                    }
                }
            }
            if ((!GetValue<bool>(mode, "Q") || !Q.LSIsReady()) && GetValue<bool>(mode, "W") && W.LSIsReady() &&
                !player.LSIsDashing())
            {
                var target = W.GetTarget();
                if (target != null &&
                    ((Orbwalk.InAutoAttackRange(target) && !HavePassive) || !Orbwalk.InAutoAttackRange(target, 20)))
                {
                    if (Orbwalk.InAutoAttackRange(target))
                    {
                        W2.CastIfWillHit(target, -1, PacketCast);
                    }
                    else
                    {
                        W.CastIfWillHit(target, -1, PacketCast);
                    }
                }
            }
        }

        private static void Clear()
        {
            var minionObj =
                GetMinions(Q2.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                    .Cast<Obj_AI_Base>()
                    .ToList();
            if (!minionObj.Any())
            {
                return;
            }
            if (GetValue<bool>("Clear", "E") && E.LSIsReady(GetValue<Slider>("Clear", "EDelay").Value))
            {
                return;
            }
            if (GetValue<bool>("Clear", "Q") && Q.LSIsReady() && !HavePassive)
            {
                var obj =
                    minionObj.Where(i => Q.IsInRange(i))
                        .MaxOrDefault(
                            i => Q2.CountHits(minionObj, player.ServerPosition.LSExtend(i.ServerPosition, Q2.Range)));
                if (obj != null && Q.CastOnUnit(obj, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "W") && W.LSIsReady() && !player.LSIsDashing() && !HavePassive)
            {
                var pos = W.GetCircularFarmLocation(minionObj.Where(i => W.IsInRange(i)).ToList());
                if (pos.MinionsHit > 1)
                {
                    W.Cast(pos.Position, PacketCast);
                }
                else
                {
                    var obj =
                        minionObj.Where(i => W.GetPrediction(i).Hitchance >= W.MinHitChance)
                            .MinOrDefault(i => i.LSDistance(player));
                    if (obj != null)
                    {
                        W.Cast(obj, PacketCast);
                    }
                }
            }
        }

        private static void AutoQ()
        {
            if (!GetValue<KeyBind>("Harass", "AutoQ").Active ||
                player.ManaPercent < GetValue<Slider>("Harass", "AutoQMpA").Value || !Q.LSIsReady())
            {
                return;
            }
            var target = Q2.GetTarget();
            if (target == null || Q.IsInRange(target))
            {
                return;
            }
            CastExtendQ(target);
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
            if (player.LSIsDashing() ||
                (!GetValue<bool>("KillSteal", "RStop") && player.IsCastingInterruptableSpell(true)))
            {
                return;
            }
            var cancelR = GetValue<bool>("KillSteal", "RStop") && player.IsCastingInterruptableSpell(true);
            if (GetValue<bool>("KillSteal", "Q") && Q.LSIsReady())
            {
                var target = Q.GetTarget() ?? Q2.GetTarget();
                if (target != null && Q.IsKillable(target))
                {
                    if (Q.IsInRange(target))
                    {
                        if ((!cancelR || R.Cast(PacketCast)) && Q.CastOnUnit(target, PacketCast))
                        {
                            return;
                        }
                    }
                    else if (CastExtendQ(target, cancelR))
                    {
                        return;
                    }
                }
            }
            if (GetValue<bool>("KillSteal", "W") && W.LSIsReady() && !player.LSIsDashing())
            {
                var target = W.GetTarget();
                if (target != null && W.IsKillable(target) && (!cancelR || R.Cast(PacketCast)))
                {
                    W.Cast(target, PacketCast);
                }
            }
        }

        private static void LockROnTarget()
        {
            var target = R.GetTarget();
            if (target == null)
            {
                return;
            }
            var endPos = (player.ServerPosition - target.ServerPosition).LSNormalized();
            var predPos = R.GetPrediction(target).CastPosition.LSTo2D();
            var fullPoint = new Vector2(predPos.X + endPos.X * R.Range * 0.98f, predPos.Y + endPos.Y * R.Range * 0.98f);
            var closestPoint = player.ServerPosition.LSTo2D().LSClosest(new List<Vector2> { predPos, fullPoint });
            if (closestPoint.LSIsValid() && !closestPoint.LSIsWall() && predPos.LSDistance(closestPoint) > E.Range)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, closestPoint.To3D());
            }
            else if (fullPoint.LSIsValid() && !fullPoint.LSIsWall() && predPos.LSDistance(fullPoint) < R.Range &&
                     predPos.LSDistance(fullPoint) > 100)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, fullPoint.To3D());
            }
        }

        private static bool CastExtendQ(AIHeroClient target, bool cancelR = false)
        {
            var objNear = new List<Obj_AI_Base>();
            objNear.AddRange(HeroManager.Enemies.Where(i => i.LSIsValidTarget(Q.Range)));
            objNear.AddRange(GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly));
            var obj =
                objNear.FirstOrDefault(
                    i => Q2.WillHit(target, player.ServerPosition.LSExtend(i.ServerPosition, Q2.Range)));
            return obj != null && (!cancelR || R.Cast(PacketCast)) && Q.CastOnUnit(obj, PacketCast);
        }

        private static double GetRDmg(AIHeroClient target)
        {
            var shot = (int) (7.5 + new[] { 7.5, 9, 10.5 }[R.Level - 1] * 1 / player.AttackDelay);
            var maxShot = new[] { 26, 30, 33 }[R.Level - 1];
            return player.CalcDamage(
                target, Damage.DamageType.Physical,
                (new[] { 40, 50, 60 }[R.Level - 1] + 0.25 * player.FlatPhysicalDamageMod +
                 0.1 * player.FlatMagicDamageMod) * (shot > maxShot ? maxShot : shot));
        }
    }
}