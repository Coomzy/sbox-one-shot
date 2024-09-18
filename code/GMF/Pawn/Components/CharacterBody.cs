
using Sandbox;
using Sandbox.Citizen;
using static Sandbox.ModelRenderer;
using static Sandbox.PhysicsContact;

[Group("GMF")]
public class CharacterBody : Component, Component.INetworkSpawn
{

	[Group("Setup"), Order(-100), Property] public ModelPhysics bodyPhysics { get; set; }
	[Group("Setup"), Order(-100), Property] public SkinnedModelRenderer bodyRenderer { get; set; }
	[Group("Setup"), Order(-100), Property] public CitizenAnimationHelper thirdPersonAnimationHelper { get; set; }
	[Group("Setup"), Order(-100), Property] public GameObject thirdPersonEquipmentAttachPoint { get; set; }

	[Group("Config"), Order(0), Property] public bool firstPersonDefaultView { get; set; } = true;

	[Group("Runtime"), Order(100), Property, Sync] public Character owner { get; set; }
	[Group("Runtime"), Order(100), Property] public float heightOffset { get; set; } = 0.0f;
	[Group("Runtime"), Order(100), Property, Sync] bool hasNetworkSpawned { get; set; } = false;


	public CharacterMovement characterMovement => owner?.movement;

	protected override void OnStart()
	{
		//LoadClothing(GameObject.Network.OwnerConnection);

		//Log.Info($"CharacterVisual::OnStart() OwnerConnection: {GameObject.Network.OwnerConnection}, IsProxy: {IsProxy}, hasNetworkSpawned: {hasNetworkSpawned}");
		//Log.Info($"CharacterVisual::OnStart()             IsOwner: {GameObject.Network.IsOwner}, IsCreator: {GameObject.Network.IsCreator}, OwnerConnection: {GameObject.Network.OwnerConnection}, IsHost: {Networking.IsHost} for '{GameObject.Name}'");
		SetFirstPersonMode(firstPersonDefaultView && !IsProxy);
		bodyPhysics.Enabled = IsProxy;

		thirdPersonAnimationHelper.Handedness = CitizenAnimationHelper.Hand.Both;
		thirdPersonAnimationHelper.HoldType = CitizenAnimationHelper.HoldTypes.Rifle;

		if (IsProxy)
		{
			Log.Info($"CharacterVisual::OnStart() OwnerConnection: {GameObject.Network.OwnerConnection}");
			//LoadClothing(GameObject.Network.OwnerConnection);
		}
	}

	public async virtual void OnNetworkSpawn(Connection connection)
	{
		hasNetworkSpawned = true;
		//Log.Info($"CharacterVisual::OnNetworkSpawn() 1 connection: {connection}, OwnerConnection: {GameObject.Network.OwnerConnection}, IsProxy: {IsProxy}");
		//Log.Info($"CharacterVisual::OnNetworkSpawn() PRE  IsOwner: {GameObject.Network.IsOwner}, IsCreator: {GameObject.Network.IsCreator}, OwnerConnection: {GameObject.Network.OwnerConnection}, IsHost: {Networking.IsHost} for '{GameObject.Name}' connection: {connection}");
		LoadClothing(connection);
		await Task.Frame();
		//Log.Info($"CharacterVisual::OnNetworkSpawn() 2 connection: {connection}, OwnerConnection: {GameObject.Network.OwnerConnection}, IsProxy: {IsProxy}");
		//Log.Info($"CharacterVisual::OnNetworkSpawn() POST IsOwner: {GameObject.Network.IsOwner}, IsCreator: {GameObject.Network.IsCreator}, OwnerConnection: {GameObject.Network.OwnerConnection}, IsHost: {Networking.IsHost} for '{GameObject.Name}' connection: {connection}");

		/*LoadClothing(connection);

		Log.Info($"CharacterVisual::OnNetworkSpawn() connection: {connection}, IsProxy: {IsProxy}");
		SetFirstPersonMode(firstPersonDefaultView && !IsProxy);*/
	}

	public virtual void LoadClothing(Connection connection = null)
	{
		if (connection == null)
		{
			connection = Network.OwnerConnection;
		}

		var avatarJson = connection.GetUserData("avatar");
		var clothing = new ClothingContainer();
		clothing.Deserialize(avatarJson);
		clothing.Apply(bodyRenderer);
	}

	public virtual void SetFirstPersonMode(bool isFirstPerson)
	{
		if (thirdPersonAnimationHelper is null)
			return;

		var renderMode = isFirstPerson ? ShadowRenderType.ShadowsOnly : ShadowRenderType.On;
		thirdPersonAnimationHelper.Target.RenderType = renderMode;
		//SetAllBodyGroups(thirdPersonAnimationHelper.Target, !isFirstPerson);

		foreach (var clothing in thirdPersonAnimationHelper.Target.Components.GetAll<ModelRenderer>(FindMode.InChildren))
		{
			if (!clothing.Tags.Has("clothing"))
				continue;

			clothing.RenderType = renderMode;
			//SetAllBodyGroups(clothing, !isFirstPerson);
		}
	}

