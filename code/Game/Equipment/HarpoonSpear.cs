
using Sandbox;
using Sandbox.Physics;
using System;
using System.Diagnostics;

[Group("GMF")]
public class HarpoonSpear : Projectile
{	
	List<GameObject> impaledCharacters { get; set; } = new List<GameObject>();

	protected override void OnUpdate()
	{
		base.OnUpdate();

		//ExtraDebug.draw.Line(Transform.Position, Transform.Position + (Transform.World.Forward * 100.0f));
	}

	// TODO: Move this into the base!
	protected override void DoFlightPlayerHitDetection(Vector3 start, Vector3 end)
	{
		//ExtraDebug.draw.Line(start, end, 15.0f);
		var radius = 15.0f;

		//var capsule = new Capsule(start, end, radius);
		var capsule = Capsule.FromHeightAndRadius(5.0f, radius);
		var trace = Scene.Trace.Capsule(capsule, start, end).IgnoreGameObjectHierarchy(GameObject).WithTag("player_hitbox").WithoutTags("ragdoll");
		//var trace = Scene.Trace.Ray(start, end).IgnoreGameObjectHierarchy(GameObject).WithTag("player_hitbox").WithoutTags("ragdoll");
		foreach (var impalee in impaledCharacters)
		{
			trace = trace.IgnoreGameObjectHierarchy(impalee);
		}
		var result = trace.Run();

		//ExtraDebug.draw.Capsule(capsule);
		//ExtraDebug.draw.Capsule(capsule);
		//ExtraDebug.draw.Sphere(start, 100.0f, 8, 15.0f);
		//Gizmo.Draw.LineSphere(start, 100.0f, 8);

		if (!result.Hit)
		{
			return;
		}
		Sound.Play("harpoon.impact.flesh", result.HitPosition);
		var characterBody = result.GameObject.Components.Get<CharacterBody>();
		if (characterBody == null)
		{
			Log.Warning($"Spear hit '{result.GameObject}' but it didn't have a character body?");
			return;
		}

		//Log.Info($"collision.Other.Body: {collision.Other.Body}");
		var osCharacter = (OSCharacter)characterBody.owner;
		DamageInfo damageInfo = new DamageInfo();
		damageInfo.instigator = owner?.instigator?.owner;
		damageInfo.damageCauser = this;
		damageInfo.hitBodyIndex = GetClosestSafeIndex(characterBody.bodyPhysics, result.Body.GroupIndex);
		damageInfo.hitVelocity = Transform.World.Forward * 100.0f;
		Log.Info($"Spear Hit Transform.Position: {Transform.Position}");
		Debuggin.draw.Sphere(start, 10.0f, 8, 15.0f);
		//ExtraDebug.draw.Sphere(Transform.Position, 10.0f, 8, 15.0f);
		Debuggin.draw.Line(Transform.Position, Transform.Position + damageInfo.hitVelocity, 15.0f);
		characterBody.TakeDamage(damageInfo);		

		if (owner is HarpoonGun harpoonGun)
		{
			harpoonGun.Reload();
			PlayerInfo.local.OnScoreKill();
		}

		//((OSCharacterBody)characterBody).Impale(this, result.Body.GroupIndex);
		//characterBody.bodyPhysics

		if (!impaledCharacters.Contains(characterBody.GameObject))
		{
			impaledCharacters.Add(characterBody.GameObject);
		}
		else
		{
			Log.Info($"impaledCharacters alread had: {characterBody.GameObject}");
		}

		if (impaledCharacters.Count > 2)
		{
			Log.Info($"Unlock double_penetration achievement!");
			//Sandbox.Services.Achievements.Unlock("double_penetration");
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
