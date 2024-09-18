
[Group("GMF")]
public class Spectator : Component
{
	public static Spectator instance;

	[Property] public float flyMoveSpeed { get; set; } = 190.0f;
	[Property] public float sprintMoveSpeed { get; set; } = 320.0f;

	protected void Awake()
	{
		if (instance != null)
		{
			instance.Destroy();
		}

		instance = this;
	}

	protected override void OnUpdate()
	{
		if (!IsUsingSpectatorCamera()) 
			return;

		MouseInput();
	}

	protected override void OnFixedUpdate()
	{
		if (!IsUsingSpectatorCamera())
			return;

		MovementInput();
	}

	void MouseInput()
	{
		var e = Transform.Rotation.Angles();
		e += Input.AnalogLook;
		e.pitch = e.pitch.Clamp(-90, 90);
		e.roll = 0.0f;
		Transform.Rotation = e.ToRotation();
	}

	float currentMoveSpeed
	{
		get
		{
			if (Input.Down("run")) return sprintMoveSpeed;

			return sprintMoveSpeed;
		}
	}

	void MovementInput()
	{
		Vector3 halfGravity = Scene.PhysicsWorld.Gravity * Time.Delta * 0.5f;

		var wishVelocity = Input.AnalogMove;

		float verticalInput = 0.0f;
		if (Input.Down("jump"))
		{
			verticalInput += 1.0f;
		}
		if (Input.Down("duck"))
		{
			verticalInput -= 1.0f;
		}
		wishVelocity.z = verticalInput;

		if ( !wishVelocity.IsNearlyZero() )
		{
			wishVelocity = new Angles( 0, Transform.Rotation.Angles().yaw, 0 ).ToRotation() * wishVelocity;
			wishVelocity = wishVelocity.ClampLength( 1 );
			wishVelocity *= currentMoveSpeed;
		}

		Transform.Position += wishVelocity * Time.Delta;
	}

	void UpdateCamera()
	{
		if (PlayerCamera.cam is null ) return;

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

	public bool IsUsingSpectatorCamera()
	{
		if (PlayerInfo.local?.character != null )
			return false;

		return true;
	}
}
