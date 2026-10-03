using System;
using System.Collections.Generic;
using System.Reflection;
using BLTAdoptAHero;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.Items;
using TOR_Core.Items.WeaponHitScripts;

namespace TORBLTBalance.Patches
{
    internal static class ViewerDetection
    {
        public static bool IsAdoptedViewer(Agent agent)
        {
            if (agent?.Character is not CharacterObject character)
                return false;

            var hero = character.HeroObject;
            return hero != null && hero.IsAdopted();
        }

        public static bool IsDefensiveTriggeredEffect(ItemTrait trait)
        {
            var scriptName = trait?.OnWeaponHitScript?.WeaponScriptName;
            if (string.IsNullOrWhiteSpace(scriptName))
                return false;

            var type = AccessTools.TypeByName(scriptName);
            return type != null && typeof(DefenseTriggerEffectScript).IsAssignableFrom(type);
        }
    }

    [HarmonyPatch]
    internal static class ApplySpecialTraitPatch
    {
        private static MethodBase TargetMethod()
            => AccessTools.Method(typeof(WeaponHitScriptsMissionLogic), "ApplySpecialTrait");

        private static bool Prefix(
            ItemTrait trait,
            Agent affectorAgent,
            Agent affectedAgent,
            bool cooldownOnTarget,
            out ProcContext __state)
        {
            __state = null;

            if (!BalanceConfig.Current.Enabled ||
                !cooldownOnTarget ||
                !ViewerDetection.IsAdoptedViewer(affectedAgent) ||
                !ViewerDetection.IsDefensiveTriggeredEffect(trait))
            {
                return true;
            }

            if (BalanceState.IsDefensiveCooldownActive(affectedAgent))
            {
                BalanceConfig.Verbose("Suppressed defensive proc " + trait.ItemTraitStringId +
                                      " for " + affectedAgent.Name +
                                      " because the shared cooldown is active.");
                return false;
            }

            __state = new ProcContext
            {
                Trait = trait,
                ViewerAgent = affectedAgent,
                Defensive = true
            };
            BalanceState.Enter(__state);
            return true;
        }

        private static void Postfix(ProcContext __state)
        {
            if (__state != null)
                BalanceState.Exit(__state);
        }

        private static Exception Finalizer(Exception __exception, ProcContext __state)
        {
            if (__state != null)
                BalanceState.Exit(__state);
            return __exception;
        }
    }

    [HarmonyPatch]
    internal static class RegisterTraitCooldownPatch
    {
        private static MethodBase TargetMethod()
            => AccessTools.Method(typeof(WeaponHitScriptsMissionLogic), "RegisterTraitCooldown");

        private static void Postfix(ItemTrait trait, Agent targetAgent)
        {
            var context = BalanceState.Current;
            if (context == null || !context.Defensive || context.ViewerAgent == null)
                return;

            BalanceState.StartDefensiveCooldown(context.ViewerAgent);
            BalanceConfig.Verbose("Started shared defensive enchantment cooldown after " +
                                  (trait?.ItemTraitStringId ?? "unknown") + ".");
        }
    }

    [HarmonyPatch(typeof(TriggeredEffect), "Trigger")]
    internal static class TriggeredEffectTargetCapPatch
    {
        private static readonly AccessTools.FieldRef<TriggeredEffect, TriggeredEffectTemplate> TemplateRef =
            AccessTools.FieldRefAccess<TriggeredEffect, TriggeredEffectTemplate>("_template");

        private static void Prefix(
            TriggeredEffect __instance,
            Vec3 position,
            Agent triggererAgent,
            ref MBList<Agent> targets)
        {
            var context = BalanceState.Current;
            var maxTargets = BalanceConfig.Current.DefensiveProcMaxTargets;

            if (context == null || !context.Defensive || maxTargets <= 0 ||
                triggererAgent != context.ViewerAgent || Mission.Current == null || targets != null)
            {
                return;
            }

            var template = TemplateRef(__instance);
            if (template == null || template.TargetType != TargetType.Enemy)
                return;

            var nearby = new MBList<Agent>();
            nearby = Mission.Current.GetNearbyEnemyAgents(
                position.AsVec2,
                template.Radius,
                triggererAgent.Team,
                nearby);

            if (nearby == null || nearby.Count <= maxTargets)
                return;

            var capped = new MBList<Agent>();
            var seen = new HashSet<int>();
            for (var i = 0; i < nearby.Count && capped.Count < maxTargets; i++)
            {
                var candidate = nearby[i];
                if (candidate == null || !candidate.IsActive() || candidate.Health <= 0f || candidate.IsFadingOut())
                    continue;
                if (!seen.Add(candidate.Index))
                    continue;
                capped.Add(candidate);
            }

            targets = capped;
            BalanceConfig.Verbose("Capped " + template.StringID + " from " + nearby.Count +
                                  " targets to " + capped.Count + ".");
        }
    }

    [HarmonyPatch]
    internal static class DefensiveStatusDurationPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(AbilityManagerMissionLogic),
                "QueueTriggeredStatusEffect",
                new[]
                {
                    typeof(Agent),
                    typeof(TOR_Core.BattleMechanics.StatusEffect.StatusEffectTemplate),
                    typeof(Agent),
                    typeof(float),
                    typeof(bool),
                    typeof(bool),
                    typeof(int)
                });
        }

        private static void Prefix(ref float duration, Agent applierAgent)
        {
            var context = BalanceState.Current;
            var maxDuration = BalanceConfig.Current.DefensiveProcMaxStatusDurationSeconds;

            if (context == null || !context.Defensive || maxDuration <= 0f ||
                applierAgent != context.ViewerAgent || duration <= maxDuration)
            {
                return;
            }

            BalanceConfig.Verbose("Capped defensive status duration from " + duration +
                                  "s to " + maxDuration + "s for " +
                                  (context.Trait?.ItemTraitStringId ?? "unknown") + ".");
            duration = maxDuration;
        }
    }
}
