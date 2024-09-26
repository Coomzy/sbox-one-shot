
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
		damageInfo.hitBodyIndex = result.Body.GroupIndex;
		damageInfo.hitVelocity = Transform.World.Forward * 100.0f;
		Log.Info($"Spear Hit Transform.Position: {Transform.Position}");
		Debuggin.draw.Sphere(start, 10.0f, 8, 15.0f);
		//ExtraDebug.draw.Sphere(Transform.Position, 10.0f, 8, 15.0f);
		Debuggin.draw.Line(Transform.Position, Transform.Position + damageInfo.hitVelocity, 15.0f);
		characterBody.TakeDamage(damageInfo);

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
}
