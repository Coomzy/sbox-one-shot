
using Sandbox;
using System;
using System.Reflection.Metadata;

[Group("GMF")]
public class PlayerInfo : Component, Component.INetworkSpawn, IRoundEvents, IMatchEvents
{
	public static PlayerInfo local { get; private set; }
	public static List<PlayerInfo> all { get; private set; } = new List<PlayerInfo>();
	public static List<PlayerInfo> allActive { get; private set; } = new List<PlayerInfo>();
	public static List<PlayerInfo> allInactive { get; private set; } = new List<PlayerInfo>();
	public static List<PlayerInfo> allAlive { get; private set; } = new List<PlayerInfo>();
	public static List<PlayerInfo> allDead { get; private set; } = new List<PlayerInfo>();

	[Group("Setup"), Order(-100), Property] public GMFVoice voice { get; set; }

	[HostSync, Sync, Property] public string displayName { get; set; }
	[HostSync, Property] public ulong steamId { get; set; }
	[HostSync, Property] public Role role { get; set; }
	[HostSync, Sync] public NetDictionary<int, float?> clothing { get; set; } = new();

	[HostSync, Sync, Property] public Character character { get; set; }

	[HostSync,Sync, Property, Change("OnRep_isActive")] public bool isActive { get; private set; } = true;
	[HostSync, Sync, Property, Change("OnRep_isDead")] public bool isDead { get; private set; } = true;
	[HostSync, Sync, Property] public TimeSince aliveTime { get; private set; }
	[HostSync, Sync, Property] public TimeSince deadTime { get; private set; }

	[HostSync, Sync, Property] public int kills { get; set; }
	[HostSync, Sync, Property] public int deaths { get; set; }
	[HostSync, Sync, Property] public int wins { get; set; }
	[HostSync, Sync, Property] public int winsMatches { get; set; }

	[HostSync, Sync, Property] public int deathsRound { get; set; }
	[HostSync, Sync, Property] public int killsRound { get; set; }

	public bool isLocal => this == local;

    public bool isRecentlyDead => isDead && deadTime < 3.0f;

	void OnRep_isActive()
	{
		if (isActive)
		{
			allActive.AddUnique(this);
			allInactive.Remove(this);
		}
		else
		{
			allActive.Remove(this);
			allInactive.AddUnique(this);
		}
		OnRep_isDead();
	}

	void OnRep_isDead()
	{
		if (!isActive)
		{
			allAlive.Remove(this);
			allDead.Remove(this);
			return;
		}

		if (isDead)
		{
			allAlive.Remove(this);
			allDead.AddUnique(this);
		}
		else
		{
			allAlive.AddUnique(this);
			allDead.Remove(this);
		}
	}

	protected override void OnAwake()
	{
		all.Add(this);
	}

	protected override void OnDestroy()
	{
		all.Remove(this);
		allActive.Remove(this);
		allInactive.Remove(this);
		allAlive.Remove(this);
		allDead.Remove(this);

		base.OnDestroy();
	}

	protected override void OnStart()
	{
		OnRep_isActive();

		if (!IsProxy)
		{
			local = this;
			Spectator.TryCreate();
		}
	}

	protected override void OnUpdate()
	{
		UpdateVoice();
	}

	protected virtual void UpdateVoice()
	{
		if (voice == null)
			return;

		if (AudioPreferences.instance.voipMode == VOIPMode.Off)
		{
			voice.Mode = Voice.ActivateMode.Manual;
			return;
		}

		// TODO: Remove magic number
		voice.WorldspacePlayback = !isDead || deadTime < 3.0f;
		voice.Renderer = character?.body?.bodyRenderer;
		voice.Mode = AudioPreferences.instance.voipMode == VOIPMode.PushToTalk ? Voice.ActivateMode.PushToTalk : Voice.ActivateMode.AlwaysOn;

		if (!IsFullyValid(PlayerCamera.cam))
			return;

		voice.WorldPosition = PlayerCamera.cam.WorldPosition;
		voice.WorldRotation = PlayerCamera.cam.WorldRotation;
	}

	public virtual void OnNetworkSpawn(Connection connection)
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
		isActive = true;
		isDead = true;
		//character = null;
	}

	public virtual void Disconnected()
	{
		isActive = false;

		if (character != null)
		{
			character.DestroyRequest();
		}
		Unpossess();
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
		deaths++;
		deathsRound++;
		isDead = true;

		Unpossess();
	}

	public virtual void OnScoreKill()
	{
		kills++;
		killsRound++;
		
		OnScoreKill_Client();
	}

	[Authority]
	public virtual void OnScoreKill_Client()
	{
		Sandbox.Services.Stats.Increment(Stat.KILLS, 1);
	}

	public virtual void OnScoreRoundWin()
	{
		wins++;
		OnScoreRoundWin_Client();
	}

	[Authority]
	public virtual void OnScoreRoundWin_Client()
	{
		Sandbox.Services.Stats.Increment(Stat.WINS_ROUNDS, 1);
	}

	public virtual void OnScoreMatchWin()
	{
		winsMatches++;
		OnScoreMatchWin_Client();
	}

	[Authority]
	public virtual void OnScoreMatchWin_Client()
	{
		Sandbox.Services.Stats.Increment(Stat.WINS_MATCHES, 1);
	}

	public virtual void RoundCleanup()
	{
		deathsRound = 0;
		killsRound = 0;
	}

	// We'll miss the first callback for this, so don't do anything for initial setup in here
	public virtual void MatchStart()
	{
		RoundCleanup();
		deaths = 0;
		kills = 0;
		wins = 0;
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
