public class DebugTest : Component, Component.INetworkSnapshot
{	
	public static bool isEnabled { get; set; }

	protected override void OnAwake()
	{
		isEnabled = Application.IsEditor;
	}

	void INetworkSnapshot.WriteSnapshot(ref ByteStream writer)
	{
		Log.Info($"INetworkSnapshot.WriteSnapshot() isEnabled: {isEnabled}, Application.IsEditor: {Application.IsEditor}");
		if (!Application.IsEditor)
		{
			writer.Write(false);
			return;
		}

		writer.Write(isEnabled);
	}

	void INetworkSnapshot.ReadSnapshot(ref ByteStream reader)
	{
		isEnabled = reader.Read<bool>();
		Log.Info($"INetworkSnapshot.ReadSnapshot() isEnabled: {isEnabled}, Application.IsEditor: {Application.IsEditor}");
	}

	[Button]
	public void Test()
	{
		Debuggin.draw.Sphere(Transform.Position + (Transform.World.Up * 250.0f), 100.0f, 8, 15.0f);
		//Sandbox.Diagnostics.Assert.IsNull(SteamAudioSource);

		//Sandbox.Services.Stats.Increment("kills", 1);
		//Debuggin.draw.Sphere(Transform.Position + (Transform.World.Up * 250.0f), 100.0f, 8, 15.0f);
	}

	protected override void OnUpdate()
	{

	}

	[Button]
	public void Test_Alt()
	{
		var result = Sandbox.Services.Stats.LocalPlayer.GetValue("kills", StatAggregation.Sum);
		Log.Info($"result: {result}");
		//Debuggin.draw.Sphere(Transform.Position + (Transform.World.Up * 250.0f), 100.0f, 8, 15.0f);
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		//Debuggin.ToScreen($"DrawGizmos()");
		//Debuggin.draw.Sphere(Transform.Position + (Transform.World.Up * 250.0f), 10.0f);
		//Gizmo.Draw.LineSphere(Vector3.Zero, 100.0f, 8);
	}
}