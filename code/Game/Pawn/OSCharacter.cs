using Sandbox;
using Sandbox.Citizen;
using System;
using System.Numerics;
using System.Threading.Channels;

[Group("OS")]
public class OSCharacter : Character, Component.INetworkSpawn
{
	[Group("Setup"), Property] public GameObject eyeHolder { get; set; }
	[Group("Setup"), Property] public GameObject firstPersonArmsHolder { get; set; }
	[Group("Setup"), Property] public GameObject gunHolder { get; set; }

	[Group("Setup"), Property] public GameObject harpoonGunPrefab { get; set; }	

	protected override void OnAwake()
	{
		movement.config = movement.config ?? PlayerSettings.instance.characterMovementConfig;

		eyeAngles = Transform.Rotation.Angles().WithRoll(0).WithPitch(0);
	}

	protected override void OnStart()
	{
		base.OnStart();

		//Log.Info($"OSPawn::OnStart() '{GameObject.Name}', IsOwner: {GameObject.Network.IsOwner},  IsCreator: {GameObject.Network.IsCreator}, OwnerConnection: {GameObject.Network.OwnerConnection}, IsHost: {Networking.IsHost}");
		if (IsProxy)
		{
			return;
		}

		var playerBodyInst = playerBodyPrefab.Clone();
		body = playerBodyInst.Components.Get<CharacterBody>();
		body.owner = this;
		playerBodyInst.NetworkSpawn(GameObject.Network.OwnerConnection);
		body.owner = this;
		//playerBody = playerBodyInst.Components.Get<PlayerBody>();

		var harpoonGunInst = harpoonGunPrefab.Clone();
		equippedItem = harpoonGunInst.Components.Get<Equipment>();
		equippedItem.instigator = this;
		//Log.Info($"OSPawn::OnNetworkSpawn() '{GameObject.Name}' connection: {connection}, IsOwner: {GameObject.Network.IsOwner},  IsCreator: {GameObject.Network.IsCreator}, OwnerConnection: {GameObject.Network.OwnerConnection}, IsHost: {Networking.IsHost}");
		harpoonGunInst.NetworkSpawn(GameObject.Network.OwnerConnection);
		equippedItem.instigator = this;
		//equippedItem = harpoonGunInst.Components.Get<Equipment>();

		//DelayedAttach();

		harpoonGunInst.SetParent(gunHolder);
		harpoonGunInst.Transform.LocalPosition = Vector3.Zero;
		harpoonGunInst.Transform.LocalRotation = Quaternion.Identity;
	}

	public async virtual void OnNetworkSpawn(Connection connection)
	{
		//await Task.Frame();
		//Log.Info($"OSPawn::OnNetworkSpawn() '{GameObject.Name}' connection: {connection}, IsOwner: {GameObject.Network.IsOwner},  IsCreator: {GameObject.Network.IsCreator}, OwnerConnection: {GameObject.Network.OwnerConnection}, IsHost: {Networking.IsHost}");


		if (IsProxy)
		{
			return;
		}
		return;
		//await Task.Frame();

		var playerBodyInst = playerBodyPrefab.Clone();
		body = playerBodyInst.Components.Get<CharacterBody>();
		body.owner = this;
		playerBodyInst.NetworkSpawn(connection);
		body.owner = this;
		//playerBody = playerBodyInst.Components.Get<PlayerBody>();

		var harpoonGunInst = harpoonGunPrefab.Clone();
		equippedItem = harpoonGunInst.Components.Get<Equipment>();
		equippedItem.instigator = this;
		//Log.Info($"OSPawn::OnNetworkSpawn() '{GameObject.Name}' connection: {connection}, IsOwner: {GameObject.Network.IsOwner},  IsCreator: {GameObject.Network.IsCreator}, OwnerConnection: {GameObject.Network.OwnerConnection}, IsHost: {Networking.IsHost}");
		harpoonGunInst.NetworkSpawn(connection);
		equippedItem.instigator = this;
		//equippedItem = harpoonGunInst.Components.Get<Equipment>();

		//DelayedAttach();

		await Task.Frame();
		harpoonGunInst.SetParent(gunHolder);
		harpoonGunInst.Transform.LocalPosition = Vector3.Zero;
		harpoonGunInst.Transform.LocalRotation = Quaternion.Identity;

		/*var harpoonGunInst = harpoonGunPrefab.Clone();
		equippedItem = harpoonGunInst.Components.Get<Equipment>();
		equippedItem.owner = this;
		//Log.Info($"OSPawn::OnNetworkSpawn() '{GameObject.Name}' connection: {connection}, IsOwner: {GameObject.Network.IsOwner},  IsCreator: {GameObject.Network.IsCreator}, OwnerConnection: {GameObject.Network.OwnerConnection}, IsHost: {Networking.IsHost}");
		harpoonGunInst.NetworkSpawn(connection);

		//DelayedAttach();

		harpoonGunInst.SetParent(gunHolder);
		harpoonGunInst.Transform.LocalPosition = Vector3.Zero;
		harpoonGunInst.Transform.LocalRotation = Quaternion.Identity;

		var camera = Scene.GetAllComponents<CameraComponent>().Where(x => x.IsMainCamera).FirstOrDefault();
		//camera.GameObject.SetParent(eyeHolder, false);
		//camera.Transform.LocalPosition = Vector3.Zero;
		//camera.Transform.LocalRotation = Rotation.Identity;
		//firstPersonArmsHolder.SetParent(eyeHolder, false);
		//firstPersonArmsHolder.Transform.LocalPosition = Vector3.Zero;
		//firstPersonArmsHolder.Transform.LocalRotation = Rotation.Identity;*/
	}

