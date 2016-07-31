using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using LeagueSharp.Common;
using Color = System.Drawing.Color;
using Utility = LeagueSharp.Common.Utility;
using Spell = LeagueSharp.Common.Spell;
using TargetSelector = LeagueSharp.Common.TargetSelector;
using SharpDX;

namespace Blitzcrank
{
    internal class Bliz
    {
        internal static Menu Root;
        internal static Random Rand;
        internal static Spell Q, W, E, R;
        internal static Orbwalking.Orbwalker Orbwalker;
        internal static AIHeroClient Player = ObjectManager.Player;
        public Bliz()
        {
            
            Setmenu();
        }

        private static void Setmenu()
        {
            
        }
    }
}
