using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using DD2ModConfig.Code.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Modding;

namespace DD2ModConfig.Code.Abstract;

[Pool(typeof(DarkestDungeonRelicPool))]

public abstract class DarkestDungeonRelicModel : CustomRelicModel
{
    //可以获取到引用该前置库的子MOD的ModId
    private string? _currentModId;
    protected string CurrentModId => _currentModId ??= ResolveModId();
    private string ResolveModId()
    {
        //GetType()直接就是返回的当前子类实例的类型，无需将CurrentModId写为virtual来override
        var asm = GetType().Assembly;
        var mod = ModManager.Mods.FirstOrDefault(m => m.assemblies.Contains(asm));
        return mod?.manifest?.id ?? "Unknown";
    }

    protected override string BigIconPath
    {
        get
        {
            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath(CurrentModId);
            return ResourceLoader.Exists(path) ? path : "default_relic.png".RelicImagePath(CurrentModId);
        }
    }
    public override string PackedIconPath
    {
        get
        {
            /*var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath(CurrentModId);
            return ResourceLoader.Exists(path) ? path : "default_relic.png".RelicImagePath(CurrentModId);*/
            return BigIconPath;
        }
    }
    protected override string PackedIconOutlinePath
    {
        get
        {
            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicImagePath(CurrentModId);
            return ResourceLoader.Exists(path) ? path : "default_relic_outline.png".RelicImagePath(CurrentModId);
        }
    }
}