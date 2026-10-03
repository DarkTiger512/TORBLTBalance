using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TOR_Core.Items;

namespace TORBLTBalance
{
    internal sealed class ProcContext
    {
        public ItemTrait Trait { get; set; }
        public Agent ViewerAgent { get; set; }
        public bool Defensive { get; set; }
    }

    internal static class BalanceState
    {
        [System.ThreadStatic]
        private static ProcContext _current;

        private static Mission _mission;
        private static readonly Dictionary<int, float> DefensiveCooldowns = new Dictionary<int, float>();

        public static ProcContext Current => _current;

        public static void Enter(ProcContext context)
        {
            ResetIfMissionChanged();
            _current = context;
        }

        public static void Exit(ProcContext context)
        {
            if (ReferenceEquals(_current, context))
                _current = null;
        }

        public static bool IsDefensiveCooldownActive(Agent agent)
        {
            ResetIfMissionChanged();
            if (agent == null || Mission.Current == null)
                return false;

            return DefensiveCooldowns.TryGetValue(agent.Index, out var until)
                   && until > Mission.Current.CurrentTime;
        }

        public static void StartDefensiveCooldown(Agent agent)
        {
            ResetIfMissionChanged();
            if (agent == null || Mission.Current == null)
                return;

            var seconds = BalanceConfig.Current.DefensiveProcSharedCooldownSeconds;
            if (seconds <= 0f)
                return;

            DefensiveCooldowns[agent.Index] = Mission.Current.CurrentTime + seconds;
        }

        private static void ResetIfMissionChanged()
        {
            if (_mission == Mission.Current)
                return;

            _mission = Mission.Current;
            DefensiveCooldowns.Clear();
            _current = null;
        }
    }
}
