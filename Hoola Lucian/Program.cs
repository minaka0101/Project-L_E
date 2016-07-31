using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms.VisualStyles;
using LeagueSharp.Common;
using Color = System.Drawing.Color;
using ItemData = LeagueSharp.Common.Data.ItemData;
using SharpDX;
using EloBuddy;
using EloBuddy.SDK.Events;
using Utility = LeagueSharp.Common.Utility;


namespace HoolaLucian
{
    public class Program
    {
        public static Menu _Menu;
        public static LCOrbwak _Orbwalker;
        public static AIHeroClient player = ObjectManager.Player;
        private static HpBarIndicator Indicator = new HpBarIndicator();
        public static Spell Q, Q1, W, E, R;
        public static bool AAPassive;

        //public static bool HEXQ = _Menu.Item("HEXQ").GetValue<bool>();
       // public static bool KillstealQ = _Menu.Item("KillstealQ").GetValue<bool>();
        //public static bool CQ = _Menu.Item("CQ").GetValue<bool>();
       // public static bool CW = _Menu.Item("CW").GetValue<bool>();
        //public static int CE = _Menu.Item("CE").GetValue<StringList>().SelectedIndex;
       // public static bool HQ = _Menu.Item("HQ").GetValue<bool>();
       // public static bool HW = _Menu.Item("HW").GetValue<bool>();
        //public static int HE = _Menu.Item("HE").GetValue<StringList>().SelectedIndex;
        //public static int HMinMana = _Menu.Item("HMinMana").GetValue<Slider>().Value;
       // public static bool JQ = _Menu.Item("JQ").GetValue<bool>();
        //public static bool JW = _Menu.Item("JW").GetValue<bool>();
        //public static bool JE = _Menu.Item("JE").GetValue<bool>();
       // public static bool LHQ = _Menu.Item("LHQ").GetValue<bool>();
       // public static int LQ = _Menu.Item("LQ").GetValue<Slider>().Value;
       // public static bool LW = _Menu.Item("LW").GetValue<bool>();
       // public static bool LE = _Menu.Item("LE").GetValue<bool>();
       // public static int LMinMana = _Menu.Item("LMinMana").GetValue<Slider>().Value;
       // public static bool Dind = _Menu.Item("Dind").GetValue<bool>();
       // public static bool DEQ = _Menu.Item("DEQ").GetValue<bool>();
        //public static bool DQ = _Menu.Item("DQ").GetValue<bool>();
       // public static bool DW = _Menu.Item("DW").GetValue<bool>();
       // public static bool DE = _Menu.Item("DE").GetValue<bool>();
        //static bool AutoQ = _Menu.Item("AutoQ").GetValue<KeyBind>().Active;
       // public static int MinMana = _Menu.Item("MinMana").GetValue<Slider>().Value;
       // public static int HHMinMana = _Menu.Item("HHMinMana").GetValue<Slider>().Value;
        //public static int Humanizer = _Menu.Item("Humanizer").GetValue<Slider>().Value;
        //static bool ForceR = _Menu.Item("ForceR").GetValue<KeyBind>().Active;
        //static bool LT = _Menu.Item("LT").GetValue<KeyBind>().Active;
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnGameLoad;
        }

        static void OnGameLoad(EventArgs args)
        {
            player = ObjectManager.Player;
            if (player.ChampionName != "Lucian") return;
            Chat.Print("Hoola Lucian - Loaded Successfully, Good Luck! :)");
            Q = new Spell(SpellSlot.Q, 675);
            Q1 = new Spell(SpellSlot.Q, 1200);
            W = new Spell(SpellSlot.W, 1200, TargetSelector.DamageType.Magical);
            E = new Spell(SpellSlot.E, 475f);
            R = new Spell(SpellSlot.R, 1400);

            OnMenuLoad();

            Q.SetTargetted(0.25f, 1400f);
            Q1.SetSkillshot(0.5f, 50, float.MaxValue, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.30f, 80f, 1600f, true, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.2f, 110f, 2500, true, SkillshotType.SkillshotLine);


            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnEndScene += Drawing_OnEndScene;
            Obj_AI_Base.OnSpellCast += OnDoCast;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnSpellCast += OnDoCastLC;
        }

