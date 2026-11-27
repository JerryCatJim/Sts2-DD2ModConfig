using Godot;

namespace DD2ModConfig.Code.Nodes;

[GlobalClass]
public partial class DeathSVC : SubViewportContainer
{
	public float delaySeconds = 5.5f; // 几秒后移除遮罩(原来是3.5f，但是死神生成后为了效果微调，隐身了两秒)

	public override void _Ready()
	{
		// 开始计时
		GetTree().CreateTimer(delaySeconds).Timeout += RemoveMask;
	}

	private void RemoveMask()
	{
		// 移除材质，遮罩效果消失
		Material = null;
	}
}
