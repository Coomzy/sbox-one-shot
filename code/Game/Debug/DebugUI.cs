
using Sandbox;
using Sandbox.Network;
using Sandbox.Utility;
using System.Numerics;

public class DebugUI : Component
{
	[Property] public RoundCountWidget roundCountWidget { get; set; }
	[Property] public float visibleTime { get; set; } = 3.0f;
	[Property] public float fadeOutTime { get; set; } = 1.0f;

	[Button]
	public void Test()
	{
		UIManager.instance.roundCountWidget.ShowAndFade();
		//roundCountWidget.ShowAndFade(visibleTime, fadeOutTime);
		//roundCountWidget.ShowAndFade();
	}
}