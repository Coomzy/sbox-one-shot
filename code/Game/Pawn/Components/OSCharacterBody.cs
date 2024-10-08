
using System;

[Group("OS")]
public class OSCharacterBody : CharacterBody
{
	[Group("Runtime"), Order(100), Property, Sync] public HarpoonSpear impaledByHarpoonSpear { get; set; }
	[Group("Runtime"), Order(100), Property, Sync] public int impaledPhysicsBodyIndex { get; set; } = -1;

	//protected override void OnFixedUpdate()
	protected override void OnUpdate()
	{
		//base.OnFixedUpdate();
		base.OnUpdate();

		if (bodyPhysics?.PhysicsGroup?.Bodies == null || bodyPhysics.PhysicsGroup.Bodies.Count() < impaledPhysicsBodyIndex || impaledPhysicsBodyIndex < 0)
			return;

		var impaledPhysicsBody = bodyPhysics.PhysicsGroup.Bodies.ElementAt(impaledPhysicsBodyIndex);

		if (impaledByHarpoonSpear == null || !impaledByHarpoonSpear.IsValid || impaledPhysicsBody == null || !impaledPhysicsBody.IsValid())
			return;

		// Move to OnRep?
		SetFirstPersonMode(false);
		bodyPhysics.MotionEnabled = true;
		bodyRenderer.UseAnimGraph = false;

		//var followToPos = impaledByHarpoonSpear.GameObject.Transform.World.Position - (impaledByHarpoonSpear.GameObject.Transform.World.Forward * 30.0f);
		var followToPos = impaledByHarpoonSpear.impalePoint.WorldPosition;

		if (impaledPhysicsBodyIndex == Bones.Terry.spine_0)
		{
			followToPos += Vector3.Up * 10.0f;
		}

		/*impaledPhysicsBody.Velocity = Vector3.Zero;
		impaledPhysicsBody.AngularVelocity = MathY.MoveTowards(impaledPhysicsBody.AngularVelocity, Vector3.Zero, Time.Delta * 15.0f);
		impaledPhysicsBody.AngularVelocity = Vector3.Zero;
		impaledPhysicsBody.Position = followToPos;*/

		float smoothRate = 0.075f;
		//smoothRate = 0.0001f;
		//smoothRate = 0.005f;
		smoothRate = 0.025f;

		float smoothRateRot = 0.075f;
		//smoothRateRot = 0.0001f;

		var velocity = impaledPhysicsBody.Velocity;
		var targetPos = impaledByHarpoonSpear.WorldPosition;
		targetPos = impaledByHarpoonSpear.impalePoint.WorldPosition;
		Vector3.SmoothDamp(impaledPhysicsBody.Position, targetPos, ref velocity, smoothRate, Time.Delta);
		//Vector3.SmoothDamp(impaledPhysicsBody.Position, targetPos, ref velocity, smoothRate, Game.ActiveScene.FixedDelta);
		impaledPhysicsBody.Velocity = velocity;
		//impaledPhysicsBody.Position = MathY.MoveTowards(impaledPhysicsBody.Position, targetPos, Game.ActiveScene.FixedDelta);
		//impaledPhysicsBody.Velocity = (followToPos - impaledPhysicsBody.Position) * 25.0f;

		if (!impaledByHarpoonSpear.isInFlight)
		{
			//impaledPhysicsBody.Position = MathY.MoveTowards(impaledPhysicsBody.Position, targetPos, Time.Delta * 100.0f);
		}

		if (!impaledByHarpoonSpear.isInFlight)
		{
			//impaledPhysicsBody.Velocity = (followToPos - impaledPhysicsBody.Position) * 150.0f;
		}

		var angularVelocity = impaledPhysicsBody.AngularVelocity;
		var targetRot = impaledByHarpoonSpear.WorldRotation;
		targetRot = Rotation.Identity;
		Rotation.SmoothDamp(impaledPhysicsBody.Rotation, targetRot, ref angularVelocity, smoothRateRot, Time.Delta);
		impaledPhysicsBody.AngularVelocity = angularVelocity;
		//impaledPhysicsBody.AngularVelocity = Vector3.Zero;
	}

	public override void Die(DamageInfo damageInfo)
	{
		if (damageInfo.damageCauser is HarpoonSpear instigatorAsHarpoonSpear)
		{
			impaledByHarpoonSpear = instigatorAsHarpoonSpear;
			Impale(damageInfo.damageCauser, damageInfo.hitBodyIndex);
			return;
		}

		base.Die(damageInfo);
	}

	public void Impale(Component spear, int bodyIndex)
	{
		bodyPhysics.Enabled = true;
		bodyPhysics.MotionEnabled = true;
		GameObject.Tags.Set("ragdoll", true);

		var physBody = (bodyPhysics?.PhysicsGroup?.Bodies.Count() > bodyIndex) ? bodyPhysics.PhysicsGroup.Bodies.ElementAt(bodyIndex) : null;

		impaledByHarpoonSpear = spear as HarpoonSpear;
		impaledPhysicsBodyIndex = bodyIndex;
	}

	// NOT IMPLEMENTED: This is to freeze the body position, which probably doesn't need to be done
	// There is probably a better way to do this as well
	public void CaptureBodyPos()
	{
		var idToPosRot = CapturePosRot(bodyPhysics.GameObject);
		bodyPhysics.Enabled = false;
		bodyPhysics.MotionEnabled = false;
		SetPosRot(bodyPhysics.GameObject, idToPosRot);
	}

	public Dictionary<Guid, (Vector3 pos, Rotation rot)> CapturePosRot(GameObject target, Dictionary<Guid, (Vector3 pos, Rotation rot)> idToPosRot = null)
	{
		idToPosRot = idToPosRot ?? new Dictionary<Guid, (Vector3, Rotation)>();

		idToPosRot[target.Id] = (target.WorldPosition, target.WorldRotation);

		foreach (var child in target.Children)
		{
			CapturePosRot(child, idToPosRot);
		}

		return idToPosRot;
	}

	public void SetPosRot(GameObject target, Dictionary<Guid, (Vector3 pos, Rotation rot)> idToPosRot)
	{
		target.WorldPosition = idToPosRot[target.Id].pos;
		target.WorldRotation = idToPosRot[target.Id].rot;
		target.Flags = GameObjectFlags.ProceduralBone;

		foreach (var child in target.Children)
		{
			SetPosRot(child, idToPosRot);
		}
	}
}
