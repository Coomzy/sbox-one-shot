
using Sandbox;
using System;

[Group("GMF")]
public class Projectile : Component, IRoundEvents, Component.INetworkSpawn
{
	[Group("Setup"), Property] public GameObject impactEffect { get; set; }

	[Group("Runtime"), Property, ReadOnly, Sync] public Equipment owner {  get; set; }

	[Group("Runtime"), Property, ReadOnly, Sync] public bool isInFlight { get; set; } = true;
	[Group("Runtime"), Property, ReadOnly, Sync] public Vector3 startPos { get; set; }

	public virtual void SpawnSource(Vector3 source)
	{
		// This seems dumb, but it works, but it seems dumb
		var end = Transform.Position;
		Transform.Position = source;
		DoMoveStep(end);
	}

	protected override void OnStart()
	{
		if (IsProxy)
			return;

		startPos = Transform.Position;
	}

	protected override void OnUpdate()
	{
		//ExtraDebug.draw.Line(Transform.Position, Transform.Position + (Transform.World.Forward * 100.0f), 10.0f);
		//ExtraDebug.draw.Line(Transform.Position, Transform.Position + (Transform.World.Forward * 100.0f));
		DoMoveStep();
	}

	public virtual void DoMoveStep()
	{
		if (!isInFlight)
			return;

		if (IsProxy)
			return;

		float moveRate = 2500.0f;
		var moveDelta = Transform.World.Forward * Time.Delta * moveRate;
		var nextMovePos = Transform.Position + moveDelta;

		var trace = MoveStepTrace(Transform.Position, nextMovePos);
		var traceResult = trace.Run();

		if (traceResult.Hit)
		{
			nextMovePos = traceResult.HitPosition;
			var impactRange = new Vector2(5.0f, 20.0f);
			var impactDist = impactRange.RandomRange();
			nextMovePos += Transform.World.Forward * impactDist;
			isInFlight = false;
			SpawnImpactEffect(traceResult.HitPosition, -Transform.World.Forward);

			// TODO: Jesus make this better
			if (traceResult.Surface.ResourceName == "wood" || traceResult.Surface.ResourceName == "wood.sheet")
			{
				Sound.Play("harpoon.impact.wood", traceResult.HitPosition);
			}
			else if (traceResult.Surface.ResourceName == "concrete" || traceResult.Surface.ResourceName == "brick")
			{
				Sound.Play("harpoon.impact.wood", traceResult.HitPosition);
			}
			else
			{
				Sound.Play("harpoon.impact.metal", traceResult.HitPosition);
			}
		}
		Debuggin.draw.Line(Transform.Position, nextMovePos);
		DoFlightPlayerHitDetection(Transform.Position, nextMovePos);

		Transform.Position = nextMovePos;
	}

	public virtual void DoMoveStep(Vector3 moveTo)
	{
		if (!isInFlight)
			return;

		if (IsProxy)
			return;

		var trace = MoveStepTrace(Transform.Position, moveTo);
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
		DoFlightPlayerHitDetection(Transform.Position, nextMovePos);

		Transform.Position = nextMovePos;
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

	public virtual SceneTrace MoveStepTrace(Vector3 start, Vector3 end)
	{
		return Scene.Trace
			.Ray(start, end)
			.IgnoreGameObjectHierarchy(GameObject)
			.WithoutTags("trigger", Tag.CHARACTER_BODY);
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
