
using Sandbox;
using Sandbox.Citizen;
using Sandbox.Network;
using System.Data;
using static Sandbox.ModelRenderer;
using static Sandbox.PhysicsContact;

[Group("GMF")]
public class CharacterBody : Component, IRoundEvents, Component.INetworkSpawn
{
	[Group("Setup"), Order(-100), Property] public ModelPhysics bodyPhysics { get; set; }
	[Group("Setup"), Order(-100), Property] public ModelCollider bodyCollider { get; set; }
	[Group("Setup"), Order(-100), Property] public SkinnedModelRenderer bodyRenderer { get; set; }
	[Group("Setup"), Order(-100), Property] public CitizenAnimationHelper thirdPersonAnimationHelper { get; set; }
	[Group("Setup"), Order(-100), Property] public Voice voiceTransmitter { get; set; }
	[Group("Setup"), Order(-100), Property] public GameObject thirdPersonEquipmentAttachPoint { get; set; }

	[Group("Config"), Order(0), Property] public bool firstPersonDefaultView { get; set; } = true;

	[Group("Runtime"), Order(100), Property] public PlayerInfo playerInfo { get; set; }
	[Group("Runtime"), Order(100), Property, Sync, Change] public Character owner { get; set; }
	[Group("Runtime"), Order(100), Property] public float heightOffset { get; set; } = 0.0f;
	[Sync, Change] public NetDictionary<int, float?> equippedClothing { get; set; } = new();

	public CharacterMovement characterMovement => owner?.movement;

	protected override void OnStart()
	{
		playerInfo = PlayerInfo.GetOwner(GameObject);

		//var playerInfo = this.GetOwningPlayerInfo();
		//Log.Info($"CharacterVisual::OnStart() connection: {Network.OwnerId} playerInfo: {playerInfo} playerInfo.character: {playerInfo?.character}");
		/*if (Application.IsDebug)
		{
			voiceTransmitter.Enabled = false;
		}*/

		if (owner != null)
		{
			//OnownerChanged(null, owner);
		}

		if (Networking.IsClient)
		{
			//Log.Info($"CharacterVisual::OnStart() connection: {Network.Owner} connection avatar: {Network.Owner.GetUserData("avatar")}");
		}

		//LoadClothing(GameObject.Network.Owner);

		//Log.Info($"CharacterVisual::OnStart() '{GameObject.Name}' Network.Owner: {GameObject.Network.Owner}, IsProxy: {IsProxy}");
		//Log.Info($"CharacterVisual::OnStart()             IsOwner: {GameObject.Network.IsOwner}, IsCreator: {GameObject.Network.IsCreator}, OwnerConnection: {GameObject.Network.Owner}, IsHost: {Networking.IsHost} for '{GameObject.Name}'");
		SetFirstPersonMode(firstPersonDefaultView && !IsProxy);
		bodyPhysics.Enabled = IsProxy;

		thirdPersonAnimationHelper.Handedness = CitizenAnimationHelper.Hand.Both;
		thirdPersonAnimationHelper.HoldType = CitizenAnimationHelper.HoldTypes.Rifle;

		if (IsProxy)
		{

			//Log.Info($"CharacterVisual::OnStart() Network.Owner: {Network.Owner} avatar: {Network.Owner.GetUserData("avatar")}");
			//LoadClothing(Network.Owner);
		}

		Wait();
	}

	async void Wait()
	{
		await Task.Frame();
		await Task.DelaySeconds(5);

		if (owner != null)
		{
			OnownerChanged(null, owner);
		}

		//Log.Info($"'{GameObject.Name}' Network.Owner: {Network.Owner} IsProxy: {IsProxy} avatar: {Network.Owner.GetUserData("avatar")}");
		if (IsProxy)
		{
			//return;
		}

		//LoadClothing();
		//SetFirstPersonMode(firstPersonDefaultView && !IsProxy);
	}

