
using Sandbox;
using Sandbox.Physics;
using Sandbox.Services;
using Sandbox.Utility;
using System;
using System.Diagnostics;

[Group("OS")]
public class HarpoonSpearFlare : Component
{
	[Property] public ModelRenderer flareRenderer { get; set; }

	protected override void OnUpdate()
	{
		if (!IsFullyValid(PlayerCamera.cam))
			return;

		var directionToCamera = PlayerCamera.cam.WorldPosition - WorldPosition;
		WorldRotation = Rotation.LookAt(directionToCamera);

		flareRenderer.LocalRotation = flareRenderer.LocalRotation.RotateAroundAxis(Vector3.Up, Time.Delta * 250.0f);

		var distanceFactor = MathY.InverseLerp(0.0f, 3000.0f, directionToCamera.Length);
		var distanceEased = EasingY.InSine(distanceFactor);
		var scaledSize = MathY.Lerp(0.35f, 5.0f, distanceEased);
		Debuggin.ToScreen($"directionToCamera.Length: {directionToCamera.Length}");
		Debuggin.ToScreen($"distanceFactor: {distanceFactor}");
		Debuggin.ToScreen($"scaledSize: {scaledSize}");
		
		WorldScale = Vector3.One * scaledSize;
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();

		flareRenderer.Enabled = true;
	}

	protected override void OnDisabled()
	{
		flareRenderer.Enabled = false;
		base.OnDisabled();
	}
}
