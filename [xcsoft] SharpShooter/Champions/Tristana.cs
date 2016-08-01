using System;
using System.Linq;
using EloBuddy;
using LeagueSharp.Common;
using Color = System.Drawing.Color;
using Utility = LeagueSharp.Common.Utility;


namespace Sharpshooter.Champions
{
    public static class Tristana
    {
        static AIHeroClient Player { get { return ObjectManager.Player; } }
        static Orbwalking.Orbwalker Orbwalker { get { return SharpShooter.Orbwalker; } }

        static Spell Q, W, E, R;
        
        public static void Load()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1170f);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R);
            
            W.SetSkillshot(0.5f, 270f, 1500f, false, SkillshotType.SkillshotCircle);

            var drawDamageMenu = new MenuItem("Draw_RDamage", "Draw (E, R) Damage", true).SetValue(true);
            var drawFill = new MenuItem("Draw_Fill", "Draw (E, R) Damage Fill", true).SetValue(new Circle(true, Color.FromArgb(90, 255, 169, 4)));

            SharpShooter.Menu.SubMenu("Combo").AddItem(new MenuItem("comboUseQ", "Use Q", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Combo").AddItem(new MenuItem("comboUseW", "Use W", true).SetValue(false));
            SharpShooter.Menu.SubMenu("Combo").AddItem(new MenuItem("comboUseE", "Use E", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Combo").AddItem(new MenuItem("comboUseR", "Use R", true).SetValue(true));

            SharpShooter.Menu.SubMenu("Harass").AddItem(new MenuItem("harassUseE", "Use E", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Harass").AddItem(new MenuItem("harassMana", "if Mana % >", true).SetValue(new Slider(50, 0, 100)));

            SharpShooter.Menu.SubMenu("Laneclear").AddItem(new MenuItem("laneclearUseQ", "Use Q", true).SetValue(false));
            //SharpShooter.Menu.SubMenu("Laneclear").AddItem(new MenuItem("laneclearMana", "if Mana % >", true).SetValue(new Slider(50, 0, 100)));

            SharpShooter.Menu.SubMenu("Jungleclear").AddItem(new MenuItem("jungleclearUseQ", "Use Q", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Jungleclear").AddItem(new MenuItem("jungleclearUseE", "Use E", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Jungleclear").AddItem(new MenuItem("jungleclearMana", "if Mana % >", true).SetValue(new Slider(20, 0, 100)));

            SharpShooter.Menu.SubMenu("Misc").AddItem(new MenuItem("antigapcloser", "Use Anti-Gapcloser", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Misc").AddItem(new MenuItem("autointerrupt", "Use Auto-Interrupt", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Misc").AddItem(new MenuItem("killsteal", "Use Killsteal (With R)", true).SetValue(true));

            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingAA", "Real AA Range", true).SetValue(new Circle(true, Color.FromArgb(255, 178, 217))));
            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingW", "W Range", true).SetValue(new Circle(true, Color.FromArgb(255, 178, 217))));

            SharpShooter.Menu.SubMenu("Drawings").AddItem(drawDamageMenu);
            SharpShooter.Menu.SubMenu("Drawings").AddItem(drawFill);

            DamageIndicator.DamageToUnit = GetComboDamage;
            DamageIndicator.Enabled = drawDamageMenu.GetValue<bool>();
            DamageIndicator.Fill = drawFill.GetValue<Circle>().Active;
            DamageIndicator.FillColor = drawFill.GetValue<Circle>().Color;

            drawDamageMenu.ValueChanged +=
            delegate(object sender, OnValueChangeEventArgs eventArgs)
            {
                DamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
            };

            drawFill.ValueChanged +=
            delegate(object sender, OnValueChangeEventArgs eventArgs)
            {
                DamageIndicator.Fill = eventArgs.GetNewValue<Circle>().Active;
                DamageIndicator.FillColor = eventArgs.GetNewValue<Circle>().Color;
            };

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            AIHeroClient.OnProcessSpellCast += AIHeroClient_OnProcessSpellCast;
        }

        static void Game_OnUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                Combo();

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                Harass();

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                Laneclear();
                Jungleclear();
            }

            Killsteal();
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;

            var drawingAA = SharpShooter.Menu.Item("drawingAA", true).GetValue<Circle>();
            var drawingW = SharpShooter.Menu.Item("drawingW", true).GetValue<Circle>();

            if (drawingAA.Active)
                Render.Circle.DrawCircle(Player.Position, Orbwalking.GetRealAutoAttackRange(Player), drawingAA.Color);

            if (W.LSIsReady() && drawingW.Active)
                Render.Circle.DrawCircle(Player.Position, W.Range, drawingW.Color);
        }

        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!SharpShooter.Menu.Item("antigapcloser", true).GetValue<Boolean>() || Player.IsDead)
                return;

            if (gapcloser.Sender.LSIsValidTarget(1000))
            {
                Render.Circle.DrawCircle(gapcloser.Sender.Position, gapcloser.Sender.BoundingRadius, Color.Gold, 5);
                var targetpos = Drawing.WorldToScreen(gapcloser.Sender.Position);
                Drawing.DrawText(targetpos[0] - 40, targetpos[1] + 20, Color.Gold, "Gapcloser");
            }

            if (R.LSIsReady() && gapcloser.Sender.LSIsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                R.Cast(gapcloser.Sender);
        }

        static void Interrupter2_OnInterruptableTarget(AIHeroClient sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (!SharpShooter.Menu.Item("autointerrupt", true).GetValue<Boolean>() || Player.IsDead)
                return;

            if (sender.LSIsValidTarget(1000))
            {
                Render.Circle.DrawCircle(sender.Position, sender.BoundingRadius, Color.Gold, 5);
                var targetpos = Drawing.WorldToScreen(sender.Position);
                Drawing.DrawText(targetpos[0] - 40, targetpos[1] + 20, Color.Gold, "Interrupt");
            }

            if (R.LSIsReady() && sender.LSIsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                R.Cast(sender);
        }
        static void AIHeroClient_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name == "TristanaE")
                Utility.DelayAction.Add(250, Orbwalking.ResetAutoAttackTimer);
        }

        static void Killsteal()
        {
            if (!SharpShooter.Menu.Item("killsteal", true).GetValue<Boolean>())
                return;

            foreach (AIHeroClient target in HeroManager.Enemies.Where(x => x.LSIsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)) && !x.HasBuffOfType(BuffType.Invulnerability) && !x.HasBuffOfType(BuffType.SpellShield)))
            {
                if (target != null)
                {
                    if (R.LSIsReady() && (target.Health + (target.HPRegenRate / 2)) <= E.GetDamage(target))
                    {
                        R.Cast(target);
                        break;
                    }
                }
            }
        }

        static float GetComboDamage(Obj_AI_Base enemy)
        {
            float damage = 0f;

            if (R.LSIsReady())
                damage += R.GetDamage(enemy);

            foreach (var buff in enemy.Buffs)
            {
                if (buff.Name == "tristanaecharge")
                {
                    damage += (float)(E.GetDamage(enemy) * (buff.Count * 0.25)) + E.GetDamage(enemy);
                    break;
                }
            }

            return damage;
        }

        static void Combo()
        {
            if (SharpShooter.Menu.Item("comboUseQ", true).GetValue<Boolean>())
            {
                var Qtarget = Orbwalker.GetTarget();

                if (Q.LSIsReady() && Qtarget != null && Qtarget.IsEnemy)
                    Q.Cast();
            }

            if (!Orbwalking.CanMove(1))
                return;

            if (SharpShooter.Menu.Item("comboUseW", true).GetValue<Boolean>() && W.LSIsReady())
            {
                foreach (AIHeroClient Wtarget in HeroManager.Enemies.Where(x => x.LSIsValidTarget(W.Range) && !Player.HasBuffOfType(BuffType.SpellShield) && !Player.HasBuffOfType(BuffType.Invulnerability)))
                {
                    var Wpred = W.GetPrediction(Wtarget);

                    if(!Wpred.CastPosition.LSUnderTurret(true) && Utility.LSCountEnemiesInRange(Wpred.CastPosition, 1000) <= 2)
                    {
                        foreach (var buff in Wtarget.Buffs)
                        {
                            if (buff.Name == "tristanaecharge" && buff.Count >= 3)
                            {
                                W.Cast(Wtarget);
                                break;
                            }
                        }

                        if (Player.Health + (Player.HPRegenRate / 2) <= W.GetDamage(Wtarget) + Player.LSGetAutoAttackDamage(Wtarget, true))
                            W.Cast(Wtarget);
                    }
                }
            }

            if (SharpShooter.Menu.Item("comboUseE", true).GetValue<Boolean>())
            {
                var Etarget = Orbwalker.GetTarget();

                if (E.LSIsReady() && Etarget != null && Etarget.IsEnemy && !Player.Spellbook.IsAutoAttacking)
                    E.Cast((AIHeroClient)Etarget);
            }

            if (SharpShooter.Menu.Item("comboUseR", true).GetValue<Boolean>() && R.LSIsReady())
            {
                foreach (AIHeroClient Rtarget in HeroManager.Enemies.Where(x => x.LSIsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)) && !Player.HasBuffOfType(BuffType.SpellShield) && !Player.HasBuffOfType(BuffType.Invulnerability)))
                {
                    if (Rtarget.Health + Rtarget.HPRegenRate <= R.GetDamage(Rtarget))
                    {
                        R.Cast(Rtarget);
                        break;
                    }
                }
            }
        }

        static void Harass()
        {
            if (!Orbwalking.CanMove(1) || !(SharpShooter.getManaPer > SharpShooter.Menu.Item("harassMana", true).GetValue<Slider>().Value))
                return;

            if (SharpShooter.Menu.Item("harassUseE", true).GetValue<Boolean>())
            {
                var Etarget = Orbwalker.GetTarget();
                
                
                if (E.LSIsReady() && Etarget != null && Etarget.IsEnemy && !Player.Spellbook.IsAutoAttacking)
                    E.Cast((AIHeroClient)Etarget);
            }
        }

        static void Laneclear()
        {
            if (!Orbwalking.CanMove(1)) //|| !(SharpShooter.getManaPer > SharpShooter.Menu.Item("laneclearMana", true).GetValue<Slider>().Value))
                return;

            var Minions = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Enemy);

            if (Minions.Count <= 0)
                return;

            if (Q.LSIsReady() && SharpShooter.Menu.Item("laneclearUseQ", true).GetValue<Boolean>())
            {
                if (Minions.Count >= 3)
                    Q.Cast();
            }
        }

        static void Jungleclear()
        {
            var Mobs = MinionManager.GetMinions(Player.ServerPosition, Orbwalking.GetRealAutoAttackRange(Player), MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (Mobs.Count <= 0)
                return;

            if (Q.LSIsReady() && SharpShooter.Menu.Item("jungleclearUseQ", true).GetValue<Boolean>())
                Q.Cast();

            if (!Orbwalking.CanMove(1) || !(SharpShooter.getManaPer > SharpShooter.Menu.Item("jungleclearMana", true).GetValue<Slider>().Value))
                return;

            if (E.LSIsReady() && SharpShooter.Menu.Item("jungleclearUseE", true).GetValue<Boolean>() && !Player.Spellbook.IsAutoAttacking)
                E.Cast(Mobs[0]);
        }
    }
}
