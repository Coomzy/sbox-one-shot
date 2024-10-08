
using Sandbox;
using System;
using System.Numerics;
using System.Text.Json.Serialization;

[Group("GMF")]
public class Equipment : Component, IGameModeEvents
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
	[Group("Config"), Property] public float zoomFOVScalar { get; set; } = 0.6f;
	[Group("Config"), Property] public float zoomFOVRate { get; set; } = 400.0f;

	[Group("Runtime"), Order(100), Property, Sync, Change("OnRep_instigator")] public Character instigator { get; set; }
	[Group("Runtime"), Order(100), Property, ReadOnly, JsonIgnore] public EquipmentProxy equipmentProxy { get; set; }

	[Group("Runtime"), Property, ReadOnly] public bool hasFireInputDown { get; set; }
	[Group("Runtime"), Property, ReadOnly] public TimeSince fireStartTime { get; set; }
	[Group("Runtime"), Property, ReadOnly] public TimeSince lastFire { get; set; }
	[Group("Runtime"), Property, ReadOnly] public TimeSince lastDryFire { get; set; }

	[Group("Runtime"), Property, ReadOnly] public bool hasFireAltInputDown { get; set; }
	[Group("Runtime"), Property, ReadOnly] public TimeSince fireAltStartTime { get; set; }
	[Group("Runtime"), Property, ReadOnly] public TimeSince lastFireAlt { get; set; }
	[Group("Runtime"), Property, ReadOnly] public TimeSince lastDryFireAlt { get; set; }

	protected override void OnAwake()
	{
		model.GameObject.Enabled = true;
		procAnim.equipment = this;

		var clone = equipmentProxyPrefab.Clone();
		equipmentProxy = clone.Components.Get<EquipmentProxy>();
		equipmentProxy.equipment = this;
		clone.NetworkMode = NetworkMode.Never;
	}	

	protected override void OnStart()
	{
		SetFirstPersonMode(!IsProxy);

		if (instigator != null)
		{
			OnRep_instigator(null, instigator);
		}
	}

	public virtual void SetFirstPersonMode(bool isFirstPerson)
	{
		model.Enabled = isFirstPerson;
		model.RenderType = ModelRenderer.ShadowRenderType.Off;
		model.RenderOptions.Overlay = isFirstPerson;

		procAnim.Enabled = isFirstPerson;
	}

	public virtual void OnRep_instigator(Character oldValue, Character newValue){}

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
		LocalPosition = Vector3.Zero;
		LocalRotation = Quaternion.Identity;
	}

	public virtual void FireStart()
	{
		hasFireInputDown = true;
		fireStartTime = 0;

		if (CanFire())
		{
			Fire();
		}
		else if (CanDryFire())
		{
			DryFire();
		}
	}

	public virtual void FireEnd()
	{
		hasFireInputDown = false;
	}

	public virtual void FireAltStart()
	{
		hasFireAltInputDown = true;
		fireStartTime = 0;

		if (CanFireAlt())
		{
			FireAlt();
		}
		else if (CanDryFireAlt())
		{
			DryFireAlt();
		}
	}

	public virtual void FireAltEnd()
	{
		hasFireAltInputDown = false;
	}

	public virtual void Fire()
	{
		lastFire = 0;
	}

	public virtual void FireAlt()
	{
		lastFireAlt = 0;
	}

	public virtual void DryFire()
	{
		lastDryFire = 0;
	}

	public virtual void DryFireAlt()
	{
		lastDryFireAlt = 0;
	}

	public virtual bool CanFire()
	{
		if (lastFire < attackCooldownTime)
		{
			return false;
		}

		return true;
	}

	public virtual bool CanFireAlt()
	{
		return true;
	}

	public virtual bool CanDryFire()
	{
		if (lastDryFire < attackCooldownTime)
		{
			return false;
		}

		return true;
	}

	public virtual bool CanDryFireAlt()
	{
		return false;
	}

	public virtual bool IsRequestingFOVZoom(out float targetFOV, out float transitionRate)
	{
		ApplyZoomFOV(out targetFOV, out transitionRate);

		if (!hasFireAltInputDown)
			return false;

		if (!IsFullyValid(instigator?.owner))
			return false;

		if (instigator.owner.isDead)
			return false;


		return true;
	}

	public virtual void ApplyZoomFOV(out float targetFOV, out float transitionRate)
	{
		targetFOV = Preferences.FieldOfView * zoomFOVScalar;
		transitionRate = zoomFOVRate;
	}

	public virtual void Drop(Vector3 force)
	{
		Drop_Remote();

		rigidbody.ApplyImpulse(force);
		var weaponRandomTorque = Game.Random.Float(1500.0f, 3500.0f);
		rigidbody.ApplyTorque(Game.Random.VectorInSphere().Normal * weaponRandomTorque * 5000.0f);
	}

	[Broadcast]
	protected virtual void Drop_Remote()
	{
		GameObject.SetParent(null, true);

		collisionChild.Enabled = true;
		procAnim.Enabled = false;
		rigidbody.Enabled = true;

		model.RenderOptions.Overlay = false;
		model.Enabled = true;

		if (equipmentProxy != null)
		{
			equipmentProxy.SetVisibility(false);
		}
	}

	public virtual void RoundCleanup()
	{
		if (IsProxy)
			return;

		GameObject.Destroy();
	}
}
