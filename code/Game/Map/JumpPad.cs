
public class JumpPad : Component, Component.ITriggerListener
{
	[Property] public BoxCollider boxCollider { get; set; }

	protected override void OnAwake()
	{
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
		Log.Info($"JumpPad::OnTriggerEnter() other: {other?.GameObject?.Name}");
		var osPawn = other.Components.Get<OSCharacter>();
		if (osPawn == null)
			return;

		osPawn.movement.Launch(Vector3.Up * 1000.0f);
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
