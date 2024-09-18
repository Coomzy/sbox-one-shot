using Sandbox;
using Sandbox.Citizen;
using System.Net.Mail;
using static Sandbox.ModelRenderer;

[Group("GMF")]
public class Character : Component//, Component.INetworkSpawn
{
	[Group("Runtime"), Order(100), Property, ReadOnly, Sync] public PlayerInfo owner { get; set; }

	[Group("Setup"), Property] public CharacterController controller { get; set; }
	[Group("Setup"), Property] public CharacterMovement movement { get; set; }
	[Group("Setup"), Property] public CapsuleCollider capsuleCollider { get; set; }
	[Group("Setup"), Property] public GameObject playerBodyPrefab { get; set; }

	[Group("Runtime"), Order(100), Property, ReadOnly, Sync] public CharacterBody body { get; set; }
	[Group("Runtime"), Order(100), Property, ReadOnly, Sync] public Equipment equippedItem { get; set; }
	[Group("Runtime"), Order(100), Property, ReadOnly, Sync] public bool isDead { get; set; }


	[Sync] public Angles eyeAngles { get; set; }

	public virtual void Possess(PlayerInfo playerInfo)
	{
		owner = playerInfo;
		playerInfo.character = this;
	}

	public virtual void Unpossess()
	{
		if (owner == null)
			return;

		owner.character = null;
		owner = null;
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

	[Authority]
	public virtual void Die(DamageInfo damageInfo)
	{
		if (isDead)
			return;

		isDead = true;
		body.Die(damageInfo);
	}

	public virtual void FellOutOfWorld()
	{
		DamageInfo damageInfo = new DamageInfo();
		damageInfo.instigator = owner;
		damageInfo.damageCauser = this;
		Die(damageInfo);
	}
}
