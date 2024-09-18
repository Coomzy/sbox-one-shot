
public sealed class DebugLocalSpace : Component
{
	[Property] public GameObject destinationGO { get; set; }

	protected override void DrawGizmos()
	{
		if (destinationGO == null)
			return;

		var destination = destinationGO.Transform.Position;
		var localPoint = Transform.World.PointToLocal(destination).WithY(0).WithZ(0);
		var destinationForwardOnly = Transform.Local.PointToWorld(localPoint);

		Gizmo.Transform = Game.ActiveScene.Transform.World;
		Gizmo.Draw.Line(Transform.Position, destination);
		Gizmo.Draw.Line(Transform.Position, destinationForwardOnly);
	}
}
