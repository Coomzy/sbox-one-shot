
using Sandbox;
using Sandbox.Physics;
using Sandbox.Services;
using System;
using System.Diagnostics;

[Group("GMF")]
public class HarpoonSpear : Projectile
{
	[Property] List<GameObject> impaledCharacters { get; set; } = new();	

	protected override void GetImpactPosition(ref Vector3 nextMovePos, SceneTraceResult traceResult)
	{
		base.GetImpactPosition(ref nextMovePos, traceResult);

		// Go into the object a random amount
		var impactRange = new Vector2(5.0f, 20.0f);
		var impactDist = impactRange.RandomRange();
		nextMovePos += Transform.World.Forward * impactDist;
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

	// TODO: Move this into the base!
	protected override void DoFlightPlayerHitDetection(Vector3 start, Vector3 end)
	{
		//ExtraDebug.draw.Line(start, end, 15.0f);
		var radius = 15.0f;

		//var capsule = new Capsule(start, end, radius);
		var capsule = Capsule.FromHeightAndRadius(5.0f, radius);
		var trace = Scene.Trace.Capsule(capsule, start, end).IgnoreGameObjectHierarchy(GameObject).WithTag(Tag.CHARACTER_BODY).WithoutTags(Tag.RAGDOLL, Tag.IMPALED);
		foreach (var impalee in impaledCharacters)
		{
			trace = trace.IgnoreGameObjectHierarchy(impalee);
		}
		var results = trace.RunAll();

		//ExtraDebug.draw.Capsule(capsule);
		//ExtraDebug.draw.Capsule(capsule);
		//ExtraDebug.draw.Sphere(start, 100.0f, 8, 15.0f);
		//Gizmo.Draw.LineSphere(start, 100.0f, 8);
		string multikillMedal = null;

		foreach (var result in results)
		{
			if (!result.Hit)
			{
				continue;
			}
			//Debuggin.ToScreen($"impaledCharacters: {impaledCharacters.Count}", 15.0f);

			var characterBody = result.GameObject.Components.Get<CharacterBody>();
			if (characterBody == null)
			{
				Log.Warning($"Spear hit '{result.GameObject}' but it didn't have a character body?");
				continue;
			}

			if (impaledCharacters.Contains(characterBody.GameObject))
			{
				continue;
			}

			characterBody.GameObject.Tags.Add(Tag.IMPALED);
			Sound.Play("harpoon.impact.flesh", result.HitPosition);
			var distance = Vector3.DistanceBetween(startPos, WorldPosition) * MathY.inchToMeter;

			//Log.Info($"collision.Other.Body: {collision.Other.Body}");
			var osCharacter = (OSCharacter)characterBody.owner;
			DamageInfo damageInfo = new DamageInfo();
			damageInfo.instigator = owner?.instigator?.owner;
			damageInfo.damageCauser = this;
			damageInfo.hitBodyIndex = GetClosestSafeIndex(characterBody.bodyPhysics, result.Body.GroupIndex);
			damageInfo.hitVelocity = Transform.World.Forward * 100.0f;
			characterBody.TakeDamage(damageInfo);

			//Log.Info($"Spear Hit WorldPosition: {WorldPosition}");
			//Debuggin.draw.Sphere(start, 10.0f, 8, 15.0f);
			//Debuggin.draw.Line(WorldPosition, WorldPosition + damageInfo.hitVelocity, 15.0f);	

			if (owner is HarpoonGun harpoonGun)
			{
				harpoonGun.Reload();
				PlayerInfo.local.OnScoreKill();
				UIManager.instance.killFeedUpWidget.AddEntry(PlayerInfo.local.displayName, $"{osCharacter?.owner?.displayName} {distance.ToString("F2")}m", "----->");
			}

			Stats.SetValue(Stat.FURTHEST_KILL, distance);

			// It's not great to hardcode this, however I didn't unlock it with the stat :/
			if (distance >= 35.0f)
			{
				Debuggin.ToScreen($"Unlock Deadeye!");
				Log.Info($"Unlock Deadeye!");
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

			// TODO: This is a lot of string allocations...
			multikillMedal = Stat.KillCountToMedal(impaledCharacters.Count);
			if (!string.IsNullOrEmpty(multikillMedal))
			{
				Stats.Increment(multikillMedal, 1);

				// TODO: Medal UI?
			}
		}

		multikillMedal = Stat.KillCountToMedal(impaledCharacters.Count);
		if (!string.IsNullOrEmpty(multikillMedal))
		{
			AnnouncerSystem.QueueSound(multikillMedal);
		}
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
