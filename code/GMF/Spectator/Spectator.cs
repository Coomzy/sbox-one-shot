
using System;
using System.Numerics;
using System.Reflection.PortableExecutable;

// TODO: Split this out so there can be a Flatscreen and VR version but the code can still reference Spectator.instance
[Group("GMF")]
public class Spectator : Component
{
	public static Spectator instance;

	[Property] public float flyMoveSpeed { get; set; } = 500.0f;
	[Property] public float sprintMoveSpeed { get; set; } = 750.0f;

	Vector3 lastVelocity = Vector3.Zero;

	float currentMoveSpeed
	{
		get
		{
			if (Input.Down("run")) return sprintMoveSpeed;

			return flyMoveSpeed;
		}
	}

	public static bool TryCreate()
	{
		GetStartingPosAndRot(out var spawnPos, out var spawnRot);
		return TryCreate(spawnPos, spawnRot);
	}

	public static bool TryCreate(Vector3 spawnPos, Rotation spawnRot)
	{
		if (IsFullyValid(instance))
		{
			return false;
		}
		var spectatorPrefab = WorldInfo.instance.spectatorPrefab;
		//var spectatorPrefab = Game.IsRunningInVR ? GMFSettings.instance.spectatorVRPrefab : GMFSettings.instance.spectatorPrefab;
		var spectatorInst = spectatorPrefab.Clone(spawnPos, spawnRot).BreakPrefab().Components.Get<Spectator>();
		spectatorInst.GameObject.Name = $"Spectator Pawn";
		return true;
	}

	protected override void OnAwake()
	{
		instance = this;
	}

	protected override void OnDestroy()
	{
		instance = null;
		base.OnDestroy();
	}

	protected override void OnUpdate()
	{
		if (!IsUsingSpectatorCamera())
		{
			if (IsFullyValid(PlayerCamera.cam))
			{
				Transform.Position = PlayerCamera.cam.Transform.Position;
				Transform.Rotation = PlayerCamera.cam.Transform.Rotation;
			}
			return;
		}

		if (!AllowedMove())
			return;

		MouseInput();
		MovementInput();
	}

	protected override void OnFixedUpdate()
	{
		if (!IsUsingSpectatorCamera())
			return;
	}

	void MouseInput()
	{
		var e = Transform.Rotation.Angles();
		e += Input.AnalogLook;
		e.pitch = e.pitch.Clamp(-90, 90);
		e.roll = 0.0f;
		Transform.Rotation = e.ToRotation();
	}

	void MovementInput()
	{
		var wishVelocity = Input.AnalogMove;

		wishVelocity *= Transform.Rotation;

		if (Input.Down("jump"))
		{
			wishVelocity.z = 1.0f;
		}
		else if (Input.Down("duck"))
		{
			wishVelocity.z = -1.0f;
		}
		wishVelocity = wishVelocity.ClampLength(1);

		/*if (!wishVelocity.IsNearlyZero())
		{
			wishVelocity = new Angles( 0, Transform.Rotation.Angles().yaw, 0 ).ToRotation() * wishVelocity;
			wishVelocity = wishVelocity.ClampLength( 1 );
			wishVelocity *= currentMoveSpeed;
		}*/
		wishVelocity *= currentMoveSpeed;

		float moveToRate = wishVelocity.IsNearZeroLength ? 2500.0f : 4500.0f;
		var newVelocity = MathY.MoveTowards(lastVelocity, wishVelocity, Time.Delta * moveToRate);
		lastVelocity = newVelocity;
		Transform.Position += lastVelocity * Time.Delta;
	}

	void UpdateCamera()
	{
		if (!IsFullyValid(PlayerCamera.cam))
			return;

		PlayerCamera.cam.Transform.Position = Transform.Position;
		PlayerCamera.cam.Transform.Rotation = Transform.Rotation;
		PlayerCamera.cam.FieldOfView = Preferences.FieldOfView;
	}

	protected override void OnPreRender()
	{
		if (!IsUsingSpectatorCamera())
			return;

		UpdateCamera();
	}

	public virtual bool AllowedMove()
	{
		if (!IsFullyValid(GameMode.instance))
			return false;

		if (GameMode.instance.modeState != ModeState.ActiveRound ||
			GameMode.instance.modeState != ModeState.WaitingForPlayers)
			return false;

		if (IsFullyValid(PlayerInfo.local?.character))
			return false;

		if (PlayerInfo.local.deadTime < 3.0f)
			return false;

		return true;
	}

	public virtual bool IsUsingSpectatorCamera()
	{
		if (!IsFullyValid(PlayerInfo.local?.character))
			return true;

		return false;
	}

	public static void Teleport(Vector3 pos, Quaternion? rot = null)
	{
		if (!IsFullyValid(instance))
			return;

		instance.Transform.Position = pos;
		if (rot.HasValue) instance.Transform.Rotation = rot.Value;

		if (!IsFullyValid(PlayerCamera.cam))
			return;

		PlayerCamera.cam.Transform.Position = instance.Transform.Position;
		PlayerCamera.cam.Transform.Rotation = instance.Transform.Rotation;
	}

	public static void TeleportToStartingPoint()
	{
		GetStartingPosAndRot(out var spawnPos, out var spawnRot);
		Teleport(spawnPos, spawnRot);
	}

	public static void GetStartingPosAndRot(out Vector3 spawnPos, out Rotation spawnRot)
	{
		spawnPos = Vector3.Zero;
		spawnRot = Rotation.Identity;

		var spectateStartSpot = Game.ActiveScene.Components.GetInDescendantsOrSelf<SpectateViewpoint>();
		if (spectateStartSpot != null)
		{
			spawnPos = spectateStartSpot.Transform.Position;
			spawnRot = spectateStartSpot.Transform.Rotation;
			return;
		}

		var spawnPoints = Game.ActiveScene.GetAllComponents<SpawnPoint>().ToArray();
		if (spawnPoints.Length > 0)
		{
			spawnPos = spawnPoints[0].Transform.Position + (Vector3.Up * 50.0f);
			spawnRot = spawnPoints[0].Transform.Rotation;
		}
	}
}
