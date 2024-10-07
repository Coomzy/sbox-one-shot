using Sandbox.Services;

public class DebugTest : Component
{	
	public static bool isEnabled { get; set; }

	protected override void OnAwake()
	{
		//Log.Info($"GameSettings.instance.payToWinGamePass.Has(): {GameSettings.instance.payToWinGamePass.Has()}");
	}

	protected override void OnStart()
	{
		//Log.Info($"GameSettings.instance.payToWinGamePass.Has(): {GameSettings.instance.payToWinGamePass.Has()}");
	}

	[Button]
	public void Test()
	{
		Debuggin.draw.Sphere(WorldPosition + (Transform.World.Up * 250.0f), 100.0f, 8, 15.0f);
		//Sandbox.Diagnostics.Assert.IsNull(SteamAudioSource);

		//Sandbox.Services.Stats.Increment("kills", 1);
		//Debuggin.draw.Sphere(WorldPosition + (Transform.World.Up * 250.0f), 100.0f, 8, 15.0f);
	}

	protected override void OnUpdate()
	{

	}

	[Button]
	public void Test_Alt()
	{
		var result = Sandbox.Services.Stats.LocalPlayer.GetValue("kills", StatAggregation.Sum);
		Log.Info($"result: {result}");
		//Debuggin.draw.Sphere(WorldPosition + (Transform.World.Up * 250.0f), 100.0f, 8, 15.0f);
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		//Debuggin.ToScreen($"DrawGizmos()");
		//Debuggin.draw.Sphere(WorldPosition + (Transform.World.Up * 250.0f), 10.0f);
		//Gizmo.Draw.LineSphere(Vector3.Zero, 100.0f, 8);
	}
}