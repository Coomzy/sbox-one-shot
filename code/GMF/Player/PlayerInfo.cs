
using Sandbox;
using System;
using System.Reflection.Metadata;

[Group("GMF")]
public class PlayerInfo : Component, Component.INetworkSpawn, IRoundEvents
{
	public static PlayerInfo local { get; private set; }
	public static List<PlayerInfo> all { get; private set; } = new List<PlayerInfo>();


	[HostSync, Sync, Property] public string displayName { get; set; }
	[HostSync, Sync] public NetDictionary<int, float?> clothing { get; set; } = new();

	public ulong steamId { get; set; }
	[HostSync, Sync, Property] public Character character { get; set; }

	[HostSync,Sync, Property] public bool isDead { get; private set; } = true;
	[HostSync, Sync, Property] public TimeSince aliveTime { get; private set; }
	[HostSync, Sync, Property] public TimeSince deadTime { get; private set; }

	[HostSync, Sync, Property] public int deathCount { get; set; }
	[HostSync, Sync, Property] public int killCount { get; set; }
	[HostSync, Sync, Property] public int winCount { get; set; }

	[HostSync, Sync, Property] public int deathCountRound { get; set; }
	[HostSync, Sync, Property] public int killCountRound { get; set; }

	public bool isLocal => this == local;

	protected override void OnAwake()
	{
		all.Add(this);
	}

	protected override void OnStart()
	{
		Debuggin.ToScreen($"PlayerInfo::OnStart() '{GameObject.Name}' sProxy: {IsProxy}", 15.0f);
		if (!IsProxy)
		{
			local = this;
			Spectator.TryCreate();
		}
	}

	public void OnNetworkSpawn(Connection connection)
	{
		if (connection.IsHost)
		{
			local = this;
			Spectator.TryCreate();
		}
		displayName = connection.DisplayName;

		var avatarJson = connection.GetUserData("avatar");
		var clothingContainer = new ClothingContainer();
		clothingContainer.Deserialize(avatarJson);
		foreach (var clothingEntry in clothingContainer.Clothing)
		{
			clothing[clothingEntry.Clothing.ResourceId] = clothingEntry.Tint;
		}
	}

	public virtual void Rejoined()
	{
		character = null;
	}

	public virtual void Diconnected()
	{
		if (character != null)
		{
			character.DestroyRequest();
		}
		character = null;
	}

	public virtual void OnBecameHost(PlayerInfo previousHost)
	{
		
	}

	public virtual void Possess(Character newCharacter)
	{
		if (character != null)
		{
			Unpossess();
		}
		character = newCharacter;
		isDead = false;
		aliveTime = 0;
	}

	public virtual void Unpossess()
	{
		if (character != null)
		{
			character.Unpossess();
		}
		character = null;
		isDead = true;
		deadTime = 0;
	}

	public virtual void DestroyPawn()
	{
		var oldCharacter = character;
		Unpossess();
		if (oldCharacter != null)
		{
			oldCharacter.DestroyRequest();
		}
	}

	public virtual void OnDie()
	{
		deathCount++;
		deathCountRound++;
		isDead = true;

		Unpossess();
	}

	public virtual void OnScoreKill()
	{
		killCount++;
		killCountRound++;
	}

	public virtual void OnScoreWin()
	{
		winCount++;
	}

	public virtual void RoundCleanup()
	{
		deathCountRound = 0;
		killCountRound = 0;
	}

	public static bool TryFromConnection(Connection connection, out PlayerInfo playerInfo) => TryFromConnection<PlayerInfo>(connection, out playerInfo);
	public static bool TryFromConnection<T>(Connection connection, out T playerInfo) where T : PlayerInfo
	{
		playerInfo = FromConnection<T>(connection);
		return IsFullyValid(playerInfo);
	}

	public static PlayerInfo FromConnection(Connection connection) => FromConnection<PlayerInfo>(connection);
	public static T FromConnection<T>(Connection connection) where T : PlayerInfo
	{
		foreach (var playerInfo in all)
		{
			if (!IsFullyValid(playerInfo))
				continue;

			if (!playerInfo.Network.Active || playerInfo.Network.OwnerId == Guid.Empty)
				continue;

			if (connection.Id == playerInfo.Network.OwnerId)
				return playerInfo as T;
		}

		return null;
	}

	public static bool TryGetOwner(Component component, out PlayerInfo playerInfo) => TryGetOwner<PlayerInfo>(component?.GameObject, out playerInfo);
	public static bool TryGetOwner(GameObject gameObject, out PlayerInfo playerInfo) => TryGetOwner<PlayerInfo>(gameObject, out playerInfo);
	public static bool TryGetOwner<T>(Component component, out T playerInfo) where T : PlayerInfo => TryGetOwner(component?.GameObject, out playerInfo);
	public static bool TryGetOwner<T>(GameObject gameObject, out T playerInfo) where T : PlayerInfo
	{
		playerInfo = GetOwner<T>(gameObject);
		return playerInfo != null;
	}

	public static PlayerInfo GetOwner(Component component) => GetOwner<PlayerInfo>(component?.GameObject);
	public static PlayerInfo GetOwner(GameObject gameObject) => GetOwner<PlayerInfo>(gameObject);
	public static T GetOwner<T>(Component component) where T : PlayerInfo => GetOwner<T>(component?.GameObject);
	public static T GetOwner<T>(GameObject gameObject) where T : PlayerInfo
	{
		if (gameObject == null || !gameObject.IsValid)
			return null;

		if (!gameObject.Network.Active || gameObject.Network.OwnerId == Guid.Empty)
			return null;

		foreach (var playerInfo in all)
		{
			if (playerInfo == null || !playerInfo.IsValid || playerInfo?.GameObject == null)
				return null;

			if (!playerInfo.Network.Active || playerInfo.Network.OwnerId == Guid.Empty)
				return null;

			if (gameObject.Network.OwnerId == playerInfo.Network.OwnerId)
				return playerInfo as T;
		}

		return null;
	}
}
