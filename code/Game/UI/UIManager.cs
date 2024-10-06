
public partial class UIManager : Component, Component.INetworkListener
{
	[Property] public RankUpWidget rankUpWidget { get; private set; }
	//[Property] public CrosshairBuilder crosshairBuilder { get; private set; }

	protected override void OnStart()
	{
		base.OnStart();

		IUIEvents.Post(x => x.DisableCrosshair());
	}
}