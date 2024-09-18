
public sealed class DebugCapsuleTrace : Component
{
	float distance = 100.0f;
	float radius = 15.0f;

	protected override void OnUpdate()
	{
		var start = Transform.Position;
		var end = start - (Transform.World.Forward * distance);

		var capsule = new Capsule(start, end, radius);
		var trace = Scene.Trace.Capsule(capsule).IgnoreGameObjectHierarchy(GameObject).WithTag("player_hitbox");
		var result = trace.Run();

		Gizmo.Draw.LineCapsule(capsule);

		if (result.Hit)
		{
			Log.Info($"Spear Trace hit: {result.GameObject}");
		}
	}

	protected override void OnFixedUpdate()
	{
		var start = Transform.Position;
		var end = start - (Transform.World.Forward * distance);

		var capsule = new Capsule(start, end, radius);
		var trace = Scene.Trace.Ray(start, end).Capsule(capsule);//.IgnoreGameObjectHierarchy(GameObject);//.WithTag("player_hitbox");
		var result = trace.Run();

		//Gizmo.Draw.LineCapsule(capsule);

		if (result.Hit)
		{
			Log.Info($"Spear Trace hit: {result.GameObject}");
		}
	}
}