	public async void DelayedAttach()
	{
		await Task.Frame();

		equippedItem.GameObject.SetParent(gunHolder);
		equippedItem.GameObject.Transform.LocalPosition = Vector3.Zero;
		equippedItem.GameObject.Transform.LocalRotation = Quaternion.Identity;

	}

	[Property] public GameObject spherePrefab { get; set; } 
	protected override void OnUpdate()
	{
		base.OnUpdate();

		//Log.Info($"OSPawn::OnNetworkSpawn() '{GameObject.Name}' IsOwner: {GameObject.Network.IsOwner},  IsCreator: {GameObject.Network.IsCreator}, OwnerConnection: {GameObject.Network.OwnerConnection}, IsHost: {Networking.IsHost}");
		if (IsProxy)
		{
			return;
		}

		MouseInput();
		Transform.Rotation = new Angles(0, eyeAngles.yaw, 0);
		FireInput();

		UpdateCamera();

		if (Input.Pressed("reload"))
		{
			var spawnPoint = PlayerCamera.instance.GetPointInFront(100.0f);
			var inst = spherePrefab.Clone(spawnPoint);
			inst.NetworkSpawn(GameObject.Network.OwnerConnection);
		}
	}

	void MouseInput()
	{
		var e = eyeAngles;
		e += Input.AnalogLook;
		e.pitch = e.pitch.Clamp( -90, 90 );
		e.roll = 0.0f;
		eyeAngles = e;
	}

	void FireInput()
	{
		if (Input.Pressed("attack1"))
		{
			equippedItem.FireStart();
		}

		if (Input.Released("attack1"))
		{
			equippedItem.FireEnd();
		}

		if (Input.Pressed("attack2"))
		{
			equippedItem.FireAltStart();
		}

		if (Input.Released("attack2"))
		{
			equippedItem.FireAltEnd();
		}
	}

	float jumpShrinkAmount = 0.0f;
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

		if (Input.Down("attack2"))
		{
			fov *= 0.6f;
		}

		PlayerCamera.cam.FieldOfView = MathY.MoveTowards(PlayerCamera.cam.FieldOfView, fov, Time.Delta * 350.0f);

		firstPersonArmsHolder.Transform.Position = PlayerCamera.cam.Transform.Position;
		firstPersonArmsHolder.Transform.Rotation = PlayerCamera.cam.Transform.Rotation;
	}

	protected override void OnDestroy()
	{
		Log.Info($"OnDestroy() body: {body}, IsProxy: {IsProxy}");
		if (IsProxy)
		{
			base.OnDestroy();
			return;
		}

		if (body != null && body.IsValid)
		{
			body.Destroy();
		}

		base.OnDestroy();
	}
}