        private static void Interrupter2_OnInterruptableTarget(AIHeroClient sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            
        }
        public static void OnMenuLoad()
        {
            _Menu = new Menu("Hoola Lucian", "hoolalucian", true);

            _Menu.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            _Orbwalker = new LCOrbwak(_Menu.SubMenu("Orbwalking"));

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            _Menu.AddSubMenu(targetSelectorMenu);

            var Combo = new Menu("Combo", "Combo");
            Combo.AddItem(new MenuItem("CQ", "Use Q").SetValue(true));
            Combo.AddItem(new MenuItem("CW", "Use W").SetValue(true));
            Combo.AddItem(new MenuItem("CE", "Use E Mode").SetValue(new StringList(new[] { "Side", "Cursor", "Enemy", "Never" })));
            Combo.AddItem(new MenuItem("ForceR", "Force R On Target Selector").SetValue(new KeyBind('T', KeyBindType.Press)));
            _Menu.AddSubMenu(Combo);

            var Misc = new Menu("Misc", "Misc");
            Misc.AddItem(new MenuItem("Humanizer", "Humanizer Delay").SetValue(new Slider(5,5,300)));
            Misc.AddItem(new MenuItem("Nocolision", "Nocolision W").SetValue(true));
            _Menu.AddSubMenu(Misc);


            var Harass = new Menu("Harass", "Harass");
            Harass.AddItem(new MenuItem("HEXQ", "Use Extended Q").SetValue(true));
            Harass.AddItem(new MenuItem("HMinMana", "Extended Q Min Mana (%)").SetValue(new Slider(80)));
            Harass.AddItem(new MenuItem("HQ", "Use Q").SetValue(true));
            Harass.AddItem(new MenuItem("HW", "Use W").SetValue(true));
            Harass.AddItem(new MenuItem("HE", "Use E Mode").SetValue(new StringList(new [] {"Side","Cursor","Enemy","Never"})));
            Harass.AddItem(new MenuItem("HHMinMana", "Harass Min Mana (%)").SetValue(new Slider(80)));
            _Menu.AddSubMenu(Harass);

            var LC = new Menu("LaneClear", "LaneClear");
            LC.AddItem(new MenuItem("LT", "Use Spell LaneClear (Toggle)").SetValue(new KeyBind('J', KeyBindType.Toggle)));
            LC.AddItem(new MenuItem("LHQ", "Use Extended Q For Harass").SetValue(true));
            LC.AddItem(new MenuItem("LQ", "Use Q (0 = Don't)").SetValue(new Slider(0,0,5)));
            LC.AddItem(new MenuItem("LW", "Use W").SetValue(true));
            LC.AddItem(new MenuItem("LE", "Use E").SetValue(true));
            LC.AddItem(new MenuItem("LMinMana", "Min Mana (%)").SetValue(new Slider(80)));
            _Menu.AddSubMenu(LC);

            var JC = new Menu("JungleClear", "JungleClear");
            JC.AddItem(new MenuItem("JQ", "Use Q").SetValue(true));
            JC.AddItem(new MenuItem("JW", "Use W").SetValue(true));
            JC.AddItem(new MenuItem("JE", "Use E").SetValue(true));
            _Menu.AddSubMenu(JC);

            var Auto = new Menu("Auto", "Auto");
            Auto.AddItem(new MenuItem("AutoQ", "Auto Extended Q (Toggle)").SetValue(new KeyBind('G', KeyBindType.Toggle)));
            Auto.AddItem(new MenuItem("MinMana", "Min Mana (%)").SetValue(new Slider(80)));
            _Menu.AddSubMenu(Auto);

            var Draw = new Menu("Draw", "Draw");
            Draw.AddItem(new MenuItem("Dind", "Draw Damage Incidator").SetValue(true));
            Draw.AddItem(new MenuItem("DEQ", "Draw Extended Q").SetValue(false));
            Draw.AddItem(new MenuItem("DQ", "Draw Q").SetValue(false));
            Draw.AddItem(new MenuItem("DW", "Draw W").SetValue(false));
            Draw.AddItem(new MenuItem("DE", "Draw E").SetValue(false));
            _Menu.AddSubMenu(Draw);

            var killsteal = new Menu("killsteal", "Killsteal");
            killsteal.AddItem(new MenuItem("KillstealQ", "Killsteal Q").SetValue(true));
            _Menu.AddSubMenu(killsteal);

            _Menu.AddToMainMenu();
        }

