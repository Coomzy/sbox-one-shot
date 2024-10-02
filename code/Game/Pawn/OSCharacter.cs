using badandbest.Sprays;
using Sandbox;
using Sandbox.Citizen;
using System;
using System.Numerics;
using System.Threading.Channels;

[Group("OS")]
public class OSCharacter : Character
{
	[Group("Setup"), Property] public GameObject eyeHolder { get; set; }
	[Group("Setup"), Property] public GameObject firstPersonArmsHolder { get; set; }
	[Group("Setup"), Property] public GameObject gunHolder { get; set; }

	// TODO: Make some kind of loadout system?
	[Group("Setup"), Property] public GameObject harpoonGunPrefab { get; set; }

	[Group("Runtime"), Property] float jumpShrinkAmount { get; set; } = 0.0f;

	protected override void OnAwake()
	{
		base.OnAwake();

		movement.config = movement.config ?? GameSettings.instance.characterMovementConfig;

		eyeAngles = Transform.Rotation.Angles().WithRoll(0).WithPitch(0);
	}

	protected override void OnStart()
	{
		base.OnStart();

		//Log.Info($"Network.Owner: {Network.Owner}, Network.OwnerId: {Network.OwnerId}, PlayerInfo: {PlayerInfo.connectionToPlayerInfos[Network.OwnerId]}");

		//Log.Info($"OSPawn::OnStart() '{GameObject.Name}', IsOwner: {GameObject.Network.IsOwner},  IsCreator: {GameObject.Network.IsCreator}, OwnerConnection: {GameObject.Network.Owner}, IsHost: {Networking.IsHost}");
		if (IsProxy)
		{
			return;
		}

		// TODO: Move this into Character
		var playerBodyInst = playerBodyPrefab.Clone();
		body = playerBodyInst.Components.Get<CharacterBody>();
		body.owner = this;
		playerBodyInst.NetworkSpawn(GameObject.Network.Owner);
		//playerBody = playerBodyInst.Components.Get<PlayerBody>();

		var harpoonGunInst = harpoonGunPrefab.Clone();
		//harpoonGunInst.NetworkInterpolation = false;
		equippedItem = harpoonGunInst.Components.Get<Equipment>();
		equippedItem.instigator = this;
		//Log.Info($"OSPawn::OnNetworkSpawn() '{GameObject.Name}' connection: {connection}, IsOwner: {GameObject.Network.IsOwner},  IsCreator: {GameObject.Network.IsCreator}, OwnerConnection: {GameObject.Network.Owner}, IsHost: {Networking.IsHost}");
		harpoonGunInst.NetworkSpawn(GameObject.Network.Owner);
		//equippedItem = harpoonGunInst.Components.Get<Equipment>();

		harpoonGunInst.SetParent(gunHolder);
		harpoonGunInst.Transform.LocalPosition = Vector3.Zero;
		harpoonGunInst.Transform.LocalRotation = Quaternion.Identity;

		var crosshairBuilder = Scene.Components.GetInDescendantsOrSelf<CrosshairBuilder>(true);
		if (crosshairBuilder != null)
		{
			crosshairBuilder.Enabled = true;
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		//Log.Info($"OSPawn::OnNetworkSpawn() '{GameObject.Name}' IsOwner: {GameObject.Network.IsOwner},  IsCreator: {GameObject.Network.IsCreator}, OwnerConnection: {GameObject.Network.Owner}, IsHost: {Networking.IsHost}");
		if (IsProxy)
		{
			return;
		}

		MouseInput();
		Transform.Rotation = new Angles(0, eyeAngles.yaw, 0);
		FireInput();

		UpdateCamera();

		if (Input.Pressed("Spray"))
		{
			Spray.Place();
		}
	}

	void MouseInput()
	{
		var e = eyeAngles;
		e += Input.AnalogLook / PlayerCamera.GetScaledSensitivity();
		e.pitch = e.pitch.Clamp( -90, 90 );
		e.roll = 0.0f;
		eyeAngles = e;
	}

	// TODO: Move to base
	void FireInput()
	{
		if (GameMode.instance.modeState == ModeState.ReadyPhase)
			return;

		if (Input.Pressed(Inputs.attack1))
		{
			equippedItem.FireStart();
		}

		if (Input.Released(Inputs.attack1))
		{
			equippedItem.FireEnd();
		}

		if (Input.Pressed(Inputs.attack2))
		{
			equippedItem.FireAltStart();
		}

		if (Input.Released(Inputs.attack2))
		{
			equippedItem.FireAltEnd();
		}
	}

	void UpdateCamera()
	{
		var camera = PlayerCamera.cam;
		if (camera == null || !camera.IsValid) return;

		var targetEyeHeight = movement.config.eyeHeight;

		if (movement.isSliding)
		{
			targetEyeHeight = movement.config.eyeHeightSliding;
		}
		else if (movement.isCrouching)
		{
			targetEyeHeight = movement.config.eyeHeightCrouching;
		}


		var previousHeight = movement.eyeHeight;
		var heightTarget = movement.isCrouching ? movement.config.crouchHeight : movement.config.characterHeight;
		//characterController.Height = heightTarget;
		var heightDelta = heightTarget - previousHeight;

		var previousEyeHeight = movement.eyeHeight;
		movement.eyeHeight = movement.eyeHeight.LerpTo(targetEyeHeight, RealTime.Delta * 10.0f);
		float eyeHeightDelta = previousEyeHeight - movement.eyeHeight;

		if (!movement.characterController.IsOnGround && heightDelta != 0.0f)
		{
			if (movement.isCrouching)
			{
				jumpShrinkAmount += heightDelta;
				//Log.Info($"heightDelta: {heightDelta}");
				//characterController.MoveTo(Transform.Position += Vector3.Up * heightDelta, false);
				//Transform.ClearInterpolation();
				//characterMovement.eyeHeight -= eyeHeightDelta;
			}
			else
			{
				jumpShrinkAmount -= heightDelta;
				//characterController.MoveTo(Transform.Position -= Vector3.Up * heightDelta, false);
				//Transform.ClearInterpolation();
				//characterMovement.eyeHeight += eyeHeightDelta;
			}
		}

		var targetCameraPos = Transform.Position + new Vector3(0, 0, movement.eyeHeight);

		// smooth view z, so when going up and down stairs or ducking, it's smooth af
		if (movement.lastUngrounded > 0.2f)
		{
			targetCameraPos.z = camera.Transform.Position.z.LerpTo(targetCameraPos.z, RealTime.Delta * 25.0f);
		}

		if (eyeHolder != null && eyeHolder.IsValid)
		{
			eyeHolder.Transform.Position = targetCameraPos;
			eyeHolder.Transform.Rotation = eyeAngles;
		}

		camera.Transform.Position = targetCameraPos;
		camera.Transform.Rotation = eyeAngles;

		var fov = Preferences.FieldOfView;

		// TODO: Move to equipment
		if (Input.Down(Inputs.attack2))
		{
			// AND DON'T HARDCODE
			fov *= 0.6f;
		}

		PlayerCamera.cam.FieldOfView = MathY.MoveTowards(PlayerCamera.cam.FieldOfView, fov, Time.Delta * 350.0f);

		firstPersonArmsHolder.Transform.Position = PlayerCamera.cam.Transform.Position;
		firstPersonArmsHolder.Transform.Rotation = PlayerCamera.cam.Transform.Rotation;
	}

	protected override void OnDestroy()
	{
		if (IsProxy)
		{
			base.OnDestroy();
			return;
		}

		var crosshairBuilder = Scene.Components.GetInDescendantsOrSelf<CrosshairBuilder>(true);
		if (crosshairBuilder != null)
		{
			crosshairBuilder.Enabled = false;
		}

		base.OnDestroy();
	}
}
