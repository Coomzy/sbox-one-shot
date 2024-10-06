
using Sandbox;
using Sandbox.Audio;
using Sandbox.Physics;
using Sandbox.Services;
using System;
using System.Diagnostics;
using static Sandbox.Connection;
using static Sandbox.Material;

[Group("OS")]
public class HarpoonSpear : Projectile
{
	[Group("Setup"), Property] public HarpoonSpearFlare flare { get; set; }
	[Group("Runtime"), Property] public List<GameObject> impaledCharacters { get; set; } = new();	

	protected override void GetImpactPosition(ref Vector3 nextMovePos, SceneTraceResult traceResult)
	{
		base.GetImpactPosition(ref nextMovePos, traceResult);

		// Go into the object a random amount
		var impactRange = new Vector2(5.0f, 20.0f);
		var impactDist = impactRange.RandomRange();
		nextMovePos += Transform.World.Forward * impactDist;

		Disable();
	}

	[Broadcast]
	public void Disable()
	{
		flare.Enabled = false;
	}

	public override SceneTrace MoveStepTrace(Vector3 start, Vector3 end)
	{
		return Scene.Trace
			.Ray(start, end)
			.IgnoreGameObjectHierarchy(GameObject)
			.WithoutTags(Tag.TRIGGER, Tag.CHARACTER_BODY, Tag.CHARACTER_BODY_REMOTE);//, Tag.PLAYER_CLIP, Tag.SKY);
	}

	protected override void PlayImpactSound(SceneTraceResult traceResult)
	{
		if (traceResult.Surface.ResourceName == "wood" || traceResult.Surface.ResourceName == "wood.sheet")
		{
			Sound.Play("harpoon.impact.wood", traceResult.HitPosition);
		}
		else if (traceResult.Surface.ResourceName == "concrete" || traceResult.Surface.ResourceName == "brick")
		{
			Sound.Play("harpoon.impact.concrete", traceResult.HitPosition);
		}
		else
		{
			Sound.Play("harpoon.impact.metal", traceResult.HitPosition);
		}
	}

	public override SceneTrace PlayerHitTrace(Vector3 start, Vector3 end)
	{
		var trace = base.PlayerHitTrace(start, end);
		trace = trace.WithoutTags(Tag.IMPALED);
		foreach (var impalee in impaledCharacters)
		{
			trace = trace.IgnoreGameObjectHierarchy(impalee);
		}
		return trace;
	}

	protected override void OnPlayerHit(CharacterBody characterBody, SceneTraceResult result)
	{
		if (impaledCharacters.Contains(characterBody.GameObject))
		{
			return;
		}

		//Log.Info($"collision.Other.Body: {collision.Other.Body}");
		DamageInfo damageInfo = new DamageInfo();
		damageInfo.instigator = instigator?.instigator?.owner;
		damageInfo.damageCauser = this;
		damageInfo.hitBodyIndex = GetClosestSafeIndex(characterBody.bodyPhysics, result.Body.GroupIndex);
		damageInfo.hitVelocity = Transform.World.Forward * 100.0f;
		characterBody.TakeDamage(damageInfo);

		characterBody.GameObject.Tags.Add(Tag.IMPALED);
		var distance = Vector3.DistanceBetween(startPos, WorldPosition) * MathY.inchToMeter;
		Sound.Play("harpoon.impact.flesh", result.HitPosition);

		//Log.Info($"Spear Hit WorldPosition: {WorldPosition}");
		//Debuggin.draw.Sphere(start, 10.0f, 8, 15.0f);
		//Debuggin.draw.Line(WorldPosition, WorldPosition + damageInfo.hitVelocity, 15.0f);	

		if (instigator is HarpoonGun harpoonGun)
		{
			harpoonGun.Reload();
			PlayerInfo.local.OnScoreKill();

			var killer = PlayerInfo.local?.displayName;
			var victim = $"{characterBody.owner?.owner?.displayName} {distance.ToString("F2")}m";
			var message = "----->";
			IUIEvents.Post(x => x.AddKillFeedEntry(killer, victim, message));
		}

		Stats.SetValue(Stat.FURTHEST_KILL, distance);

		// It's not great to hardcode this, however I didn't unlock it with the stat :/
		if (distance >= 35.0f)
		{
			Achievements.Unlock(Achievement.DEADEYE);
		}

		if (!impaledCharacters.Contains(characterBody.GameObject))
		{
			impaledCharacters.Add(characterBody.GameObject);
		}
		else
		{
			Log.Warning($"impaledCharacters already had: {characterBody.GameObject}");
		}

		if (impaledCharacters.Count > 2)
		{
			Achievements.Unlock(Achievement.DOUBLE_PENETRATION);
		}

		Stat.ProcessMultiKill(impaledCharacters.Count);
		IUIEvents.Post(x => x.OnDamagedEnemy());
	}

	// Any other hit bones turn terry into stretch armstrong
	public int GetClosestSafeIndex(ModelPhysics bodyPhysics, int index)
	{
		if (index == Bones.Terry.spine_0 || index == Bones.Terry.spine_2 || index == Bones.Terry.head)
		{
			return index;
		}

		var hitBodyPosition = bodyPhysics.PhysicsGroup.Bodies.ElementAt(index).Position;
		var spine_0Position = bodyPhysics.PhysicsGroup.Bodies.ElementAt(Bones.Terry.spine_0).Position;
		var spine_2Position = bodyPhysics.PhysicsGroup.Bodies.ElementAt(Bones.Terry.spine_2).Position;
		var headPosition = bodyPhysics.PhysicsGroup.Bodies.ElementAt(Bones.Terry.head).Position;

		var spine_0_Dist = Vector3.DistanceBetween(hitBodyPosition, spine_0Position);
		var spine_2_Dist = Vector3.DistanceBetween(hitBodyPosition, spine_2Position);
		var head_Dist = Vector3.DistanceBetween(hitBodyPosition, headPosition);

		if (spine_0_Dist <= spine_2_Dist && spine_0_Dist <= head_Dist)
		{
			return Bones.Terry.spine_0;
		}

		if (spine_2_Dist <= spine_0_Dist && spine_2_Dist <= head_Dist)
		{
			return Bones.Terry.spine_2;
		}

		return Bones.Terry.head;
	}
}
