
using Sandbox;
using static Sandbox.VertexLayout;

[Group("GMF")]
public class PlayerFootsteps : Component
{
	[Property] SkinnedModelRenderer Source { get; set; }

	protected override void OnEnabled()
	{
		if ( Source is null )
			return;

		Source.OnFootstepEvent += OnEvent;
	}

	protected override void OnDisabled()
	{
		if ( Source is null )
			return;

		Source.OnFootstepEvent -= OnEvent;
	}

	TimeSince timeSinceStep;

	void OnEvent( SceneModel.FootstepEvent e )
	{
		if (IsProxy)
			return;

		if ( timeSinceStep < 0.2f )
			return;

		var tr = Scene.Trace
			.Ray( e.Transform.Position + Vector3.Up * 20, e.Transform.Position + Vector3.Up * -20 )
			.Run();

		if ( !tr.Hit )
			return;

		if ( tr.Surface is null )
			return;

		timeSinceStep = 0;

		var sound = e.FootId == 0 ? tr.Surface.Sounds.FootLeft : tr.Surface.Sounds.FootRight;
		if ( sound is null ) return;

		var position = tr.HitPosition + tr.Normal * 5;
		BroadcastFootstep(sound, position, e.Volume);
	}

	[Broadcast]
	void BroadcastFootstep(string sound, Vector3 position, float volume)
	{
		var handle = Sound.Play(sound, position);
		handle.Volume *= volume;
	}
}
