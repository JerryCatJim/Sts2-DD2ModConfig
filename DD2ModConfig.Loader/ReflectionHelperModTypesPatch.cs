using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;

namespace DD2ModConfig.Loader
{
    /// <summary>
    /// Harmony patch that ensures dynamically loaded variant assembly types
    /// are always included when ReflectionHelper.ModTypes is queried.
    /// This is the same strategy used by RitsuLib.
    /// </summary>
    [HarmonyPatch(typeof(ReflectionHelper), nameof(ReflectionHelper.ModTypes), MethodType.Getter)]
    internal static class ReflectionHelperModTypesPatch
    {
        private static void Postfix(ref Type[] __result)
        {
            var variantTypes = Bootstrap.GetVariantModTypes();
            if (variantTypes.Length == 0)
                return;

            __result = __result.Concat(variantTypes).Distinct().ToArray();
        }
    }
}
