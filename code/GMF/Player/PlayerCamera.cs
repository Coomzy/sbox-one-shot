
using System;

[Group("GMF")]
public class PlayerCamera : Component
{
	public static PlayerCamera instance { get; private set; }
	public static CameraComponent cam => instance != null ? instance.camera : null;

	[Group("Setup"), Property] CameraComponent camera { get; set; }

	[Group("Runtime"), Property] public float? targetFOV { get; set; } = null;
	[Group("Runtime"), Property] public float fovTransitionRate { get; set; } = 400.0f;

	[ConVar] public static bool debug_camera { get; set; }

	protected override void OnAwake()
	{
		instance = this;
	}

	protected override void OnUpdate()
	{
		// TODO: This could be better, if multiple things want to effect the FOV there isn't a good way to do that currently
		var fovTarget = Preferences.FieldOfView;
		var fovTargetRate = fovTransitionRate;

		var equippedItem = PlayerInfo.local?.character?.equippedItem;
		if (IsFullyValid(equippedItem))
		{
			if (equippedItem.IsRequestingFOVZoom(out var newFovTarget, out var newFovTargetRate))
			{
				fovTarget = newFovTarget;
				fovTargetRate = newFovTargetRate;
			}
		}
		cam.FieldOfView = MathY.MoveTowards(cam.FieldOfView, fovTarget, Time.Delta * fovTargetRate);

		if (debug_camera)
		{
			Debuggin.ToScreen($"Preferences.FieldOfView: {Preferences.FieldOfView}");
			Debuggin.ToScreen($"cam.FieldOfView: {cam.FieldOfView}");
			Debuggin.ToScreen($"fovTarget: {fovTarget}");
			Debuggin.ToScreen($"fovTargetRate: {fovTargetRate}");
		}
	}

	public Vector3 GetPointInFront(float distance)
	{
		Vector3 pos = WorldPosition;

		if (cam == null)
			return pos;

		pos += WorldRotation.Forward * distance;

		return pos;
	}

	public static float GetScaledSensitivity()
	{
		var scaledSensitivity = CalculateZoomedSensitivity(Preferences.Sensitivity, Preferences.FieldOfView, cam.FieldOfView);
		var sensitivityDelta = Preferences.Sensitivity - scaledSensitivity;
		sensitivityDelta = scaledSensitivity / Preferences.Sensitivity;
		return sensitivityDelta;
	}

	public static float CalculateZoomedSensitivity(float baseSensitivity, float baseFOV, float zoomedFOV)
	{
		float baseFOVRadians = DegreesToRadians(baseFOV) / 2f;
		float zoomedFOVRadians = DegreesToRadians(zoomedFOV) / 2f;

		return baseSensitivity * MathF.Tan(baseFOVRadians) / MathF.Tan(zoomedFOVRadians);
	}

	// TODO: Make this a property in MathY
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
