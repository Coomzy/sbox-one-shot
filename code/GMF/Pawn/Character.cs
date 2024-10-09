using Sandbox;
using Sandbox.Citizen;
using Sandbox.Network;
using System.Net.Mail;
using System.Numerics;
using static Sandbox.ModelRenderer;

// TODO: I'm seriously considering moving more of this onto the body, that might make it harder to make characters share a body though
[Group("GMF")]
public class Character : Component, IGameModeEvents//, Component.INetworkSpawn
{
	public PlayerInfo owner => PlayerInfo.GetOwner(GameObject);
	public bool hasOwner => owner != null;

	//[Group("Runtime"), Order(100), Property, ReadOnly, HostSync] public PlayerInfo owner { get; set; }

	[Group("Setup"), Property] public CharacterController controller { get; set; }
	[Group("Setup"), Property] public CharacterMovement movement { get; set; }
	[Group("Setup"), Property] public CapsuleCollider capsuleCollider { get; set; }
	[Group("Setup"), Property] public GameObject playerBodyPrefab { get; set; }

	[Group("Setup"), Property] public GameObject eyeHolder { get; set; }
	[Group("Setup"), Property] public GameObject firstPersonArmsHolder { get; set; }
	[Group("Setup"), Property] public GameObject gunHolder { get; set; }

	[Group("Runtime"), Order(100), Property, ReadOnly, Sync, Change("OnRep_body")] public CharacterBody body { get; set; }
	[Group("Runtime"), Order(100), Property, ReadOnly, Sync, Change("OnRep_equippedItem")] public Equipment equippedItem { get; set; }
	[Group("Runtime"), Order(100), Property, ReadOnly, Sync] public bool isDead { get; set; }

	[Group("Runtime"), Order(100), Property, ReadOnly, Sync] public Angles eyeAngles { get; set; }
	[Group("Runtime"), Order(100), Property, ReadOnly] public bool keepCharacterBody { get; set; }
	[Group("Runtime"), Property] public float jumpShrinkAmount { get; private set; } = 0.0f;

	public virtual void OnRep_body(CharacterBody oldValue, CharacterBody newValue)
	{
		TryAttachEquippedItem();
	}

	public virtual void OnRep_equippedItem(Equipment oldValue, Equipment newValue)
	{
		TryAttachEquippedItem();
	}

	public virtual void TryAttachEquippedItem()
	{
		if (!IsFullyValid(body, equippedItem))
		{
			return;
		}

		equippedItem.equipmentProxy.AttachTo(body.thirdPersonEquipmentAttachPoint);
	}

	protected override void OnAwake()
	{
		eyeAngles = WorldRotation.Angles().WithRoll(0).WithPitch(0);
	}

	protected override void OnStart()
	{
		if (IsProxy)
		{
			GameObject.Tags.Add(Tag.CHARACTER_REMOTE);
			GameObject.Tags.Remove(Tag.CHARACTER);
			return;
		}

		GameObject.Tags.Add(Tag.CHARACTER);
		GameObject.Tags.Remove(Tag.CHARACTER_REMOTE);

		PlayerInfo.local.SetSpectateMode(SpectateMode.None);
		Spectator.Teleport(WorldPosition, WorldRotation);

		SpawnBody();

		SpawnLoadout();
		TryAttachEquippedItem();

		IUIEvents.Post(x => x.EnableCrosshair());
	}

	protected virtual void SpawnBody()
	{
		var playerBodyInst = playerBodyPrefab.Clone();
		body = playerBodyInst.Components.Get<CharacterBody>();
		body.owner = this;
		body.GameObject.Name = $"Body ({owner?.displayName})";
		playerBodyInst.NetworkSpawn(GameObject.Network.Owner);
	}

	// TODO: Some kind of inventory system?
	protected virtual void SpawnLoadout()
	{

	}

	protected Equipment SpawnEquipment(GameObject prefab)
	{
		var equipmentInst = prefab.Clone();
		//harpoonGunInst.NetworkInterpolation = false;
		var equipment = equipmentInst.Components.Get<Equipment>();
		equipment.instigator = this;
		equipmentInst.NetworkSpawn(GameObject.Network.Owner);

		equipmentInst.SetParent(gunHolder);
		equipmentInst.LocalPosition = Vector3.Zero;
		equipmentInst.LocalRotation = Quaternion.Identity;

		return equipment;
	}

	public virtual void OnPossess(PlayerInfo playerInfo)
	{

	}

	public virtual void Unpossess()
	{
		if (body != null)
		{
			//body.Network.DropOwnership();
		}
		//Network.DropOwnership();
	}
	protected override void OnUpdate()
	{
		if (isDead)
			return;

		if (IsProxy)
			return;

		MouseInput();
		WorldRotation = new Angles(0, eyeAngles.yaw, 0);
		FireInput();

		movement.Update();

		UpdateCamera();

		float killZ = IsFullyValid(WorldInfo.instance) ? WorldInfo.instance.killZ : -500.0f;
		if (GameObject.WorldPosition.z <= killZ)
		{
			FellOutOfWorld();
		}
	}

