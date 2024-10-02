
public class GMFSpawnPoint : Component, IRoundEvents
{
	[Property] public Color Color { get; set; } = "#E3510D";

	[Property, HostSync] public TimeSince? lastUsed { get; set; }
	[Property, HostSync] public PlayerInfo lastUsedBy { get; set; }

	public void RoundCleanup()
	{
		lastUsed = null;
		lastUsedBy = null;
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();
		Model model = Model.Load("models/editor/spawnpoint.vmdl");
		Gizmo.Hitbox.Model(model);
		Gizmo.Draw.Color = Color.WithAlpha((Gizmo.IsHovered || Gizmo.IsSelected) ? 0.7f : 0.5f);
		SceneObject sceneObject = Gizmo.Draw.Model(model);
		if (sceneObject != null)
		{
			sceneObject.Flags.CastShadows = true;
		}
	}
}