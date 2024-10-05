
public class JumpPad : Component, Component.ITriggerListener
{
	[Property] public BoxCollider boxCollider { get; set; }

	protected override void OnAwake()
	{
		GameObject.NetworkMode = NetworkMode.Object;
		SetupBoxCollider();
		//boxCollider.OnTriggerEnter += OnTriggerEnter;
	}

	protected override void OnDestroy()
	{
		//boxCollider.OnTriggerEnter -= OnTriggerEnter;
		base.OnDestroy();
	}

	public void OnTriggerEnter(Collider other)
	{
		var osPawn = other.Components.Get<OSCharacter>();
		if (osPawn == null)
			return;

		if (osPawn.IsProxy)
			return;

		osPawn.movement.Launch(Vector3.Up * 1000.0f);
		PlayJumpPadSound(WorldPosition);
	}

	[Broadcast]
	static void PlayJumpPadSound(Vector3 pos)
	{
		Sound.Play("world.jumppad", pos);
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
