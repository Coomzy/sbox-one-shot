
using Sandbox;
using System;
using System.Numerics;

[Group("GMF")]
public class Equipment : Component, IRoundInstance, Component.INetworkSpawn
{
	[Group("Setup"), Property] public SkinnedModelRenderer model { get; set; }
	[Group("Setup"), Property] public ProceduralAnimation procAnim { get; set; }
	[Group("Setup"), Property] public GameObject equipmentProxyPrefab { get; set; }
	[Group("Setup"), Property] public GameObject collisionChild { get; set; }
	[Group("Setup"), Property] public Rigidbody rigidbody { get; set; }
	[Group("Setup"), Property] public GameObject muzzleSocket { get; set; }
	[Group("Setup"), Property] public GameObject twoHandedGrip { get; set; }

	[Group("Config"), Property] public float attackCooldownTime { get; set; } = 0.25f;

	[Group("Config"), Property] public Vector3 hip { get; set; } = new Vector3(16.5f, -13.0f, -11.0f);
	[Group("Config"), Property] public Vector3 ironSights { get; set; } = new Vector3(15.0f, 0.0f, -8.25f);

	[Group("Runtime"), Order(100), Property, Sync, Change("OnRep_instigator")] public Character instigator { get; set; }
	[Group("Runtime"), Order(100), Property] public EquipmentProxy equipmentProxy { get; set; }

	[Group("Runtime"), Property, ReadOnly] public bool hasFireInputDown { get; set; }
	[Group("Runtime"), Property, ReadOnly] public TimeSince lastFire { get; set; }

	[Group("Runtime"), Property, ReadOnly] public bool hasFireAltInputDown { get; set; }
	[Group("Runtime"), Property, ReadOnly] public TimeSince lastFireAlt { get; set; }

	protected override void OnAwake()
	{
		model.GameObject.Enabled = true;
		procAnim.equipment = this;

		var clone = equipmentProxyPrefab.Clone();
		equipmentProxy = clone.Components.Get<EquipmentProxy>();
		equipmentProxy.equipment = this;
		clone.NetworkMode = NetworkMode.Never;
	}

	protected async override void OnStart()
	{
		SetFirstPersonMode(!IsProxy);

		if (instigator != null)
		{
			OnRep_instigator(null, instigator);
		}

		while (instigator == null)
			await Task.Frame();

		OnRep_instigator(null, instigator);
	}

	public virtual void SetFirstPersonMode(bool isFirstPerson)
	{
		model.Enabled = isFirstPerson;
		model.RenderType = ModelRenderer.ShadowRenderType.Off;
		model.RenderOptions.Overlay = isFirstPerson;

		procAnim.Enabled = isFirstPerson;

		Log.Info($"");
	}

	public virtual void OnRep_instigator(Character oldValue, Character newValue)
	{
		return;
		if (Check.IsFullyValid(equipmentProxy, instigator?.body?.thirdPersonEquipmentAttachPoint))
		{
			Log.Info($"OninstigatorChanged() attempted valid attach! Yay");
			equipmentProxy.AttachTo(instigator.body.thirdPersonEquipmentAttachPoint);
		}
		else if (Check.IsFullyValid(newValue))
		{
			Log.Error($"OninstigatorChanged() and have an instigator '{newValue}' but something is null equipmentProxy '{equipmentProxy}' body '{newValue?.body}' thirdPersonEquipmentAttachPoint '{newValue?.body?.thirdPersonEquipmentAttachPoint}'");
		}

		if (IsProxy)
		{
			//equipmentProxy.GameObject.SetParent(instigator.body.thirdPersonEquipmentAttachPoint);
			//equipmentProxy.Transform.LocalPosition = Vector3.Zero;
			//equipmentProxy.Transform.LocalRotation = Quaternion.Identity;
			return;
		}

		/*var proxyInst = equipmentProxyPrefab.Clone();
		equipmentProxy = proxyInst.Components.Get<EquipmentProxy>();
		proxyInst.NetworkMode = NetworkMode.Never;

		equipmentProxy.GameObject.SetParent(instigator.body.thirdPersonEquipmentAttachPoint);
		equipmentProxy.Transform.LocalPosition = Vector3.Zero;
		equipmentProxy.Transform.LocalRotation = Quaternion.Identity;*/

		model.RenderOptions.Overlay = true;
		//equipmentProxy.ShadowOnly();
		//Network.DisableInterpolation();
	}

	[ConCmd]
	public static void LocalBroadcastAttach()
	{
		PlayerInfo.local.character.equippedItem.BroadcastAttach();
	}

	[Broadcast]
	public void BroadcastAttach()
	{
		if(!IsProxy)
			return;

		GameObject.SetParent(instigator.body.thirdPersonEquipmentAttachPoint);
		Transform.LocalPosition = Vector3.Zero;
		Transform.LocalRotation = Quaternion.Identity;
	}

	public async virtual void OnNetworkSpawn(Connection connection)
	{
		//Log.Info($"Equipment::OnNetworkSpawn() PRE connection: {connection}");

		await Task.Frame();

		//Log.Info($"Equipment::OnNetworkSpawn() POST connection: {connection}");
		/*if (IsProxy)
		{			
			return;
		}

		model.SceneObject.Flags.OverlayLayer = true;*/
	}

	protected override void OnUpdate()
	{
		//Vector3 targetPos = hasFireAltInputDown ? ironSights : hip;
		//float adsSpeed = 200.0f;
		//GameObject.Parent.Transform.LocalPosition = MathY.MoveTowards(GameObject.Parent.Transform.LocalPosition, targetPos, Time.Delta * adsSpeed);		
	}

	public virtual void FireStart()
	{
		hasFireInputDown = true;
		lastFire = 0;
	}

	public virtual void FireEnd()
	{
		hasFireInputDown = false;
	}

	public virtual void FireAltStart()
	{
		hasFireAltInputDown = true;
		lastFireAlt = 0;
	}

	public virtual void FireAltEnd()
	{
		hasFireAltInputDown = false;
	}

	public virtual void TryFire()
	{
		if (!CanFire())
			return;

		Fire_Local();
	}

	public virtual void Fire_Local()
	{
		lastFire = 0;
	}

	public virtual bool CanFire()
	{
		if (lastFire < attackCooldownTime)
			return false;

		return true;
	}

	public virtual void Drop(Vector3 force)
	{
		GameObject.SetParent(null, true);

		collisionChild.Enabled = true;
		procAnim.Enabled = false;
		rigidbody.Enabled = true;

		rigidbody.ApplyImpulse(force);
		var weaponRandomTorque = Game.Random.Float(1500.0f, 3500.0f);
		rigidbody.ApplyTorque(Game.Random.Rotation().Forward * weaponRandomTorque);

		model.RenderOptions.Overlay = false;

		if (equipmentProxy != null)
		{
			equipmentProxy.SetVisibility(false);
		}
	}

	public void Cleanup()
	{
		if (IsProxy)
			return;

		GameObject.Destroy();
	}
}