	protected virtual void FireInput()
	{
		if (!IsFullyValid(GameMode.instance, equippedItem))
			return;

		if (GameMode.instance.modeState == ModeState.ReadyPhase)
			return;

		if (Input.Pressed(Inputs.attack1))
		{
			equippedItem.FireStart();
		}
		else if (Input.Released(Inputs.attack1))
		{
			equippedItem.FireEnd();
		}

		if (Input.Pressed(Inputs.attack2))
		{
			equippedItem.FireAltStart();
		}
		else if (Input.Released(Inputs.attack2))
		{
			equippedItem.FireAltEnd();
		}
	}

	protected virtual void MouseInput()
	{
		var e = eyeAngles;
		e += Input.AnalogLook / PlayerCamera.GetScaledSensitivity();
		e.pitch = e.pitch.Clamp(-90, 90);
		e.roll = 0.0f;
		eyeAngles = e;
	}

	public virtual void UpdateCamera()
	{
		if (PlayerInfo.local.spectateMode != SpectateMode.None)
		{
			return;
		}

		var camera = PlayerCamera.cam;
		if (!IsFullyValid(camera))
			return;

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
				//characterController.MoveTo(WorldPosition += Vector3.Up * heightDelta, false);
				//Transform.ClearInterpolation();
				//characterMovement.eyeHeight -= eyeHeightDelta;
			}
			else
			{
				jumpShrinkAmount -= heightDelta;
				//characterController.MoveTo(WorldPosition -= Vector3.Up * heightDelta, false);
				//Transform.ClearInterpolation();
				//characterMovement.eyeHeight += eyeHeightDelta;
			}
		}

		var targetCameraPos = WorldPosition + new Vector3(0, 0, movement.eyeHeight);

		// smooth view z, so when going up and down stairs or ducking, it's smooth af
		if (movement.lastUngrounded > 0.2f)
		{
			targetCameraPos.z = camera.WorldPosition.z.LerpTo(targetCameraPos.z, RealTime.Delta * 25.0f);
		}

		if (eyeHolder != null && eyeHolder.IsValid)
		{
			eyeHolder.WorldPosition = targetCameraPos;
			eyeHolder.WorldRotation = eyeAngles;
		}

		camera.WorldPosition = targetCameraPos;
		camera.WorldRotation = eyeAngles;

		firstPersonArmsHolder.WorldPosition = PlayerCamera.cam.WorldPosition;
		firstPersonArmsHolder.WorldRotation = PlayerCamera.cam.WorldRotation;
	}

	public virtual void TakeDamage(DamageInfo damageInfo)
	{
		if (!IsFullyValid(this))
			return;

		if (IsProxy)
		{
			return;
		}
		Die(damageInfo);
	}

	public virtual void Die(DamageInfo damageInfo)
	{
		if (IsProxy)
			return;

		if (isDead)
			return;

		isDead = true;
		keepCharacterBody = true;

		if (IsFullyValid(equippedItem))
		{
			equippedItem.Drop(Vector3.Zero);
		}
		body.Die(damageInfo);
		owner.OnDie();
		IUIEvents.Post(x => x.DisableCrosshair());

		var hitVelocity = damageInfo.hitVelocity.Normal;
		var cameraPoint = PlayerCamera.cam.WorldPosition - (hitVelocity * 150.0f);
		Rotation? hitDirection = Rotation.LookAt(hitVelocity.Normal, Vector3.Up);

		if (damageInfo.hitVelocity.IsNearlyZero())
		{
			hitDirection = null;
		}

		Spectator.instance.spectateTarget = damageInfo.instigator;
		Spectator.Teleport(cameraPoint, hitDirection);
		PlayerInfo.local.SetSpectateMode(SpectateMode.CharacterDeath);

		DestroyRequest();
	}

	public virtual void FellOutOfWorld()
	{
		DamageInfo damageInfo = new DamageInfo();
		damageInfo.instigator = owner;
		damageInfo.damageCauser = this;
		Die(damageInfo);
	}

	[Authority]
	public virtual void Teleport(Vector3 point)
	{
		WorldPosition = point;
		Transform.ClearInterpolation();
	}

	[Authority]
	public virtual void DestroyRequest()
	{
		GameObject.Destroy();
	}

	public void RoundCleanup()
	{
		if (IsProxy)
			return;

		GameObject.Destroy();
	}

	protected override void OnDestroy()
	{
		if (IsProxy)
		{
			base.OnDestroy();
			return;
		}

		IUIEvents.Post(x => x.DisableCrosshair());

		if (IsFullyValid(body))
		{
			if (keepCharacterBody)
			{
				body.OwnerDestroyed();
			}
			else
			{
				body.GameObject.Destroy();
			}
		}
	}
}
