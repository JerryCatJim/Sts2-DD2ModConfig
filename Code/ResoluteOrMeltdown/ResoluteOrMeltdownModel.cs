using BaseLib.Extensions;
using DD2ModConfig.Code.Extensions;
using DD2ModConfig.Code.ResoluteOrMeltdown.Vfx;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace DD2ModConfig.Code.ResoluteOrMeltdown;

public enum ResoluteOrMeltdownType
{
    None,
    Resolute,
    Meltdown,
    Toxic //You can add more types.
}

public abstract class ResoluteOrMeltdownModel : AbstractModel
{
    private Player? _player;
    public Player Owner => _player ?? throw new InvalidOperationException("Not a mutable instance");

    public abstract ResoluteOrMeltdownType RMType { get; }
    //用来展示在压力power下的你的人物想展示的独特爆压效果，例如Jerry的苦修爆压后进入怨毒，获得一层怨毒形态，那么DisplayPowers里就可以带一个怨毒形态power
    public abstract IEnumerable<PowerModel>? DisplayPowers { get; }

    public ResoluteOrMeltdownModel ToMutable(Player player)
    {
        var mutable = (ResoluteOrMeltdownModel)MutableClone();
        mutable._player = player;
        return mutable;
    }

    private LocString Title => new("powers", $"{MainFile.ModId.ToUpperInvariant()}-{Id.Entry}.title");
    private LocString Description => new("powers", $"{MainFile.ModId.ToUpperInvariant()}-{Id.Entry}.description");
    private string PackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();
    private Texture2D Icon => ResourceLoader.Load<Texture2D>(PackedIconPath);
    public HoverTip DumbHoverTip
    {
        get
        {
            var description = Description;
            AddDumbVariablesToDescription(description);
            return new HoverTip(Title, description.GetFormattedText(), Icon);
        }
    }

    private void AddDumbVariablesToDescription(LocString description)
    {
        description.Add("singleStarIcon", "[img]res://images/packed/sprite_fonts/star_icon.png[/img]");
        var pool = IsMutable ? Owner.Character.CardPool : ModelDb.CardPool<ColorlessCardPool>();
        description.Add("energyPrefix", EnergyIconHelper.GetPrefix(pool));
    }

    protected virtual VfxConfig RMVfxConfig => new(
        EnterSfxPath: GetEnterSfxPath(),
        ScreenShakeStrength: ShakeStrength.Strong
    );

    private VfxController? _vfx;

    //public IEnumerable<string> AssetPaths => VfxConfig.AssetPaths;

    public virtual async Task OnEnterResoluteOrMeltdown(PlayerChoiceContext ctx, Player owner, CardModel? source)
    {
        _vfx = new VfxController(RMVfxConfig);
        await _vfx.OnEnter(owner.Creature);

    }

    public virtual async Task OnExitResoluteOrMeltdown(PlayerChoiceContext ctx, Player owner, CardModel? source)
    {
        if (_vfx != null)
            await _vfx.OnExit(owner.Creature);
        _vfx = null;
    }

    private string GetEnterSfxPath()
    {
        string Path = "";
        switch (RMType)
        {
            case ResoluteOrMeltdownType.Resolute:
                Path = "Resolute";
                break;
            case ResoluteOrMeltdownType.Meltdown:
                Path = "Meltdown";
                break;
            case ResoluteOrMeltdownType.Toxic:
                Path = "Toxic";
                break;
            default:
                break;
        }
        return Path;
    }
}