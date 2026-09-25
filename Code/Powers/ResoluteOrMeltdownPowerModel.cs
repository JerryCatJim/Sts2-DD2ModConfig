using DD2ModConfig.Code.Abstract;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace DD2ModConfig.Code.Powers;
public class ResoluteOrMeltdownPowerModel : DD2ModConfigPowerModel
{
    //这个类和其派生类都只是为了把美德和折磨效果具象化为一个仅供展示的挂在人物下方的BUFF图标
    //美德和折磨的具体实现应在ResoluteOrMeltdown文件夹下实现，因为我想把美德和折磨设计为类似观者姿态的那种与人物BUFF和DEBUFF区分开的一种独特效果
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;
}