
using Sandbox;
using Sandbox.Citizen;
using System;
using System.Text.Json.Serialization;
using static Sandbox.ModelRenderer;

[Group("OS")]
public class OSCharacterBody : CharacterBody, Component.INetworkSpawn
{
	[Group("Runtime"), Order(100), Property, Sync, OnRep] public HarpoonSpear impaledByHarpoonSpear { get; set; }
	[Group("Runtime"), Order(100), Property, Sync, Change(nameof(OnRep_impaledPhysicsBodyIndex))] public int impaledPhysicsBodyIndex { get; set; } = -1;

	protected override void OnAwake()
	{
		base.OnAwake();

		thirdPersonAnimationHelper.Handedness = CitizenAnimationHelper.Hand.Both;
		thirdPersonAnimationHelper.HoldType = CitizenAnimationHelper.HoldTypes.Rifle;
	}

	public void OnRep___impaledByHarpoonSpear__Attrs(HarpoonSpear oldValue, HarpoonSpear newValue)
	{
		Log.Info($"OnRep___impaledByHarpoonSpear__Attrs: {newValue}");
	}

	public void OnRep_impaledByHarpoonSpear(HarpoonSpear oldValue, HarpoonSpear newValue)
	{
		Log.Info($"OnRep_impaledByHarpoonSpear: {newValue}");
	}

	public void OnRep_impaledPhysicsBodyIndex(int oldValue, int newValue)
	{
		Log.Info($"OnRep_impaledPhysicsBodyIndex: {newValue}");
	}

	void OnimpaledByHarpoonSpearChanged(HarpoonSpear oldValue, HarpoonSpear newValue)
	{
		Log.Info($"impaledByHarpoonSpear: {newValue}");
	}

	void OnimpaledPhysicsBodyIndexChanged(int oldValue, int newValue)
	{
		Log.Info($"impaledPhysicsBodyIndex: {newValue}");
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
		//impaledPhysicsBody.Velocity = Vector3.Zero;
		//impaledPhysicsBody.Position = MathY.MoveTowards(impaledPhysicsBody.Position, followToPos, Time.Delta * 100.0f);
		//Log.Info($"delta.Length: {delta.Length}, impaledByHarpoonSpear.isInFlight: {impaledByHarpoonSpear.isInFlight}");

		if (delta.Length < 15.0f)// && !impaledByHarpoonSpear.isInFlight)
		{
			impaledPhysicsBody.Velocity = Vector3.Zero;
			impaledPhysicsBody.AngularVelocity = MathY.MoveTowards(impaledPhysicsBody.AngularVelocity, Vector3.Zero, Time.Delta * 15.0f);
			impaledPhysicsBody.AngularVelocity = Vector3.Zero;
			impaledPhysicsBody.Position = followToPos;
			//impaledPhysicsBody.Velocity = delta * 100.0f;
		}
	}

	public override void Die(DamageInfo damageInfo)
	{
		Log.Info($"OSCharacterBody::Die() damageInfo.damageCauser: {damageInfo.damageCauser}");

		if (damageInfo.damageCauser is HarpoonSpear instigatorAsHarpoonSpear)
		{
			Log.Info($"OSCharacterBody::Die() instigatorAsHarpoonSpear: {instigatorAsHarpoonSpear}");
			impaledByHarpoonSpear = instigatorAsHarpoonSpear;
			Impale(damageInfo.damageCauser, damageInfo.hitBodyIndex);
			return;
		}

		base.Die(damageInfo);
	}

	public override void TakeDamage(DamageInfo damageInfo)
	{
		Log.Info($"OSCharacterBody::TakeDamage() damageInfo.damageCauser: {damageInfo.damageCauser}");
		base.TakeDamage(damageInfo);
	}

	//[Broadcast]
	public void Impale(Component spear, int bodyIndex)
	{
		if (owner?.equippedItem != null)
			owner.equippedItem.Drop(Vector3.Zero);

		bodyPhysics.Enabled = true;
		bodyPhysics.MotionEnabled = true;
		GameObject.Tags.Set("ragdoll", true);

		var instigatorAsHarpoonSpear = spear as HarpoonSpear;
		var physBody = (bodyPhysics?.PhysicsGroup?.Bodies.Count() > bodyIndex) ? bodyPhysics.PhysicsGroup.Bodies.ElementAt(bodyIndex) : null;

		foreach (var body in physBody.PhysicsGroup.Bodies)
		{
			//Log.Info($"Die() body: {body}");
		}

		Log.Info($"Die() spear: {spear}, bodyIndex: {bodyIndex}, instigatorAsHarpoonSpear: {instigatorAsHarpoonSpear}, physBody: {physBody}");

		if (instigatorAsHarpoonSpear != null)
		{
			impaledByHarpoonSpear = instigatorAsHarpoonSpear;
		}
		impaledPhysicsBodyIndex = bodyIndex;

		if (IsProxy)
			return;

		//Network.SetOrphanedMode(NetworkOrphaned.ClearOwner);
		//ClearOwner(3.0f);
		//Network.SetOrphanedMode(NetworkOrphaned.ClearOwner);
	}

	public async void ClearOwner(float delay)
	{
		await Task.DelaySeconds(delay);

		Log.Info($"ClearOwner() PlayerInfo.all: {PlayerInfo.all.Count}");
		foreach (var playerInfo in PlayerInfo.all)
		{
			Log.Info($"playerInfo: {playerInfo}, playerInfo.IsValid: {playerInfo.IsValid}, playerInfo?.GameObject == null: {playerInfo?.GameObject == null}, playerInfo.Network.Active: {playerInfo.Network.Active}, playerInfo.Network.OwnerId: {playerInfo.Network.OwnerId}");
		}

		playerInfo.character.DestroyRequest();//.Network.SetOrphanedMode(NetworkOrphaned.ClearOwner);
		//this.GetOwningPlayerInfo().character = null;
		//Network.SetOrphanedMode(NetworkOrphaned.ClearOwner);
	}
}
