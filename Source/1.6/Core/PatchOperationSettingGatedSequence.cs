using System.Reflection;
using System.Xml;
using Verse;

namespace BetterTradersGuild
{
    // PatchOperationSequence gated on a bool field of BetterTradersGuildSettings, named by
    // <setting> in the XML. Safe because mod classes (and thus settings) are created before
    // LoadedModManager.ApplyPatches runs (CreateModClasses precedes it, verified RW 1.6).
    // Returns true whenever it applies nothing so PatchOperation.Complete never reports a
    // spurious failure. A missing settings object fails open (applies, the default-ON
    // behaviour); a mistyped field name logs once and skips the file's patches.
    // The private operations list is inherited: RimWorld's XML loader walks the base type
    // chain when filling fields, so the vanilla <operations> element works unchanged.
    public class PatchOperationSettingGatedSequence : PatchOperationSequence
    {
        public string setting;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            BetterTradersGuildSettings settings = BetterTradersGuildMod.Settings;
            if (settings == null)
                return base.ApplyWorker(xml);

            FieldInfo field = string.IsNullOrEmpty(setting)
                ? null
                : typeof(BetterTradersGuildSettings).GetField(setting, BindingFlags.Public | BindingFlags.Instance);
            if (field == null || field.FieldType != typeof(bool))
            {
                Log.Error($"[BTG] PatchOperationSettingGatedSequence: unknown bool setting '{setting}' in {sourceFile}; skipping its patches");
                return true;
            }

            if (!(bool)field.GetValue(settings))
                return true;
            return base.ApplyWorker(xml);
        }
    }
}
