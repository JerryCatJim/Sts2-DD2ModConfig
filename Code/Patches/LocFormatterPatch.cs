using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using SmartFormat;
using DD2ModConfig.Code.Formatters;

namespace DD2ModConfig.Code.Patches;

[HarmonyPatch(typeof(LocManager), "LoadLocFormatters")]
public static class LocFormatterPatch
{
    public static void Postfix(LocManager __instance)
    {
        SmartFormatter TempSF = Traverse.Create(typeof(LocManager)).Field("_smartFormatter").GetValue<SmartFormatter>();
        TempSF.AddExtensions(new StressIconsFormatter(), new ComboIconsFormatter());
        Smart.Default = TempSF;
    }
}