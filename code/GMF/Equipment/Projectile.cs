
using Sandbox;
using System;

[Group("GMF")]
public class Projectile : Component, IGameModeEvents, Component.INetworkSpawn
{
	[Group("Setup"), Property] public GameObject impactEffect { get; set; }

	[Group("Runtime"), Property, ReadOnly, Sync] public Equipment owner {  get; set; }

	[Group("Runtime"), Property, ReadOnly, Sync] public bool isInFlight { get; set; } = true;
	[Group("Runtime"), Property, ReadOnly, Sync] public Vector3 startPos { get; set; }

	public virtual void SpawnSource(Vector3 source)
	{
		startPos = WorldPosition;

		// This seems dumb, but it works, but it seems dumb
		var end = WorldPosition;
		WorldPosition = source;
		DoMoveStep(end);
	}

	protected override void OnStart()
	{
		if (IsProxy)
			return;

		startPos = WorldPosition;
	}

	protected override void OnUpdate()
	{
		DoMoveStep();
	}

	public virtual void DoMoveStep(Vector3? moveToOverride = null)
	{
		if (!isInFlight)
			return;

		if (IsProxy)
			return;

		float moveRate = 2500.0f;
		var moveDelta = Transform.World.Forward * Time.Delta * moveRate;
		var moveTo = moveToOverride.HasValue ? moveToOverride.Value : WorldPosition + moveDelta;

		var trace = MoveStepTrace(WorldPosition, moveTo);
		var traceResult = trace.Run();
		var nextMovePos = moveTo;

		if (traceResult.Hit)
		{
			if (!CanPenetrate(traceResult))
			{
				isInFlight = false;
			}

			GetImpactPosition(ref nextMovePos, traceResult);
			SpawnImpactEffect(traceResult.HitPosition, -Transform.World.Forward);
			PlayImpactSound(traceResult);
		}
		DoFlightPlayerHitDetection(WorldPosition, nextMovePos);

		WorldPosition = nextMovePos;
	}

	public virtual SceneTrace MoveStepTrace(Vector3 start, Vector3 end)
	{
		return Scene.Trace
			.Ray(start, end)
			.IgnoreGameObjectHierarchy(GameObject)
			.WithoutTags(Tag.TRIGGER, Tag.CHARACTER_BODY);
	}

	// This is not for player penetration, it's for walls and shit
	protected virtual bool CanPenetrate(SceneTraceResult traceResult)
	{
		return false;
	}

	protected virtual void GetImpactPosition(ref Vector3 nextMovePos, SceneTraceResult traceResult)
	{
		nextMovePos = traceResult.HitPosition;
	}

	protected virtual void PlayImpactSound(SceneTraceResult traceResult)
	{
		traceResult.Surface.PlayCollisionSound(traceResult.HitPosition);
	}

	protected virtual void DoFlightPlayerHitDetection(Vector3 start, Vector3 end)
	{
	}

	[Broadcast]
	protected virtual void SpawnImpactEffect(Vector3 hitPoint, Vector3 hitNormal)
	{ 
		if (!IsFullyValid(impactEffect))
		{
			//Log.Warning($"Missing Impact Effects on '{GameObject}'");
			//return;
		}
		var inst = impactEffect.Clone(hitPoint, hitNormal.EulerAngles.ToRotation());
		//impactEffect
	}

	public void OnNetworkSpawn(Connection connection)
	{

	}

	public void RoundCleanup()
	{
		if (IsProxy)
			return;

		GameObject.Destroy();
	}
}
