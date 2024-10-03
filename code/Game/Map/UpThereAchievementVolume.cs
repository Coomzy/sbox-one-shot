
public class UpThereAchievementVolume : Component, Component.ITriggerListener
{
	[Property] public BoxCollider boxCollider { get; set; }

	public void OnTriggerEnter(Collider other)
	{
		var osCharacter = other.Components.Get<OSCharacter>();
		if (osCharacter == null)
			return;

		if (osCharacter.IsProxy)
			return;

		Sandbox.Services.Achievements.Unlock(Achievement.UP_THERE);
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
