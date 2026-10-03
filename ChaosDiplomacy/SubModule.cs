using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BLTAdoptAHero;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TORBLTChaosDiplomacy
{
    public sealed class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            try
            {
                new Harmony("darktiger512.torbltchaosdiplomacy").PatchAll(typeof(SubModule).Assembly);
                TaleWorlds.Library.Debug.Print("[TORBLTChaosDiplomacy] Viewer Chaos diplomacy enabled.");
            }
            catch (Exception exception)
            {
                TaleWorlds.Library.Debug.Print("[TORBLTChaosDiplomacy] Patch failed; TOR behavior remains active: " + exception);
            }
        }
    }

    [HarmonyPatch]
    internal static class ChaosWarPatch
    {
        private static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("TOR_Core.CampaignMechanics.Chaos.ChaosCampaignBehavior");
            return AccessTools.Method(type, "EnforceWarWithChaos", new[] { typeof(CampaignGameStarter) })
                ?? throw new MissingMethodException("TOR EnforceWarWithChaos API was not found.");
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var declareWar = AccessTools.Method(typeof(FactionManager), "DeclareWar", new[] { typeof(IFaction), typeof(IFaction) });
            var guarded = AccessTools.Method(typeof(ChaosWarPatch), nameof(DeclareWarUnlessViewerChaos));
            var calls = code.Where(instruction => instruction.Calls(declareWar)).ToList();
            // Validate the known TOR release shape before changing any instructions.
            if (calls.Count != 3)
                throw new InvalidOperationException("Expected 3 TOR forced-war calls; found " + calls.Count + ". Patch requires review.");
            foreach (var instruction in calls)
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = guarded;
            }
            return code;
        }

        internal static bool IsViewerChaosFaction(IFaction faction)
        {
            // A viewer vassal does not exempt their NPC ruler's entire kingdom.
            if (faction is Clan clan && clan.Kingdom != null)
                faction = clan.Kingdom;
            if (faction?.Culture?.StringId != "chaos_culture")
                return false;
            var leader = faction is Kingdom kingdom ? kingdom.Leader : (faction as Clan)?.Leader;
            return leader != null && leader.IsAdopted();
        }

        private static void DeclareWarUnlessViewerChaos(IFaction first, IFaction second)
        {
            if (IsViewerChaosFaction(first) || IsViewerChaosFaction(second))
                return;
            FactionManager.DeclareWar(first, second);
        }
    }
}
