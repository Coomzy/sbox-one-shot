
using Sandbox;
using System;

[Group("GMF")]
public class Projectile : Component, IGameModeEvents, Component.INetworkSpawn
{
	[Group("Setup"), Property] public GameObject impactEffect { get; set; }

	[Group("Config"), Property] public bool usesGravity { get; set; } = false;
	[Group("Config"), Property] public float coyoteTime { get; set; } = 0.35f;
	[Group("Config"), Property] public float gravityRate { get; set; } = 350.0f;

	[Group("Runtime"), Property, ReadOnly, Sync] public PlayerInfo owner {  get; set; }
	[Group("Runtime"), Property, ReadOnly, Sync] public Equipment instigator {  get; set; }

	[Group("Runtime"), Property, ReadOnly, Sync] public bool isInFlight { get; set; } = true;
	[Group("Runtime"), Property, ReadOnly, Sync] public Vector3 velocity { get; set; }
	[Group("Runtime"), Property, ReadOnly, Sync] public TimeSince startFlightTime { get; set; }
	[Group("Runtime"), Property, ReadOnly, Sync] public Vector3 startPos { get; set; }

	[ConVar] public static bool spear_uses_gravity { get; set; } = true;
	[ConVar] public static float spear_coyote_time { get; set; } = 0.15f;
	[ConVar] public static float spear_gravity_rate { get; set; } = 35.0f;

	public virtual void SpawnSource(Vector3 source)
	{
		startPos = WorldPosition;
		startFlightTime = 0;

		// This seems dumb, but it works, but it seems dumb
		var end = WorldPosition;
		WorldPosition = source;
		DoMoveStep(end, false);
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

	public virtual void DoMoveStep(Vector3? moveToOverride = null, bool allowGravity = true)
	{
		if (!isInFlight)
			return;

		if (IsProxy)
			return;

		if (allowGravity && usesGravity && startFlightTime >= coyoteTime)
		{
			velocity += Vector3.Down * Time.Delta * gravityRate;
		}

		var moveDelta = velocity * Time.Delta;
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
			.WithoutTags(Tag.TRIGGER, Tag.CHARACTER_BODY, Tag.IGNORE, Tag.PLAYER_CLIP, Tag.SKY);
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
		var trace = PlayerHitTrace(start, end);
		var results = trace.RunAll();

		//ExtraDebug.draw.Capsule(capsule);
		//ExtraDebug.draw.Capsule(capsule);
		//ExtraDebug.draw.Sphere(start, 100.0f, 8, 15.0f);
		//Gizmo.Draw.LineSphere(start, 100.0f, 8);

		foreach (var result in results)
		{
			if (!result.Hit)
			{
				continue;
			}

			var characterBody = result.GameObject.Components.Get<CharacterBody>();
			if (characterBody == null)
			{
				Log.Warning($"Projectile hit '{result.GameObject}' but it didn't have a character body?");
				continue;
			}
			OnPlayerHit(characterBody, result);
		}
	}

	protected virtual void OnPlayerHit(CharacterBody characterBody, SceneTraceResult traceResult)
	{
		DamageInfo damageInfo = new DamageInfo();
		damageInfo.instigator = instigator?.instigator?.owner;
		damageInfo.damageCauser = this;
		damageInfo.hitBodyIndex = traceResult.Body.GroupIndex;
		damageInfo.hitVelocity = Transform.World.Forward * 100.0f;
		characterBody.TakeDamage(damageInfo);
	}

	public virtual SceneTrace PlayerHitTrace(Vector3 start, Vector3 end)
	{
		var height = 5.0f;
		var radius = 15.0f;

		var capsule = Capsule.FromHeightAndRadius(height, radius);
		var trace = Scene.Trace.Capsule(capsule, start, end)
								.IgnoreGameObjectHierarchy(GameObject)
								.WithTag(Tag.CHARACTER_BODY_REMOTE)
								.WithoutTags(Tag.RAGDOLL);
		return trace;
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