	public void OnownerChanged(Character oldValue, Character newValue)
	{
		//Log.Info($"OnownerChanged() newValue.Network.Owner: {newValue.Network.Owner} owner: {owner} owner?.owner: {owner?.owner} owner?.owner?.clothing.Count: {owner?.owner?.clothing.Count}");
		//LoadClothing(newValue.Network.Owner);

		if (owner?.owner?.clothing == null)
		{
			return;
		}

		var clothingContainer = new ClothingContainer();

		var clothingIdToResource = new Dictionary<int, Clothing>();
		foreach (var x in ResourceLibrary.GetAll<Clothing>())
		{
			clothingIdToResource[x.ResourceId] = x;
		}

		if (owner?.owner?.clothing != null)
		{			
			//Log.Info($"OnownerChanged() owner?.owner?.clothing.Count: {owner?.owner?.clothing.Count}");
		}
		else
		{

			//Log.Info($"OnownerChanged() owner?.owner?.clothing: null");
		}

		foreach (var kvp in owner.owner.clothing)
		{
			//Log.Info($"OnownerChanged() kvp.Key: {kvp.Key} kvp.Value: {kvp.Value}");
			var clothing = clothingIdToResource[kvp.Key];
			clothingContainer.Toggle(clothing);

			var entry = clothingContainer.FindEntry(clothing);

			if (kvp.Value != null)
			{
				entry.Tint = kvp.Value.Value;
			}
		}

		clothingContainer.Apply(bodyRenderer);
		SetFirstPersonMode(firstPersonDefaultView && !IsProxy);
	}

	public void OnequippedClothingChanged(NetDictionary<int, float?> oldValue, NetDictionary<int, float?> newValue)
	{
		return;
		Log.Info($"OnequippedClothingChanged() equippedClothing: {equippedClothing.Count}");

		var clothingContainer = new ClothingContainer();

		var clothingIdToResource = new Dictionary<int, Clothing>();
		foreach (var x in ResourceLibrary.GetAll<Clothing>())
		{
			clothingIdToResource[x.ResourceId] = x;
		}

		foreach (var kvp in equippedClothing)
		{
			var clothing = clothingIdToResource[kvp.Key];
			clothingContainer.Toggle(clothing);

			var entry = clothingContainer.FindEntry(clothing);

			if (kvp.Value != null)
			{
				entry.Tint = kvp.Value.Value;
			}
		}

		clothingContainer.Apply(bodyRenderer);
		SetFirstPersonMode(firstPersonDefaultView && !IsProxy);
		//LoadClothing(OnclothingChanged.Network.Owner);
	}


	public virtual async void OnNetworkSpawn(Connection connection)
	{
		//Log.Info($"CharacterVisual::OnNetworkSpawn() 1 connection: {connection} connection avatar: {connection.GetUserData("avatar")}");
		//Log.Info($"CharacterVisual::OnNetworkSpawn() 1 connection: {connection}, OwnerConnection: {GameObject.Network.Owner}, IsProxy: {IsProxy}");
		//Log.Info($"CharacterVisual::OnNetworkSpawn() PRE  IsOwner: {GameObject.Network.IsOwner}, IsCreator: {GameObject.Network.IsCreator}, OwnerConnection: {GameObject.Network.Owner}, IsHost: {Networking.IsHost} for '{GameObject.Name}' connection: {connection}");

		//LoadClothing(connection);
		await Task.Frame();
		await Task.DelaySeconds(5);
		//Log.Info($"connection: {connection}, Network.Owner: {Network.Owner}");
		//LoadClothing(connection);
		//await Task.Frame();
		//Log.Info($"CharacterVisual::OnNetworkSpawn() 2 connection: {connection}, OwnerConnection: {GameObject.Network.Owner}, IsProxy: {IsProxy}");
		//Log.Info($"CharacterVisual::OnNetworkSpawn() POST IsOwner: {GameObject.Network.IsOwner}, IsCreator: {GameObject.Network.IsCreator}, OwnerConnection: {GameObject.Network.Owner}, IsHost: {Networking.IsHost} for '{GameObject.Name}' connection: {connection}");

		/*LoadClothing(connection);

		Log.Info($"CharacterVisual::OnNetworkSpawn() connection: {connection}, IsProxy: {IsProxy}");
		SetFirstPersonMode(firstPersonDefaultView && !IsProxy);*/
	}

