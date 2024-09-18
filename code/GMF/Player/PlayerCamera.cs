
using System;

[Group("GMF")]
public class PlayerCamera : Component
{
	public static PlayerCamera instance { get; private set; }
	public static CameraComponent cam => instance != null ? instance.camera : null;

	[Group("Setup"), Property] CameraComponent camera { get; set; }

	protected override void OnAwake()
	{
		instance = this;
	}

	protected override void OnUpdate()
	{
		
	}

	public Vector3 GetPointInFront(float distance)
	{
		Vector3 pos = Transform.Position;

		if (cam == null)
			return pos;

		pos += Transform.Rotation.Forward * distance;

		return pos;
	}

	public override void Reset()
	{
		base.Reset();

		camera = Components.Get<CameraComponent>();
	}
}
