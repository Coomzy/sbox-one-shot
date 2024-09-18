
using Sandbox;
using Sandbox.Physics;
using System;
using System.Diagnostics;

[Group("GMF")]
public class HarpoonSpear : Projectile, Component.ICollisionListener
{	
	[Group("Setup"), Property] public Rigidbody rigidbody { get; set; }
	[Group("Setup"), Property] public CapsuleCollider capsuleCollider { get; set; }
	[Group("Setup"), Order(-1), Property] public SkinnedModelRenderer model { get; set; }
	[Group("Setup"), Order(-1), Property] public HighlightOutline outline { get; set; }

	[Group("Config"), Property] public float launchForce { get; set; } = 75.0f;
	[Group("Config"), Property] public float traceDistance { get; set; } = 15.0f;

	[Group("Runtime"), Property, ReadOnly] public bool isInFlight { get; set; } = true;
	public PhysicsBody hitPhysicsBody { get; set; }

	List<GameObject> impaledCharacters { get; set; } = new List<GameObject>();

	protected override void OnStart()
	{
		base.OnStart();

		if (IsProxy)
		{
			rigidbody.Enabled = false;
			capsuleCollider.Enabled = false;
		}

		lastFixedUpdatePos = Transform.Position;
	}

	protected override void OnUpdate()
	{
		//CheckForImpale();
	}

	void CheckForImpale()
	{
		if (!isInFlight)
			return;

		var startPos = GameObject.Transform.World.Position;
		var endPos = startPos + GameObject.Transform.World.Forward * traceDistance;

		var mapColliders = Scene.GetAllComponents<MapCollider>().ToArray();
		var mapCollider = mapColliders.Count() > 0 ? mapColliders[0] : null;
		//Log.Info($"mapCollider: {mapCollider}");

		//Gizmo.Draw.Line(startPos, endPos);

		var traceResult = Scene.Trace
			.Ray(startPos, endPos)
			.IgnoreGameObjectHierarchy(GameObject)
			//.IgnoreGameObject(mapCollider?.GameObject)
			.WithoutTags("trigger")
			.Run();

		if (traceResult.Hit)
		{
			Log.Info($"hit: {traceResult.GameObject?.Name}");
			isInFlight = false;
			rigidbody.Enabled = false;
		}
	}

	public void Launch(Vector3 characterVelocity)
	{
		var launchVelocity = GameObject.Transform.World.Forward * launchForce;
		//launchVelocity += characterVelocity;
		rigidbody.ApplyImpulse(launchVelocity);
	}

	Vector3 lastFixedUpdatePos = Vector3.Zero;
	protected override void OnFixedUpdate()
	{
		if (IsProxy)
			return;

		if (isInFlight)
		{
			DoFlightPlayerHitDetection();
			return;
		}

		//MovePlayerToHarpoon();
	}

	void DoFlightPlayerHitDetection()
	{
		var start = Transform.Position;
		var end = lastFixedUpdatePos;
		var radius = 15.0f;

		var capsule = new Capsule(start, end, radius);
		var trace = Scene.Trace.Capsule(capsule).IgnoreGameObjectHierarchy(GameObject).WithTag("player_hitbox").WithoutTags("ragdoll");
		foreach (var impalee in impaledCharacters)
		{
			trace = trace.IgnoreGameObjectHierarchy(impalee);
		}
		var result = trace.Run();

		//Gizmo.Draw.LineCapsule(capsule);

		if (result.Hit)
		{
			var characterBody = result.GameObject.Components.Get<CharacterBody>();

			if (characterBody != null)
			{
				//Log.Info($"collision.Other.Body: {collision.Other.Body}");
				var osCharacter = (OSCharacter)characterBody.owner;
				DamageInfo damageInfo = new DamageInfo();
				damageInfo.instigator = owner?.instigator?.owner;
				damageInfo.damageCauser = this; 
				damageInfo.hitBody = result.Body; 
				//osCharacter.Die(damageInfo);

				((OSCharacterBody)characterBody).Impale(this, result.Body.GroupIndex);
				//characterBody.bodyPhysics

				hitPhysicsBody = result.Body;
				//TestFollow(result.Body);
				impaledCharacters.Add(characterBody.GameObject);
			}
			Log.Info($"Spear Trace hit: {result.GameObject}");
		}

		lastFixedUpdatePos = Transform.Position;
	}

	void MovePlayerToHarpoon()
	{
		if (hitPhysicsBody == null)
			return;

		var followToPos = GameObject.Transform.World.Position - (GameObject.Transform.World.Forward * 30.0f);
		//hitPhysicsBody.Position = followToPos;
		var delta = Vector3.Direction(hitPhysicsBody.Position, followToPos);
		hitPhysicsBody.Velocity = delta * 500.0f;
		if (delta.Length < 15.0f)
		{
			hitPhysicsBody.Velocity = Vector3.Zero;
			hitPhysicsBody.AngularVelocity = MathY.MoveTowards(hitPhysicsBody.AngularVelocity, Vector3.Zero, Time.Delta * 15.0f);
			hitPhysicsBody.Position = followToPos;
		}
	}

	async void TestFollow(PhysicsBody hitPhysicsBody)
	{
		TimeSince startFollow = 0;

		while (this.Enabled)
		{
			var followToPos = GameObject.Transform.World.Position - (GameObject.Transform.World.Forward * 30.0f);
			//hitPhysicsBody.Position = followToPos;
			var delta = Vector3.Direction(hitPhysicsBody.Position, followToPos);
			hitPhysicsBody.Velocity = delta * 500.0f;
			if (delta.Length < 15.0f)
			{
				hitPhysicsBody.Velocity = Vector3.Zero;
				hitPhysicsBody.AngularVelocity = MathY.MoveTowards(hitPhysicsBody.AngularVelocity, Vector3.Zero, Time.Delta * 15.0f);
				hitPhysicsBody.Position = followToPos;
			}
			await Task.Frame();
		}
	}

	void ICollisionListener.OnCollisionStart(Sandbox.Collision collision)
	{
		var osPawn = collision.Other.GameObject.Components.Get<OSCharacterBody>();

		if (osPawn != null)
		{
			return;
		}

		//Log.Info($"OnCollisionStart() collision: {collision.Other.GameObject.Name}");
		rigidbody.Enabled = false;
		outline.Enabled = true;
		var dir = Vector3.Direction(GameObject.Transform.Position, collision.Contact.Point);
		GameObject.Transform.Rotation = Rotation.From(dir.EulerAngles);
		//GameObject.Transform.Rotation = lastRot;
		GameObject.Transform.Position = collision.Contact.Point + (dir.Normal * 15.0f);

		if (collision.Contact.Point == Vector3.Zero)
		{
			Log.Info("Collision point at world origin?");
		}
	}
}