        public static void OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            var spellName = args.SData.Name;
            int Humanizer = _Menu.Item("Humanizer").GetValue<Slider>().Value;
            if (!sender.IsMe || !Orbwalking.IsAutoAttack(spellName)) return;

            if (args.Target is AIHeroClient)
            {
                var target = (Obj_AI_Base)args.Target;
                if (_Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && target.IsValid)
                {
                    Utility.DelayAction.Add(Humanizer, () => OnDoCastDelayed(args));
                }
                if (_Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed && target.IsValid)
                {
                    Utility.DelayAction.Add(Humanizer, () => OnDoCastDelayed(args));
                }
            }
            if (args.Target is Obj_AI_Minion)
            {
                if (_Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && args.Target.IsValid)
                {
                    Utility.DelayAction.Add(Humanizer, () => OnDoCastDelayed(args));
                }
            }
        }
        public static void OnDoCastLC(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            var spellName = args.SData.Name;
            int Humanizer = _Menu.Item("Humanizer").GetValue<Slider>().Value;
            if (!sender.IsMe || !Orbwalking.IsAutoAttack(spellName)) return;

            if (args.Target is Obj_AI_Minion)
            {
                if (_Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && args.Target.IsValid)
                {
                    Utility.DelayAction.Add(Humanizer, () => OnDoCastDelayedLC(args));
                }
            }
        }

        static void killsteal()
        {
            bool KillstealQ = _Menu.Item("KillstealQ").GetValue<bool>();
            if (KillstealQ && Q.LSIsReady())
            {
                var targets = HeroManager.Enemies.Where(x => x.LSIsValidTarget(Q.Range) && !x.IsZombie);
                foreach (var target in targets)
                {
                    if (target.Health < Q.GetDamage(target) && (!target.HasBuff("kindrednodeathbuff") && !target.HasBuff("Undying Rage") && !target.HasBuff("JudicatorIntervention")))
                        Q.Cast(target);
                }
            }
        }
        public static void OnDoCastDelayedLC(GameObjectProcessSpellCastEventArgs args)
        {
            AAPassive = false;
            int LQ = _Menu.Item("LQ").GetValue<Slider>().Value;
            bool LW = _Menu.Item("LW").GetValue<bool>();
            bool LE = _Menu.Item("LE").GetValue<bool>();
            int LMinMana = _Menu.Item("LMinMana").GetValue<Slider>().Value;
            bool LT = _Menu.Item("LT").GetValue<KeyBind>().Active;
            if (args.Target is Obj_AI_Minion && args.Target.IsValid)
            {
                if (_Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && player.ManaPercent > LMinMana)
                {
                    var Minions = MinionManager.GetMinions(Orbwalking.GetRealAutoAttackRange(player), MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health);
                    if (Minions[0].IsValid && Minions.Count != 0)
                    {
                        if (!LT) return;

                        if (E.LSIsReady() && !AAPassive && LE) E.Cast(player.Position.LSExtend(Game.CursorPos, 70));
                        if (Q.LSIsReady() && (!E.LSIsReady() || (E.LSIsReady() && !LE)) && LQ != 0 && !AAPassive)
                        {
                            var QMinions = MinionManager.GetMinions(Q.Range);
                            var exminions = MinionManager.GetMinions(Q1.Range);
                            foreach (var Minion in QMinions)
                            {
                                var QHit = new Geometry.Polygon.Rectangle(player.Position,player.Position.LSExtend(Minion.Position, Q1.Range),Q1.Width);
                                if (exminions.Count(x => !QHit.IsOutside(x.Position.LSTo2D())) >= LQ)
                                {
                                    Q.Cast(Minion);
                                    break;
                                }
                            }
                        }
                        if ((!E.LSIsReady() || (E.LSIsReady() && !LE)) && (!Q.LSIsReady() || (Q.LSIsReady() && LQ == 0)) && LW && W.LSIsReady() && !AAPassive) W.Cast(Minions[0].Position);
                    }
                }
            }
        }
        public static Vector2 Deviation(Vector2 point1, Vector2 point2, double angle)
        {
            angle *= Math.PI / 180.0;
            Vector2 temp = Vector2.Subtract(point2, point1);
            Vector2 result = new Vector2(0);
            result.X = (float)(temp.X * Math.Cos(angle) - temp.Y * Math.Sin(angle)) / 4;
            result.Y = (float)(temp.X * Math.Sin(angle) + temp.Y * Math.Cos(angle)) / 4;
            result = Vector2.Add(result, point1);
            return result;
        }
        public static void OnDoCastDelayed(GameObjectProcessSpellCastEventArgs args)
        {
            int CE = _Menu.Item("CE").GetValue<StringList>().SelectedIndex;
            bool CW = _Menu.Item("CW").GetValue<bool>();
            bool CQ = _Menu.Item("CQ").GetValue<bool>();
            bool HQ = _Menu.Item("HQ").GetValue<bool>();
            bool HW = _Menu.Item("HW").GetValue<bool>();
            int HE = _Menu.Item("HE").GetValue<StringList>().SelectedIndex;
            bool JQ = _Menu.Item("JQ").GetValue<bool>();
            bool JW = _Menu.Item("JW").GetValue<bool>();
            bool JE = _Menu.Item("JE").GetValue<bool>();
            AAPassive = false;
            if (args.Target is AIHeroClient)
            {
                var target = (Obj_AI_Base)args.Target;
                if (_Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && target.IsValid)
                {
                    if (ItemData.Youmuus_Ghostblade.GetItem().IsReady()) ItemData.Youmuus_Ghostblade.GetItem().Cast();
                    if (E.LSIsReady() && !AAPassive && CE == 0) E.Cast((Deviation(player.Position.LSTo2D(), target.Position.LSTo2D(), 65).To3D()));
                    if (E.LSIsReady() && !AAPassive && CE == 1) E.Cast(Game.CursorPos);
                    if (E.LSIsReady() && !AAPassive && CE == 2) E.Cast(player.Position.LSExtend(target.Position, 50));
                    if (Q.LSIsReady() && (!E.LSIsReady() || (E.LSIsReady() && CE == 3)) && CQ && !AAPassive) Q.Cast(target);
                    if ((!E.LSIsReady() || (E.LSIsReady() && CE == 3)) && (!Q.LSIsReady() || (Q.LSIsReady() && !CQ)) && CW && W.LSIsReady() && !AAPassive) W.Cast(target.Position);
                }
                if (_Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed && target.IsValid)
                {
                    int HHMinMana = _Menu.Item("HHMinMana").GetValue<Slider>().Value;
                    if (player.ManaPercent < HHMinMana) return;

                    if (E.LSIsReady() && !AAPassive && HE == 0) E.Cast((Deviation(player.Position.LSTo2D(), target.Position.LSTo2D(),65).To3D()));
                    if (E.LSIsReady() && !AAPassive && HE == 1) E.Cast(player.Position.LSExtend(Game.CursorPos, 50));
                    if (E.LSIsReady() && !AAPassive && HE == 2) E.Cast(player.Position.LSExtend(target.Position, 50));
                    if (Q.LSIsReady() && (!E.LSIsReady() || (E.LSIsReady() && HE == 3)) && HQ && !AAPassive) Q.Cast(target);
                    if ((!E.LSIsReady() || (E.LSIsReady() && HE == 3)) && (!Q.LSIsReady() || (Q.LSIsReady() && !HQ)) && HW && W.LSIsReady() && !AAPassive) W.Cast(target.Position);
                }
            }
            if (args.Target is Obj_AI_Minion && args.Target.IsValid)
            {
                if (_Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
                {
                    var Mobs = MinionManager.GetMinions(Orbwalking.GetRealAutoAttackRange(player), MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                    if (Mobs[0].IsValid && Mobs.Count != 0)
                    {
                        if (E.LSIsReady() && !AAPassive && JE) E.Cast(player.Position.LSExtend(Game.CursorPos, 70));
                        if (Q.LSIsReady() && (!E.LSIsReady() || (E.LSIsReady() && !JE)) && JQ && !AAPassive) Q.Cast(Mobs[0]);
                        if ((!E.LSIsReady() || (E.LSIsReady() && !JE)) && (!Q.LSIsReady() || (Q.LSIsReady() && !JQ)) && JW && W.LSIsReady() && !AAPassive) W.Cast(Mobs[0].Position);
                    }
                }
            }
        }

        public static void Harass()
        {
            int HMinMana = _Menu.Item("HMinMana").GetValue<Slider>().Value;
            bool HEXQ = _Menu.Item("HEXQ").GetValue<bool>();
            if (player.ManaPercent < HMinMana) return;

            if (Q.LSIsReady() && HEXQ)
            {
                var target = TargetSelector.GetTarget(Q1.Range, TargetSelector.DamageType.Physical);
                var Minions = MinionManager.GetMinions(Q.Range);
                foreach (var Minion in Minions)
                {
                    var QHit = new Geometry.Polygon.Rectangle(player.Position, player.Position.LSExtend(Minion.Position, Q1.Range),Q1.Width);
                    var QPred = Q1.GetPrediction(target);
                    if (!QHit.IsOutside(QPred.UnitPosition.LSTo2D()) && QPred.Hitchance == HitChance.High)
                    {
                        Q.Cast(Minion);
                        break;
                    }
                }
            }
        }
        static void LaneClear()
        {
            bool LHQ = _Menu.Item("LHQ").GetValue<bool>();
            int LMinMana = _Menu.Item("LMinMana").GetValue<Slider>().Value;
            if (player.ManaPercent < LMinMana) return;

            if (Q.LSIsReady() && LHQ)
            {
                var extarget = TargetSelector.GetTarget(Q1.Range, TargetSelector.DamageType.Physical);
                var Minions = MinionManager.GetMinions(Q.Range);
                foreach (var Minion in Minions)
                {
                    var QHit = new Geometry.Polygon.Rectangle(player.Position, player.Position.LSExtend(Minion.Position, Q1.Range), Q1.Width);
                    var QPred = Q1.GetPrediction(extarget);
                    if (!QHit.IsOutside(QPred.UnitPosition.LSTo2D()) && QPred.Hitchance == HitChance.High)
                    {
                        Q.Cast(Minion);
                        break;
                    }
                }
            }
        }
        static void AutoUseQ()
        {
            bool AutoQ = _Menu.Item("AutoQ").GetValue<KeyBind>().Active;
            int MinMana = _Menu.Item("MinMana").GetValue<Slider>().Value;
            if (Q.LSIsReady() && AutoQ && player.ManaPercent > MinMana)
            {
                var extarget = TargetSelector.GetTarget(Q1.Range, TargetSelector.DamageType.Physical);
                var Minions = MinionManager.GetMinions(Q.Range);
                foreach (var Minion in Minions)
                {
                    var QHit = new Geometry.Polygon.Rectangle(player.Position, player.Position.LSExtend(Minion.Position, Q1.Range), Q1.Width);
                    var QPred = Q1.GetPrediction(extarget);
                    if (!QHit.IsOutside(QPred.UnitPosition.LSTo2D()) && QPred.Hitchance == HitChance.High)
                    {
                        Q.Cast(Minion);
                        break;
                    }
                }
            }
        }

        static void UseRTarget()
        {
            bool ForceR = _Menu.Item("ForceR").GetValue<KeyBind>().Active;
            var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
            if (ForceR && R.LSIsReady() && target.IsValid && target is AIHeroClient && !player.HasBuff("LucianR")) R.Cast(target.Position);
        }
        static void Game_OnUpdate(EventArgs args)
        {
            bool ForceR = _Menu.Item("ForceR").GetValue<KeyBind>().Active;
            W.Collision = _Menu.Item("Nocolision").GetValue<bool>();
            AutoUseQ();

            if (ForceR) UseRTarget();
            killsteal();
            if (_Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed) Harass();
            if (_Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear) LaneClear();
        }
        static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot == SpellSlot.Q || args.Slot == SpellSlot.W || args.Slot == SpellSlot.E) AAPassive = true;
            if (args.Slot == SpellSlot.E) Orbwalking.ResetAutoAttackTimer();
            if (args.Slot == SpellSlot.R) ItemData.Youmuus_Ghostblade.GetItem().Cast();
        }

        static float getComboDamage(Obj_AI_Base enemy)
        {
            if (enemy != null)
            {
                float damage = 0;
                if (E.LSIsReady()) damage = damage + (float)player.LSGetAutoAttackDamage(enemy) * 2;
                if (W.LSIsReady()) damage = damage + W.GetDamage(enemy) + (float)player.LSGetAutoAttackDamage(enemy);
                if (Q.LSIsReady())
                {
                    damage = damage + Q.GetDamage(enemy) + (float)player.LSGetAutoAttackDamage(enemy);
                }
                damage = damage + (float)player.LSGetAutoAttackDamage(enemy);

                return damage;
            }
            return 0;
        }

        static void OnDraw(EventArgs args)
        {
            bool DEQ = _Menu.Item("DEQ").GetValue<bool>();
            bool DQ = _Menu.Item("DQ").GetValue<bool>();
            bool DW = _Menu.Item("DW").GetValue<bool>();
            bool DE = _Menu.Item("DE").GetValue<bool>();
            if (DEQ) Render.Circle.DrawCircle(player.Position, Q1.Range, Q.LSIsReady() ? Color.LimeGreen : Color.IndianRed);
            if (DQ) Render.Circle.DrawCircle(player.Position, Q.Range, Q.LSIsReady() ? Color.LimeGreen : Color.IndianRed);
            if (DW) Render.Circle.DrawCircle(player.Position, W.Range, W.LSIsReady() ? Color.LimeGreen : Color.IndianRed);
            if (DE) Render.Circle.DrawCircle(player.Position, E.Range, E.LSIsReady() ? Color.LimeGreen : Color.IndianRed);
        }
        static void Drawing_OnEndScene(EventArgs args)
        {
            bool Dind = _Menu.Item("Dind").GetValue<bool>();
            if (Dind)
            {
                foreach (
                    var enemy in
                        ObjectManager.Get<AIHeroClient>()
                            .Where(ene => ene.LSIsValidTarget() && !ene.IsZombie))
                {
                    Indicator.unit = enemy;
                    Indicator.drawDmg(getComboDamage(enemy), new ColorBGRA(255, 204, 0, 160));

                }
            }
        }
    }
}