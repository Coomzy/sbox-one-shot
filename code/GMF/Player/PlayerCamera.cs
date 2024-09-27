
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

	public static float GetScaledSensitivity()
	{
		//return 1.0f;
		var scaledSensitivity = CalculateZoomedSensitivity(Preferences.Sensitivity, Preferences.FieldOfView, cam.FieldOfView);
		var sensitivityDelta = Preferences.Sensitivity - scaledSensitivity;
		sensitivityDelta = scaledSensitivity / Preferences.Sensitivity;
		/*Debuggin.ToScreen($"Preferences.Sensitivity: {Preferences.Sensitivity}");
		Debuggin.ToScreen($"scaledSensitivity: {scaledSensitivity}");
		Debuggin.ToScreen($"sensitivityDelta: {sensitivityDelta}");*/
		//sensitivityDelta = 1.0f - sensitivityDelta;
		return sensitivityDelta;
	}

	public static float CalculateZoomedSensitivity(float baseSensitivity, float baseFOV, float zoomedFOV)
	{
		float baseFOVRadians = DegreesToRadians(baseFOV) / 2f;
		float zoomedFOVRadians = DegreesToRadians(zoomedFOV) / 2f;

		return baseSensitivity * MathF.Tan(baseFOVRadians) / MathF.Tan(zoomedFOVRadians);
	}

	public static float DegreesToRadians(float degrees)
	{
		return degrees * (float)(Math.PI / 180.0);
	}


	public override void Reset()
	{
		base.Reset();

		camera = Components.Get<CameraComponent>();
	}
}
