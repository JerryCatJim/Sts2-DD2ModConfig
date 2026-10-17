using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using SmartFormat;
using DD2ModConfig.Code.Formatters;
using MegaCrit.Sts2.Core.Logging;

namespace DD2ModConfig.Code.Patches;

[HarmonyPatch(typeof(LocManager), "LoadLocFormatters")]
public static class LocFormatterPatch
{
    public static void Postfix(SmartFormatter ____smartFormatter)
    {
        try
        {
            var formatter = ____smartFormatter ?? Smart.Default;
            if (formatter == null)
            {
                Log.Error("[DD2ModConfig LocFormatterPatch] LocManager.LoadLocFormatters completed but no SmartFormatter instance was available.");
                return;
            }
            formatter.AddExtensions(new StressIconsFormatter(), new ComboIconsFormatter());
        }
        catch (Exception ex)
        {
            Log.Error($"[DD2ModConfig LocFormatterPatch] Failed to inject registered localization extensions: {ex.Message}");
        }
    }
}