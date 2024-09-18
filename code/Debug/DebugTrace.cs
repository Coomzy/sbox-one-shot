
public class DebugTrace : Component
{
	protected override void OnUpdate()
	{
		float distance = 100.0f;
		float radius = 15.0f;
		float height = 15.0f;

		var start = Transform.Position;
		var end = start + (Transform.World.Forward * distance);

		var traceRay = Scene.Trace.Ray(start, end);
		var resultRay = traceRay.Run();

		//var capsule = new Capsule(start, end, radius);
		//var traceCapsule = Scene.Trace.Capsule(capsule);
		//var capsule = new Capsule(Vector3.Zero, Vector3.Up * height, radius);
		var capsule = Capsule.FromHeightAndRadius(height, radius);
		var traceCapsule = Scene.Trace.Capsule(capsule, start, end);
		var resultCapsule = traceCapsule.Run();

		Gizmo.Draw.Color = Color.Yellow;
		//Gizmo.Draw.LineSphere(start, 2.5f);

		Gizmo.Draw.Color = resultRay.Hit ? Color.Green : Color.Red;
		Gizmo.Draw.Line(start, end);

		Gizmo.Draw.Color = resultCapsule.Hit ? Color.Green : Color.Red;
		//Gizmo.Draw.LineCapsule(capsule);
		Gizmo.Draw.LineCapsule(new Capsule(start, end, radius));
	}
}