	[ConCmd("ReclothEveryone")]
	public static void ReclothEveryone()
	{
		var insts = Game.ActiveScene.GetAllComponents<CharacterBody>();
		foreach (var inst in insts)
		{
			inst.OnequippedClothingChanged(null, null);
			//inst.LoadClothing();
		}
	}

	public virtual void LoadClothing(Connection connection = null)
	{
		if (connection == null)
		{
			connection = Network.Owner;
		}

		if (connection == null)
			return;

		var avatarJson = connection.GetUserData("avatar");
		var clothingContainer = new ClothingContainer();
		clothingContainer.Deserialize(avatarJson);
		//clothingContainer.Apply(bodyRenderer);
		//SetFirstPersonMode(firstPersonDefaultView && !IsProxy);

		if (IsProxy)
			return;

		foreach (var clothingEntry in clothingContainer.Clothing)
		{
			equippedClothing[clothingEntry.Clothing.ResourceId] = clothingEntry.Tint;
		}
		OnequippedClothingChanged(null, null);
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

		if (IsProxy)
		{
			return;
		}

		if (owner == null || !owner.IsValid)
		{
			return;
		}

		SetPosition(true);
		Transform.Rotation = owner.Transform.Rotation;

		//Log.Info($"Network.Owner: {Network.Owner}, avatar: {Network.Owner.GetUserData("avatar")}");
	}

	protected virtual void UpdateAnimation()
	{
		if (thirdPersonAnimationHelper is null || characterMovement is null) return;

		thirdPersonAnimationHelper.WithWishVelocity(characterMovement.wishVelocity);
		thirdPersonAnimationHelper.WithVelocity(characterMovement.wishVelocity);
		thirdPersonAnimationHelper.IsGrounded = characterMovement.isGrounded;
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

		thirdPersonAnimationHelper.IkLeftHand = owner?.equippedItem?.equipmentProxy?.twoHandedGrip;
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
		if (bodyPhysics.MotionEnabled)
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

	[Broadcast]
	public virtual void TakeDamage(DamageInfo damageInfo)
	{
		ProceduralHitReaction(damageInfo);
		if (IsProxy)
		{
			return;
		}
		owner.TakeDamage(damageInfo);
	}

	public void ProceduralHitReaction(DamageInfo info)
	{
		var target = thirdPersonAnimationHelper.Target;

		var tx = target.GetBoneObject(info.hitBodyIndex);
		var localToBone = tx.Transform.Local.Position;
		if (localToBone == Vector3.Zero) localToBone = Vector3.One;

		var damageScale = 10.0f;
		target.Set("hit", true);
		target.Set("hit_bone", info.hitBodyIndex);
		target.Set("hit_offset", localToBone);
		target.Set("hit_direction", info.hitVelocity.Normal);
		target.Set("hit_strength", (info.hitVelocity.Length / 1000.0f) * damageScale);
	}

	[Broadcast]
	public virtual void Die(DamageInfo damageInfo)
	{
		bodyPhysics.Enabled = true;
		bodyPhysics.MotionEnabled = true;

		if (IsProxy)
			return;

		SetFirstPersonMode(false);
		CleanupBody();
	}

	public virtual async void CleanupBody()
	{
		await Task.DelaySeconds(10.0f);

		GameObject.Destroy();
	}

	public virtual void OwnerDestroyed()
	{

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

	public void RoundCleanup()
	{
		if (IsProxy)
			return;

		GameObject.Destroy();
	}
}
