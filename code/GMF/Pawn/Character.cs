using Sandbox;
using Sandbox.Citizen;
using Sandbox.Network;
using System.Net.Mail;
using System.Numerics;
using static Sandbox.ModelRenderer;

[Group("GMF")]
public class Character : Component, IRoundEvents//, Component.INetworkSpawn
{
	public PlayerInfo owner => PlayerInfo.GetOwner(GameObject);
	public bool hasOwner => owner != null;

	//[Group("Runtime"), Order(100), Property, ReadOnly, HostSync] public PlayerInfo owner { get; set; }

	[Group("Setup"), Property] public CharacterController controller { get; set; }
	[Group("Setup"), Property] public CharacterMovement movement { get; set; }
	[Group("Setup"), Property] public CapsuleCollider capsuleCollider { get; set; }
	[Group("Setup"), Property] public GameObject playerBodyPrefab { get; set; }

	[Group("Runtime"), Order(100), Property, ReadOnly, Sync, Change("OnRep_body")] public CharacterBody body { get; set; }
	[Group("Runtime"), Order(100), Property, ReadOnly, Sync, Change("OnRep_equippedItem")] public Equipment equippedItem { get; set; }
	[Group("Runtime"), Order(100), Property, ReadOnly, Sync] public bool isDead { get; set; }

	[Group("Runtime"), Order(100), Property, ReadOnly, Sync] public Angles eyeAngles { get; set; }
	[Group("Runtime"), Order(100), Property, ReadOnly] public bool keepCharacterBody { get; set; }

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

	protected override void OnStart()
	{
		if (IsProxy)
		{
			GameObject.Tags.Add("remote");
			GameObject.Tags.Remove("local");
		}
		else
		{
			GameObject.Tags.Add("local");
			GameObject.Tags.Remove("remote");
		}

		TryAttachEquippedItem();
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

		float killZ = WorldInfo.instance != null ? WorldInfo.instance.killZ : -500.0f;

		if (GameObject.WorldPosition.z <= killZ)
		{
			FellOutOfWorld();
		}
	}

	public virtual void TakeDamage(DamageInfo damageInfo)
	{
		if (!IsFullyValid(this))
			return;

		Log.Info($"Character::TakeDamage() damageInfo.damageCauser: {damageInfo.damageCauser}");
		if (IsProxy)
		{
			return;
		}
		Die(damageInfo);
	}

	//[Authority]
	public virtual void Die(DamageInfo damageInfo)
	{
		Log.Info($"Character::Die() damageInfo.damageCauser: {damageInfo.damageCauser}");
		if (IsProxy)
		{
			return;
		}

		if (isDead)
			return;

		isDead = true;
		keepCharacterBody = true;
		if (equippedItem != null)
		{
			equippedItem.Drop(Vector3.Zero);
		}
		body.Die(damageInfo);
		owner.OnDie();

		var hitVelocity = damageInfo.hitVelocity.Normal;
		if (damageInfo.hitVelocity.IsNearlyZero())
		{
			//hitVelocity = GameObject.Transform.World.Forward;
		}
		Debuggin.ToScreen($"damageInfo.hitVelocity: {damageInfo.hitVelocity}, damageInfo.hitVelocity.IsNearlyZero() {damageInfo.hitVelocity.IsNearlyZero()}", 20.0f);

		var cameraPoint = PlayerCamera.cam.WorldPosition - (hitVelocity * 150.0f);
		Rotation? hitDirection = Rotation.LookAt(hitVelocity.Normal, Vector3.Up);
		//Rotation? hitDirection = PlayerCamera.cam.WorldRotation;

		if (damageInfo.hitVelocity.IsNearlyZero())
		{
			//hitDirection = null;
		}

		Spectator.Teleport(cameraPoint, hitDirection);

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
