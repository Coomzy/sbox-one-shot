
using Sandbox;
using Sandbox.Citizen;
using System.Text.Json.Serialization;
using static Sandbox.ModelRenderer;

[Group("OS")]
public class OSCharacterBody : CharacterBody, Component.INetworkSpawn
{
	[Group("Runtime"), Order(100), Property, Sync] public HarpoonSpear impaledByHarpoonSpear { get; set; }
	[Group("Runtime"), Order(100), Property, Sync] public int impaledPhysicsBodyIndex { get; set; } = -1;

	protected override void OnAwake()
	{
		base.OnAwake();

		thirdPersonAnimationHelper.Handedness = CitizenAnimationHelper.Hand.Both;
		thirdPersonAnimationHelper.HoldType = CitizenAnimationHelper.HoldTypes.Rifle;
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if (bodyPhysics?.PhysicsGroup?.Bodies == null || bodyPhysics.PhysicsGroup.Bodies.Count() < impaledPhysicsBodyIndex || impaledPhysicsBodyIndex < 0)
			return;

		var impaledPhysicsBody = bodyPhysics.PhysicsGroup.Bodies.ElementAt(impaledPhysicsBodyIndex);

		if (impaledByHarpoonSpear == null || !impaledByHarpoonSpear.IsValid || impaledPhysicsBody == null || !impaledPhysicsBody.IsValid())
			return;

		SetFirstPersonMode(false);
		bodyPhysics.MotionEnabled = true;
		var followToPos = impaledByHarpoonSpear.GameObject.Transform.World.Position - (impaledByHarpoonSpear.GameObject.Transform.World.Forward * 30.0f);
		//hitPhysicsBody.Position = followToPos;
		var delta = Vector3.Direction(impaledPhysicsBody.Position, followToPos);
		impaledPhysicsBody.Velocity = delta * 500.0f;
		if (delta.Length < 15.0f)
		{
			impaledPhysicsBody.Velocity = Vector3.Zero;
			impaledPhysicsBody.AngularVelocity = MathY.MoveTowards(impaledPhysicsBody.AngularVelocity, Vector3.Zero, Time.Delta * 15.0f);
			impaledPhysicsBody.Position = followToPos;
		}
	}

	public override void Die(DamageInfo damageInfo)
	{
		base.Die(damageInfo);

		Log.Info($"Die() damageInfo.damageCauser: {damageInfo.damageCauser}");

		var instigatorAsHarpoonSpear = (HarpoonSpear)damageInfo.damageCauser;
		Log.Info($"Die() instigatorAsHarpoonSpear: {instigatorAsHarpoonSpear}");
		if (instigatorAsHarpoonSpear != null)
		{
			impaledByHarpoonSpear = instigatorAsHarpoonSpear;
		}
		//impaledPhysicsBody = damageInfo.hitBody;
	}

	[Broadcast]
	public void Impale(Component spear, int bodyIndex)
	{
		if (owner?.equippedItem != null)
			owner.equippedItem.Drop(Vector3.Zero);

		bodyPhysics.Enabled = true;
		bodyPhysics.MotionEnabled = true;
		GameObject.Tags.Set("ragdoll", true);

		var instigatorAsHarpoonSpear = (HarpoonSpear)spear;
		var physBody = (bodyPhysics?.PhysicsGroup?.Bodies.Count() > bodyIndex) ? bodyPhysics.PhysicsGroup.Bodies.ElementAt(bodyIndex) : null;

		foreach (var body in physBody.PhysicsGroup.Bodies)
		{
			Log.Info($"Die() body: {body}");
		}

		Log.Info($"Die() spear: {spear}, bodyIndex: {bodyIndex}, instigatorAsHarpoonSpear: {instigatorAsHarpoonSpear}, physBody: {physBody}");

		if (instigatorAsHarpoonSpear != null)
		{
			impaledByHarpoonSpear = instigatorAsHarpoonSpear;
		}
		impaledPhysicsBodyIndex = bodyIndex;

		if (IsProxy)
			return;

		Network.SetOrphanedMode(NetworkOrphaned.ClearOwner);
	}
}
