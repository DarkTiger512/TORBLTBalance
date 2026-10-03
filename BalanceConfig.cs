using System;
using System.IO;
using System.Xml.Serialization;
using TaleWorlds.Library;

namespace TORBLTBalance
{
    [Serializable]
    public sealed class BalanceConfig
    {
        public bool Enabled { get; set; } = true;
        public float DefensiveProcSharedCooldownSeconds { get; set; } = 5f;
        public int DefensiveProcMaxTargets { get; set; } = 4;
        public float DefensiveProcMaxStatusDurationSeconds { get; set; } = 8f;
        public bool VerboseLogging { get; set; } = false;

        public static BalanceConfig Current { get; private set; } = new BalanceConfig();

        public static void Load()
        {
            try
            {
                var path = Path.Combine(
                    BasePath.Name,
                    "Modules",
                    "TORBLTBalance",
                    "ModuleData",
                    "TORBLTBalance.xml");

                if (!File.Exists(path))
                {
                    Current = new BalanceConfig();
                    Log("Config not found; using defaults.");
                    return;
                }

                var serializer = new XmlSerializer(typeof(BalanceConfig), new XmlRootAttribute("TORBLTBalanceConfig"));
                using (var stream = File.OpenRead(path))
                {
                    Current = serializer.Deserialize(stream) as BalanceConfig ?? new BalanceConfig();
                }

                Current.DefensiveProcSharedCooldownSeconds = Math.Max(0f, Current.DefensiveProcSharedCooldownSeconds);
                Current.DefensiveProcMaxTargets = Math.Max(0, Current.DefensiveProcMaxTargets);
                Current.DefensiveProcMaxStatusDurationSeconds = Math.Max(0f, Current.DefensiveProcMaxStatusDurationSeconds);

                Log("Loaded configuration.");
            }
            catch (Exception ex)
            {
                Current = new BalanceConfig();
                Log("Failed to read config; using defaults. " + ex.Message);
            }
        }

        public static void Log(string message)
        {
            TaleWorlds.Library.Debug.Print("[TORBLTBalance] " + message);
        }

        public static void Verbose(string message)
        {
            if (Current.VerboseLogging)
                Log(message);
        }
    }
}
