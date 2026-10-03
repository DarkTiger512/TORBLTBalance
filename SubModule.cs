using System;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace TORBLTBalance
{
    public sealed class TORBLTBalanceSubModule : MBSubModuleBase
    {
        private Harmony _harmony;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            try
            {
                BalanceConfig.Load();
                if (!BalanceConfig.Current.Enabled)
                {
                    BalanceConfig.Log("Disabled by configuration.");
                    return;
                }

                _harmony = new Harmony("darktiger512.torbltbalance");
                _harmony.PatchAll(typeof(TORBLTBalanceSubModule).Assembly);
                BalanceConfig.Log("v0.1.0 loaded.");
            }
            catch (Exception ex)
            {
                BalanceConfig.Log("Startup failed: " + ex);
            }
        }

        protected override void OnSubModuleUnloaded()
        {
            try
            {
                _harmony?.UnpatchAll(_harmony.Id);
            }
            finally
            {
                _harmony = null;
                base.OnSubModuleUnloaded();
            }
        }
    }
}
