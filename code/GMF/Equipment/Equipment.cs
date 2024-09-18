
using Sandbox;
using System;
using System.Numerics;

[Group("GMF")]
public class Equipment : Component, Component.INetworkSpawn
{
	[Group("Setup"), Property] public SkinnedModelRenderer model { get; set; }
	[Group("Setup"), Property] public ProceduralAnimation procAnim { get; set; }
	[Group("Setup"), Property] public GameObject equipmentProxyPrefab { get; set; }
	[Group("Setup"), Property] public GameObject collisionChild { get; set; }
	[Group("Setup"), Property] public Rigidbody rigidbody { get; set; }

	[Group("Config"), Property] public float attackCooldownTime { get; set; } = 0.25f;

	[Group("Config"), Property] public Vector3 hip { get; set; } = new Vector3(16.5f, -13.0f, -11.0f);
	[Group("Config"), Property] public Vector3 ironSights { get; set; } = new Vector3(15.0f, 0.0f, -8.25f);

	[Group("Runtime"), Order(100), Property, Sync] public Character instigator { get; set; }
	[Group("Runtime"), Order(100), Property] public EquipmentProxy equipmentProxy { get; set; }

	[Group("Runtime"), Property, ReadOnly] public bool hasFireInputDown { get; set; }
	[Group("Runtime"), Property, ReadOnly] public TimeSince lastFire { get; set; }

	[Group("Runtime"), Property, ReadOnly] public bool hasFireAltInputDown { get; set; }
	[Group("Runtime"), Property, ReadOnly] public TimeSince lastFireAlt { get; set; }

	protected async override void OnStart()
	{
		while (instigator == null)
			await Task.Frame();

		if (procAnim != null)
		{
			procAnim.Enabled = !IsProxy;
			procAnim.owner = instigator;
		}

		var proxyInst = equipmentProxyPrefab.Clone();
		equipmentProxy = proxyInst.Components.Get<EquipmentProxy>();
		
		equipmentProxy.GameObject.SetParent(instigator.body.thirdPersonEquipmentAttachPoint);
		equipmentProxy.Transform.LocalPosition = Vector3.Zero;
		equipmentProxy.Transform.LocalRotation = Quaternion.Identity;

		if (IsProxy)
		{
			return;
		}

		model.RenderOptions.Overlay = true;
		equipmentProxy.ShadowOnly();
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
		equipmentProxy.ShadowOnly();
	}
}
