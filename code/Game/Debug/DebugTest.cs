
using Sandbox;
using System.Numerics;

public class DebugTest : Component
{
	[Button]
	public void Test()
	{
		Debuggin.draw.Sphere(Transform.Position + (Transform.World.Up * 250.0f), 100.0f, 8, 15.0f);
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		//Debuggin.ToScreen($"DrawGizmos()");
		//Debuggin.draw.Sphere(Transform.Position + (Transform.World.Up * 250.0f), 10.0f);
		//Gizmo.Draw.LineSphere(Vector3.Zero, 100.0f, 8);
	}
}