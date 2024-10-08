using badandbest.Sprays;
using Sandbox;
using Sandbox.Citizen;
using Sandbox.Services;
using System;
using System.Numerics;
using System.Threading.Channels;

[Group("OS")]
public class OSCharacter : Character
{
	// TODO: Make some kind of loadout system?
	[Group("Setup"), Property] public GameObject harpoonGunPrefab { get; set; }

	[Group("Runtime"), Property] bool isTrackingGottaGoFast { get; set; } = true;

	protected override void OnAwake()
	{
		base.OnAwake();

		movement.config = movement.config ?? GameSettings.instance.characterMovementConfig;
	}

	protected override void SpawnLoadout()
	{
		base.SpawnLoadout();

		equippedItem = SpawnEquipment(harpoonGunPrefab);
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (isDead)
			return;

		if (IsProxy)
			return;

		if (Input.Pressed(Inputs.spray))
		{
			Spray.Place();
		}

		CheckForGottaGoFastAchievement();
	}

	void CheckForGottaGoFastAchievement()
	{
		if (!isTrackingGottaGoFast)
			return;

		if (!movement.isSliding)
			return;

		if (controller.Velocity.Length < 850.0f)
			return;

		Achievements.Unlock(Achievement.GOTTA_GO_FAST);
	}
}
