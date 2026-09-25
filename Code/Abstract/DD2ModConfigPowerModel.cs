using BaseLib.Abstracts;
using BaseLib.Extensions;
using DD2ModConfig.Code.Extensions;
using Godot;

namespace DD2ModConfig.Code.Abstract;

public abstract class DD2ModConfigPowerModel : CustomPowerModel
{
    public override string CustomBigIconPath
    {
        get
        {
            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();
            return ResourceLoader.Exists(path) ? path : "default_power.png".PowerImagePath();
        }
    }

    public override string CustomPackedIconPath
    {
        get
        {
            /*var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();
			return ResourceLoader.Exists(path) ? path : "default_power.png".PowerImagePath();*/
            return CustomBigIconPath;
        }
    }
}
