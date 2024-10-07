
using System;

public class Pickup : Component, Component.ITriggerListener
{
	[Group("Setup"), Property] public BoxCollider boxCollider { get; set; }
	[Group("Setup"), Property] public DecalRenderer decalRenderer { get; set; }
	[Group("Setup"), Property] public GameObject visualHolder { get; set; }
	//[Group("Setup"), Property] public SkinnedModelRenderer skinnedModelRenderer { get; set; }

	[Group("Config"), Property] public float cooldownTime { get; set; } = 10.0f;
	[Group("Config"), Property] public Color activeColor { get; set; } = Color.Green;
	[Group("Config"), Property] public Color inactiveColor { get; set; } = Color.Red;

	[Group("Runtime"), Property, HostSync] public bool isActive { get; set; } = true;
	[Group("Runtime"), Property] public TimeSince lastPickup { get; set; }

	// TODO: This is shite, this whole thing needs to be DE-OneShot-ifyied
	[Group("Runtime"), Property] public OSCharacter trackingCharacter { get; set; }
	[Group("Runtime"), Property] public HarpoonGun trackingGun { get; set; }

	protected override void OnAwake()
	{
		if (IsProxy)
			return;

		isActive = true;
	}

	protected override void OnUpdate()
	{
		if (!IsProxy)
		{
			if (!isActive && lastPickup >= cooldownTime)
			{
				Replenish();
			}
		}

		CheckTrackingCharacter();

		UpdateVisuals();
	}

	protected virtual void UpdateVisuals()
	{
		var visualPos = visualHolder.LocalPosition;
		var visualAngles = visualHolder.LocalRotation.Angles();

		float bobRate = 7.5f;
		float bobAmount = 10.0f;
		var sinLerp = MathF.Sin(Time.Now * bobRate);
		visualPos.z = sinLerp * bobAmount;

		var spinSpeed = 350.0f;
		visualAngles.yaw += Time.Delta * spinSpeed;

		visualHolder.LocalPosition = visualPos;
		visualHolder.LocalRotation = visualAngles.ToRotation();
	}

	protected virtual void CheckTrackingCharacter()
	{
		if (!IsFullyValid(trackingCharacter, trackingGun))
		{
			trackingCharacter = null;
			trackingGun = null;
			return;
		}

		if (trackingCharacter.isDead)
		{
			trackingCharacter = null;
			trackingGun = null;
			return;
		}

		TryPickup();
	}


	[Broadcast]
	public virtual void Replenish()
	{
		visualHolder.Enabled = true;
		decalRenderer.TintColor = activeColor;
		if (IsProxy)
		{
			return;
		}

		isActive = true;
	}

	[Broadcast]
	public virtual void PickedUp()
	{
		Sound.Play("pickup.pickedup", WorldPosition);
		visualHolder.Enabled = false;
		decalRenderer.TintColor = inactiveColor;
		if (IsProxy)
		{
			return;
		}

		isActive = false;
		lastPickup = 0;
	}

	public virtual void OnTriggerEnter(Collider other)
	{
		var osCharacter = other.Components.Get<OSCharacter>();

		if (osCharacter.IsProxy)
			return;

		var harpoonGun = (HarpoonGun)osCharacter?.equippedItem;
		if (harpoonGun == null)
			return;

		trackingCharacter = osCharacter;
		trackingGun = harpoonGun;

		TryPickup();
	}

	public virtual void OnTriggerExit(Collider other)
	{
		var osCharacter = other.Components.Get<OSCharacter>();
		if (osCharacter != trackingCharacter)
			return;

		var harpoonGun = (HarpoonGun)osCharacter?.equippedItem;
		if (harpoonGun != trackingGun)
			return;

		trackingCharacter = null;
		trackingGun = null;
	}

	public virtual bool CanPickup()
	{
		if (!IsFullyValid(trackingCharacter, trackingGun))
			return false;

		if (!isActive)
			return false;

		if (trackingCharacter.isDead)
			return false;

		if (trackingGun.hasAmmo || trackingGun.isReloading)
			return false;

		return true;
	}

	public virtual void TryPickup()
	{
		if (!CanPickup())
			return;

		trackingGun.Reload();
		PickedUp();
	}

	public override void Reset()
	{
		base.Reset();

		SetupBoxCollider();
	}

	void SetupBoxCollider()
	{
		boxCollider = GameObject.Components.GetOrCreate<BoxCollider>();
		boxCollider.IsTrigger = true;
	}
}
