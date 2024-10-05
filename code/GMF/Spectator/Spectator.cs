
using System;
using System.Numerics;
using System.Reflection.PortableExecutable;

public enum SpectateMode
{
	None,
	Viewpoint,
	CharacterDeath,
	ThirdPerson,
	FreeCam
}

// TODO: Split this out so there can be a Flatscreen and VR version but the code can still reference Spectator.instance
[Group("GMF")]
public class Spectator : Component
{
	public static Spectator instance;

	[Group("Setup"), Property] public CameraBoom boom { get; set; }

	[Group("Config"), Property] public float flyMoveSpeed { get; set; } = 500.0f;
	[Group("Config"), Property] public float sprintMoveSpeed { get; set; } = 750.0f;

	[Group("Runtime"), Property] public SpectateMode mode { get; private set; } = SpectateMode.Viewpoint;
	[Group("Runtime"), Property] public TimeSince stateTime { get; private set; }

	// Viewpoint
	[Group("Runtime - Viewpoint"), Property] public SpectateViewpoint viewpoint { get; set; }

	// Third Person
	[Group("Runtime - Third Person"), Property] public Rotation initalRotation { get; set; } = Rotation.Identity;
	[Group("Runtime - Third Person"), Property] public Angles inputAngles = new Angles();
	[Group("Runtime - Third Person"), Property] public float characterLookPitch { get; set; }
	[Group("Runtime - Third Person"), Property] public TimeSince lastInput { get; set; }
	[Group("Runtime - Third Person"), Property] public PlayerInfo spectateTarget { get; set; }

	// Free Cam
	[Group("Runtime"), Property] public Vector3 lastVelocity = Vector3.Zero;

	// Debug
	[ConVar] public static bool debug_spectator_mode { get; set; }
	[ConVar] public static bool debug_spectator_boom { get; set; }

	float currentMoveSpeed
	{
		get
		{
			if (Input.Down("run")) return sprintMoveSpeed;

			return flyMoveSpeed;
		}
	}

	protected override void OnAwake()
	{
		instance = this;
		stateTime = 0;
		mode = SpectateMode.Viewpoint;
	}

	protected override void OnStart()
	{
		viewpoint = viewpoint ?? WorldInfo.instance.spectateViewpoints[0];		
	}

	protected override void OnDestroy()
	{
		instance = null;
		base.OnDestroy();
	}

	protected override void OnUpdate()
	{
		UpdateMode();

		if (debug_spectator_mode)
		{
			Debuggin.ToScreen($"Spectator mode: {mode}");
		}
	}

	public virtual void SetMode(SpectateMode newMode)
	{
		if (debug_spectator_mode)
		{
			Debuggin.ToScreen($"Spectator mode: {mode}");
			Log.Info($"Spectator::SetMode() newMode: {newMode} mode: {mode}");
		}

		if (mode == newMode)
			return;

		switch (mode)
		{
			case SpectateMode.None:
				Exit_None();
				break;
			case SpectateMode.Viewpoint:
				Exit_Viewpoint();
				break;
			case SpectateMode.CharacterDeath:
				Exit_CharacterDeath();
				break;				
			case SpectateMode.ThirdPerson:
				Exit_ThirdPerson();
				break;
			case SpectateMode.FreeCam:
				Exit_FreeCam();
				break;
		}

		mode = newMode;
		stateTime = 0;

		switch (mode)
		{
			case SpectateMode.None:
				Enter_None();
				break;
			case SpectateMode.Viewpoint:
				Enter_Viewpoint();
				break; 
			case SpectateMode.CharacterDeath:
				Enter_CharacterDeath();
				break;
			case SpectateMode.ThirdPerson:
				Enter_ThirdPerson();
				break;
			case SpectateMode.FreeCam:
				Enter_FreeCam();
				break;
		}
	}

	protected virtual void Enter_None(){}
	protected virtual void Enter_Viewpoint(){}
	protected virtual void Enter_CharacterDeath(){}
	protected virtual void Enter_ThirdPerson()
	{
		NextSpectateTarget();
	}
	protected virtual void Enter_FreeCam(){}

