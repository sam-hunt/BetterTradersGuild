using Verse;

namespace BetterTradersGuild.Integrations
{
    // Single place to ask "is mod X active?" from C#.
    //
    // Do NOT use ModsConfig.IsActive(string) for this: it is an exact match against the
    // ids stored in ModsConfig.xml, and Steam Workshop installs are stored with a
    // "_steam" postfix (vanillaexpanded.gravship2_steam), so it returns false for every
    // Workshop build of the mod and true only for local copies in Mods/. That is why the
    // VGE2 settings notes vanished the moment the dev copies were swapped for the
    // subscribed builds (2026-09-12). The engine's own MayRequire, IfModActive and
    // PatchOperation gates go through ModLister's NoSuffix helpers, which is what this
    // wraps, so C# detection now agrees with the XML gates.
    public static class ModDetection
    {
        public static bool IsActive(string packageId)
        {
            return ModLister.GetActiveModWithIdentifier(packageId, ignorePostfix: true) != null;
        }
    }
}
