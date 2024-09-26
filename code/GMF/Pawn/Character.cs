using Sandbox;
using Sandbox.Citizen;
using Sandbox.Network;
using System.Net.Mail;
using System.Numerics;
using static Sandbox.ModelRenderer;

[Group("GMF")]
public class Character : Component, IRoundInstance//, Component.INetworkSpawn
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
		if (!Check.IsFullyValid(body, equippedItem))
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
		base.OnUpdate();

		if (IsProxy)
			return;

		float killZ = WorldSettings.instance != null ? WorldSettings.instance.killZ : -500.0f;

		if (GameObject.Transform.Position.z <= killZ)
		{
			FellOutOfWorld();
		}
	}

	public virtual void TakeDamage(DamageInfo damageInfo)
	{
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
		owner.Unpossess();

		var cameraPoint = PlayerCamera.cam.Transform.Position - (damageInfo.hitVelocity.Normal * 150.0f);
		var hitDirection = Rotation.LookAt(-damageInfo.hitVelocity.Normal, Vector3.Up);
		hitDirection = Rotation.LookAt(damageInfo.hitVelocity.Normal, Vector3.Up);
		//hitDirection = Rotation.LookAt(Vector3.Right, Vector3.Up);
		Debuggin.ToScreen($"damageInfo.hitVelocity.Normal: {damageInfo.hitVelocity.Normal}", 15.0f);
		//hitDirection = Vector3.Up.EulerAngles.ToRotation();

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
		Transform.Position = point;
		Transform.ClearInterpolation();
	}

	[Authority]
	public virtual void DestroyRequest()
	{
		GameObject.Destroy();
	}

	public void Cleanup()
	{
		if (IsProxy)
			return;

		GameObject.Destroy();
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