	protected virtual void Exit_None(){}
	protected virtual void Exit_Viewpoint(){}
	protected virtual void Exit_CharacterDeath(){}
	protected virtual void Exit_ThirdPerson(){}
	protected virtual void Exit_FreeCam(){}

	protected virtual void UpdateMode()
	{
		switch (mode)
		{
			case SpectateMode.None:
				Update_None();
				return;
			case SpectateMode.Viewpoint:
				Update_Viewpoint();
				return; 
			case SpectateMode.CharacterDeath:
				Update_CharacterDeath();
				return;
			case SpectateMode.ThirdPerson:
				Update_ThirdPerson();
				return;
			case SpectateMode.FreeCam:
				Update_FreeCam();
				return;
		}
	}

	protected virtual void Update_None()
	{
		if (!IsFullyValid(PlayerCamera.cam))
			return;

		WorldPosition = PlayerCamera.cam.WorldPosition;
		WorldRotation = PlayerCamera.cam.WorldRotation;
	}

	protected virtual void Update_Viewpoint()
	{
		if (!IsFullyValid(viewpoint))
			return;

		WorldPosition = viewpoint.WorldPosition;
		WorldRotation = viewpoint.WorldRotation;

		if (!IsFullyValid(PlayerCamera.cam))
			return;

		PlayerCamera.cam.WorldPosition = WorldPosition;
		PlayerCamera.cam.WorldRotation = WorldRotation;
		PlayerCamera.cam.FieldOfView = Preferences.FieldOfView;
	}

	protected virtual void Update_CharacterDeath()
	{
		// TODO: Magic fucking number
		if (stateTime > 3.0f)
		{
			SetMode(SpectateMode.ThirdPerson);
		}
	}

	// TODO: Give this a look over when it's not 6am
	protected virtual void Update_ThirdPerson()
	{
		if (spectateTarget == null || (spectateTarget.isDead && !spectateTarget.isRecentlyDead))
		{
			NextSpectateTarget();
		}

		if (spectateTarget == null)
		{
			SetMode(SpectateMode.Viewpoint);
			return;
		}

		if (Input.Pressed(Inputs.attack2))
		{
			PrevSpectateTarget();
		}

		if (Input.Pressed(Inputs.attack1))
		{
			NextSpectateTarget();
		}

		var character = spectateTarget?.character;
		var characterBody = spectateTarget?.character?.body;

		if (!IsFullyValid(characterBody))
			return;

		boom.WorldPosition = characterBody.WorldPosition;
		boom.WorldRotation = characterBody.WorldRotation;

		var input = Input.AnalogLook.WithRoll(0);
		var inputRotation = input.ToRotation();

		bool hasInput = !input.IsNearlyZero();
		if (hasInput)
		{
			lastInput = 0;
		}

		var localAngles = new Angles();

		if (!hasInput && lastInput > 1.0f)
		{
			characterLookPitch = MathY.Lerp(characterLookPitch, character.eyeAngles.pitch, Time.Delta / 0.05f);
			inputAngles = Angles.Lerp(inputAngles, new Angles(), Time.Delta / 0.5f);
		}

		localAngles.pitch = characterLookPitch;

		inputAngles += input;
		localAngles += inputAngles;

		localAngles.pitch = localAngles.pitch.Clamp(-85, 85);

		boom.roller.LocalRotation = localAngles.ToRotation();
		
		var targetPos = boom.socket.WorldPosition;

		var capsule = Capsule.FromHeightAndRadius(5.0f, 5.0f);
		var trace = Scene.Trace.Capsule(capsule).FromTo(boom.roller.WorldPosition, targetPos).IgnoreGameObjectHierarchy(GameObject).WithoutTags("trigger", Tag.CHARACTER_BODY);
		var traceResult = trace.Run();

		if (traceResult.Hit)
		{
			targetPos = traceResult.HitPosition + (boom.socket.WorldRotation.Forward * 5.0f);
		}

		WorldPosition = targetPos;
		WorldRotation = boom.socket.WorldRotation;

		if (debug_spectator_boom)
		{
			Debuggin.draw.Sphere(boom.roller.WorldPosition, 10.0f, color: Color.White);
			Debuggin.draw.Line(boom.roller.WorldPosition, targetPos, color: Color.Yellow);
			Debuggin.draw.Sphere(targetPos, 10.0f, color: Color.Yellow);
			Debuggin.draw.Sphere(boom.socket.WorldPosition, 10.0f, color: Color.Green);
		}
	}

