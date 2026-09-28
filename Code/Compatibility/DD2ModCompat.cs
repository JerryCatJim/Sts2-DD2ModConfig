using MegaCrit.Sts2.Core.Modding;
using System.Reflection;

namespace DD2ModConfig.Code.Compatibility;

public static class DD2ModCompat  //兼容的是官方的Mod.cs
{
    // 探测一次，缓存结果
    private static readonly FieldInfo? SingleAssemblyField =
        typeof(Mod).GetField("assembly", BindingFlags.Public | BindingFlags.Instance);

    private static readonly FieldInfo? AssemblyListField =
        typeof(Mod).GetField("assemblies", BindingFlags.Public | BindingFlags.Instance);

    public static IReadOnlyList<Assembly> GetAssemblies(Mod mod)
    {
        // 0.111+：List<Assembly>
        if (AssemblyListField != null)
        {
            var list = AssemblyListField.GetValue(mod) as List<Assembly>;
            return list ?? (IReadOnlyList<Assembly>)Array.Empty<Assembly>();
        }

        // 0.107：Assembly?  //为什么107版本返回的一定是空？
        if (SingleAssemblyField != null)
        {
            var asm = SingleAssemblyField.GetValue(mod) as Assembly;
            return asm != null ? new[] { asm } : Array.Empty<Assembly>();
        }
        return Array.Empty<Assembly>();
    }
}
