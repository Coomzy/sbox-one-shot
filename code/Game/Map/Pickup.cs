
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


	[Broadcast]
	public void Replenish()
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
	public void PickedUp()
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

	public void OnTriggerEnter(Collider other)
	{
		if (!isActive)
			return;

		var osPawn = other.Components.Get<OSCharacter>();
		var harpoonGun = (HarpoonGun)osPawn?.equippedItem;
		if (harpoonGun == null)
			return;

		if (osPawn.IsProxy)
			return;

		if (harpoonGun.hasAmmo || harpoonGun.isReloading)
			return;

		harpoonGun.Reload();
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
