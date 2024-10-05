
using Sandbox.Diagnostics;

public class JumpingAchievementVolume : Component, Component.ITriggerListener
{
	enum JumpAchievementType
	{
		None,
		Plank,
		Hangar
	}

	[Property] JumpAchievementType jumpAchievementType { get; set; }
	[Property] public JumpingAchievementVolume linkedVolume { get; set; }
	[Property] public BoxCollider boxCollider { get; set; }
	[Property] public OSCharacter trackingPlayer { get; set; }

	[ConVar] public static bool debug_achievement_jumping { get; set; }

	protected override void OnUpdate()
	{
		if (!HasCharacterMetRequirements(trackingPlayer))
		{
			trackingPlayer = null;
		}
	}

	bool HasCharacterMetRequirements(OSCharacter character)
	{
		if (!IsFullyValid(trackingPlayer?.movement))
			return false;

		if (trackingPlayer.isDead)
			return false;

		if (trackingPlayer.movement.isGrounded && trackingPlayer.movement.lastUngrounded > 0.25f)
			return false;

		if (trackingPlayer.movement.isMantling)
			return false;

		return true;
	}

	public void OnTriggerEnter(Collider other)
	{
		var osCharacter = other.Components.Get<OSCharacter>();
		if (osCharacter == null)
			return;

		if (osCharacter.IsProxy)
			return;

		if (debug_achievement_jumping)
		{
			Debuggin.ToScreen($"Entered '{GameObject}'", 10.0f);
		}

		if (!IsFullyValid(linkedVolume.trackingPlayer))
			return;

		Assert.True(linkedVolume.trackingPlayer == osCharacter, "How the fuck are we tracking a different character?");

		if (debug_achievement_jumping)
		{
			Debuggin.ToScreen($"Unlocking achievement '{jumpAchievementType}' '{GameObject}'", 10.0f);			
		}

		if (jumpAchievementType == JumpAchievementType.Plank)
		{
			Sandbox.Services.Achievements.Unlock(Achievement.JUMP_PLANK);
		}
		else if (jumpAchievementType == JumpAchievementType.Hangar)
		{
			Sandbox.Services.Achievements.Unlock(Achievement.JUMP_HANGAR);
		}
		else
		{
			Log.Warning($"You didn't set a achievement type on '{GameObject}'");
		}
	}

	public void OnTriggerExit(Collider other)
	{
		var osCharacter = other.Components.Get<OSCharacter>();
		if (osCharacter == null)
			return;

		if (osCharacter.IsProxy)
			return;

		if (debug_achievement_jumping)
		{
			Debuggin.ToScreen($"Exited '{GameObject}'", 10.0f);
		}

		trackingPlayer = osCharacter;
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