	public virtual void PrevSpectateTarget()
	{
		var currentIndex = PlayerInfo.allAlive.IndexOf(spectateTarget);
		currentIndex--;
		if (currentIndex < 0)
		{
			currentIndex = PlayerInfo.allAlive.Count - 1;
		}
		if (currentIndex == -1)
		{
			currentIndex = 0;
		}

		PlayerInfo newTarget = null;

		if (PlayerInfo.allAlive.ContainsIndex(currentIndex))
		{
			newTarget = PlayerInfo.allAlive[currentIndex];
		}

		if (newTarget == spectateTarget)
			return;

		SetSpectateTargetIndex(newTarget);
	}

	public virtual void NextSpectateTarget()
	{
		var currentIndex = PlayerInfo.allAlive.IndexOf(spectateTarget);
		currentIndex++;
		if (currentIndex >= PlayerInfo.allAlive.Count)
		{
			currentIndex = 0;
		}
		if (currentIndex == -1)
		{
			currentIndex = 0;
		}


		PlayerInfo newTarget = null;

		if (PlayerInfo.allAlive.ContainsIndex(currentIndex))
		{
			newTarget = PlayerInfo.allAlive[currentIndex];
		}

		if (newTarget == spectateTarget)
			return;

		SetSpectateTargetIndex(newTarget);
	}

	public virtual void SetSpectateTargetIndex(PlayerInfo target)
	{
		spectateTarget = target;
		inputAngles = new Angles();
		if (IsFullyValid(spectateTarget?.character))
		{
			characterLookPitch = spectateTarget.character.eyeAngles.pitch;
		}
	}

	protected virtual void Update_FreeCam()
	{
		if (!AllowedMove())
			return;

		MouseInput();
		MovementInput();
	}

	void MouseInput()
	{
		var e = WorldRotation.Angles();
		e += Input.AnalogLook;
		e.pitch = e.pitch.Clamp(-90, 90);
		e.roll = 0.0f;
		WorldRotation = e.ToRotation();
	}

	void MovementInput()
	{
		var wishVelocity = Input.AnalogMove;

		wishVelocity *= WorldRotation;

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
			wishVelocity = new Angles( 0, WorldRotation.Angles().yaw, 0 ).ToRotation() * wishVelocity;
			wishVelocity = wishVelocity.ClampLength( 1 );
			wishVelocity *= currentMoveSpeed;
		}*/
		wishVelocity *= currentMoveSpeed;

		float moveToRate = wishVelocity.IsNearZeroLength ? 2500.0f : 4500.0f;
		var newVelocity = MathY.MoveTowards(lastVelocity, wishVelocity, Time.Delta * moveToRate);
		lastVelocity = newVelocity;
		WorldPosition += lastVelocity * Time.Delta;
	}

	void UpdateCamera()
	{
		if (!IsFullyValid(PlayerCamera.cam))
			return;

		PlayerCamera.cam.WorldPosition = WorldPosition;
		PlayerCamera.cam.WorldRotation = WorldRotation;
		PlayerCamera.cam.FieldOfView = Preferences.FieldOfView;
	}

	protected override void OnPreRender()
	{
		if (mode == SpectateMode.None)
			return;

		if (!IsFullyValid(PlayerCamera.cam))
			return;

		PlayerCamera.cam.WorldPosition = WorldPosition;
		PlayerCamera.cam.WorldRotation = WorldRotation;
		PlayerCamera.cam.FieldOfView = Preferences.FieldOfView;
	}

	public virtual bool AllowedMove()
	{
		if (!IsFullyValid(GameMode.instance))
			return false;

		if (GameMode.instance.modeState != ModeState.ActiveRound &&
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

		instance.WorldPosition = pos;
		if (rot.HasValue) instance.WorldRotation = rot.Value;

		if (!IsFullyValid(PlayerCamera.cam))
			return;

		PlayerCamera.cam.WorldPosition = instance.WorldPosition;
		PlayerCamera.cam.WorldRotation = instance.WorldRotation;
	}
}