	void SetAllBodyGroups(ModelRenderer skinnedModelRenderer, bool active)
	{
		for (var i = 0; i < 5; i++)
		{
			skinnedModelRenderer.SetBodyGroup(i, 1);
		}
	}

	protected override void OnUpdate()
	{
		UpdateAnimation();

		if (owner == null || !owner.IsValid)
		{
			return;
		}

		SetPosition(true);
		Transform.Rotation = owner.Transform.Rotation;
	}

	protected virtual void UpdateAnimation()
	{
		if (thirdPersonAnimationHelper is null || characterMovement is null) return;

		thirdPersonAnimationHelper.WithWishVelocity(characterMovement.wishVelocity);
		thirdPersonAnimationHelper.WithVelocity(characterMovement.characterController.Velocity);
		thirdPersonAnimationHelper.IsGrounded = characterMovement.characterController.IsOnGround;
		thirdPersonAnimationHelper.MoveStyle = characterMovement.wishVelocity.Length < 160f ? CitizenAnimationHelper.MoveStyles.Walk : CitizenAnimationHelper.MoveStyles.Run;

		if (characterMovement.isSliding)
		{
			thirdPersonAnimationHelper.DuckLevel = MathY.MoveTowards(thirdPersonAnimationHelper.DuckLevel, 1.0f, Time.Delta * characterMovement.config.slideDuckSpeed);
			thirdPersonAnimationHelper.SpecialMove = CitizenAnimationHelper.SpecialMoveStyle.Slide;
		}
		else
		{
			thirdPersonAnimationHelper.SpecialMove = CitizenAnimationHelper.SpecialMoveStyle.None;

			float duckTarget = characterMovement.isCrouching ? 1.0f : 0.0f;
			thirdPersonAnimationHelper.DuckLevel = MathY.MoveTowards(thirdPersonAnimationHelper.DuckLevel, duckTarget, Time.Delta * characterMovement.config.slideDuckSpeed);
		}

		//animationHelper.Target.Set("b_grounded", characterMovement.characterController.IsOnGround);
		//animationHelper.Target.Set("b_grounded", characterMovement.characterController.IsOnGround);
		//thirdPersonAnimationHelper.Target.Set("b_grounded", characterMovement.characterController.IsOnGround);
		//thirdPersonAnimationHelper.Target.Set("duck", characterMovement.crouching ? 1.0f : 0.0f);
		//thirdPersonAnimationHelper.Target.Set("move_style", (int)(characterMovement.wishVelocity.Length < 160f ? CitizenAnimationHelper.MoveStyles.Walk : CitizenAnimationHelper.MoveStyles.Run));

		var lookDir = owner.eyeAngles.ToRotation().Forward * 1024;
		thirdPersonAnimationHelper.WithLook(lookDir, 1, 0.5f, 0.25f);
	}

	public void Jump()
	{
		thirdPersonAnimationHelper.TriggerJump();
	}

	public void Shoot()
	{
		thirdPersonAnimationHelper.Target.Set("b_attack", true);
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		//SetPosition();
		//Transform.Rotation = owner.Transform.Rotation;
	}

	public void SetPosition(bool teleport = false)
	{
		if (bodyPhysics.Enabled)
		{
			return;
		}

		var playerPos = owner.Transform.Position;
		playerPos.z -= heightOffset;
		Transform.Position = playerPos;

		if (teleport)
		{
			Transform.ClearInterpolation();
		}
	}

	public virtual void Die(DamageInfo damageInfo)
	{		
		bodyPhysics.MotionEnabled = true;
	}

	public override void Reset()
	{
		base.Reset();

		bodyPhysics = Components.GetInDescendantsOrSelf<ModelPhysics>();
		bodyRenderer = Components.GetInDescendantsOrSelf<SkinnedModelRenderer>();
		thirdPersonAnimationHelper = Components.GetInDescendantsOrSelf<CitizenAnimationHelper>();
		thirdPersonEquipmentAttachPoint = Utils.FindInChildren(GameObject, x => x.Name == "hold_R");
		//thirdPersonEquipmentAttachPoint = Components.GetAll<GameObject>(FindMode.EverythingInChildren).First(x => Log.Info("") x.Name == "hold_R");
		//Components.GetAll<GameTransform>(FindMode.EverythingInChildren).First(x => { Log.Info($"x.Name: {x.GameObject.Name}"); return true; });
	}
}
