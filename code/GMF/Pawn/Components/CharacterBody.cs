
using Sandbox;
using Sandbox.Citizen;
using Sandbox.Network;
using System.Data;
using static Sandbox.ModelRenderer;
using static Sandbox.PhysicsContact;

[Group("GMF")]
public class CharacterBody : Component, IGameModeEvents
{
	[Group("Setup"), Order(-100), Property] public ModelPhysics bodyPhysics { get; set; }
	[Group("Setup"), Order(-100), Property] public ModelCollider bodyCollider { get; set; }
	[Group("Setup"), Order(-100), Property] public SkinnedModelRenderer bodyRenderer { get; set; }
	[Group("Setup"), Order(-100), Property] public CitizenAnimationHelper thirdPersonAnimationHelper { get; set; }
	[Group("Setup"), Order(-100), Property] public GameObject voipSocket { get; set; }
	[Group("Setup"), Order(-100), Property] public GameObject thirdPersonEquipmentAttachPoint { get; set; }

	[Group("Config"), Order(0), Property] public bool firstPersonDefaultView { get; set; } = true;

	[Group("Runtime"), Order(100), Property] public PlayerInfo playerInfo { get; set; }
	[Group("Runtime"), Order(100), Property, Sync, Change] public Character owner { get; set; }
	[Group("Runtime"), Order(100), Property] public float heightOffset { get; set; } = 0.0f;
	[Sync] public NetDictionary<int, float?> equippedClothing { get; set; } = new();

	public CharacterMovement characterMovement => owner?.movement;

	protected override void OnStart()
	{
		playerInfo = PlayerInfo.GetOwner(GameObject);

		if (IsProxy)
		{
			GameObject.Tags.Add(Tag.CHARACTER_BODY_REMOTE);
			GameObject.Tags.Remove(Tag.CHARACTER_BODY);
		}
		else
		{
			GameObject.Tags.Add(Tag.CHARACTER_BODY);
			GameObject.Tags.Remove(Tag.CHARACTER_BODY_REMOTE);
		}

		SetFirstPersonMode(firstPersonDefaultView && !IsProxy);
		bodyPhysics.Enabled = IsProxy;
	}

	public void OnownerChanged(Character oldValue, Character newValue)
	{
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

		foreach (var kvp in owner.owner.clothing)
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

		if (IsProxy)
			return;

		foreach (var clothingEntry in clothingContainer.Clothing)
		{
			equippedClothing[clothingEntry.Clothing.ResourceId] = clothingEntry.Tint;
		}
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
		WorldRotation = owner.WorldRotation;
	}

	protected virtual void UpdateAnimation()
	{
		if (thirdPersonAnimationHelper is null || characterMovement is null) return;

		thirdPersonAnimationHelper.WithWishVelocity(characterMovement.wishVelocity);
		thirdPersonAnimationHelper.WithVelocity(characterMovement.wishVelocity);
		thirdPersonAnimationHelper.IsGrounded = characterMovement.isGrounded;
		thirdPersonAnimationHelper.MoveStyle = characterMovement.wishVelocity.Length < 160f ? CitizenAnimationHelper.MoveStyles.Walk : CitizenAnimationHelper.MoveStyles.Run;

		// TODO: Make this based on the equipment type
		thirdPersonAnimationHelper.Handedness = CitizenAnimationHelper.Hand.Both;
		thirdPersonAnimationHelper.HoldType = CitizenAnimationHelper.HoldTypes.Rifle;

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

	public void SetPosition(bool teleport = false)
	{
		if (bodyPhysics.MotionEnabled)
		{
			return;
		}

		var playerPos = owner.WorldPosition;
		playerPos.z -= heightOffset;
		WorldPosition = playerPos;

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

	public virtual void ProceduralHitReaction(DamageInfo info)
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
		GameObject.Tags.Add(Tag.RAGDOLL);

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
	}

	public void RoundCleanup()
	{
		if (IsProxy)
			return;

		GameObject.Destroy();
	}
}
