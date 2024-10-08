
using Sandbox;
using Sandbox.Diagnostics;
using System;
using System.Reflection.Metadata;
using System.Text.Json.Serialization;
using static Sandbox.Voice;

[Group("GMF")]
public class PlayerInfo : Component, Component.INetworkSpawn, IGameModeEvents
{
	public static PlayerInfo local { get; private set; }
	public static List<PlayerInfo> all { get; private set; } = new List<PlayerInfo>();
	public static List<PlayerInfo> allActive { get; private set; } = new List<PlayerInfo>();
	public static List<PlayerInfo> allInactive { get; private set; } = new List<PlayerInfo>();
	public static List<PlayerInfo> allAlive { get; private set; } = new List<PlayerInfo>();
	public static List<PlayerInfo> allDead { get; private set; } = new List<PlayerInfo>();

	[Property, HostSync] public Guid networkID { get; set; }
	[Property, HostSync] public ulong steamID { get; set; }
	[Property, HostSync, Sync] public string displayName { get; set; }
	[Property, HostSync] public Role role { get; set; }
	[Property, HostSync, Sync] public NetDictionary<int, float?> clothing { get; set; } = new();
	[Property, Sync] public Voice voiceGlobal { get; set; }

	[Property, HostSync, Sync] public Character character { get; set; }
	[Property, HostSync, Sync, Change("OnRep_spectateMode")] public SpectateMode spectateMode { get; private set; } = SpectateMode.Viewpoint;

	[Property, HostSync, Sync, Change("OnRep_isActive")] public bool isActive { get; private set; } = true;
	[Property, HostSync, Sync, Change("OnRep_isDead")] public bool isDead { get; private set; } = true;
	[Property, HostSync, Sync] public TimeSince aliveTime { get; private set; }
	[Property, HostSync, Sync] public TimeSince deadTime { get; private set; }

	[Property, HostSync, Sync] public int kills { get; set; }
	[Property, HostSync, Sync] public int deaths { get; set; }
	[Property, HostSync, Sync] public int wins { get; set; }
	[Property, HostSync, Sync] public int winsMatches { get; set; }

	[Property, HostSync, Sync] public int deathsRound { get; set; }
	[Property, HostSync, Sync] public int killsRound { get; set; }

	[Group("Runtime"), Property, JsonIgnore] public bool isLocal => this == local;
	[Group("Runtime"), Property, JsonIgnore] public Connection connection => Connection.Find(networkID);
	[Group("Runtime"), Property, JsonIgnore] public bool isRecentlyDead => isDead && deadTime < 3.0f;

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

	void OnRep_spectateMode(SpectateMode oldValue, SpectateMode newValue)
	{
		if (IsProxy)
			return;

		Spectator.instance.SetMode(oldValue, newValue);
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

		if (IsProxy)
			return;

		local = this;

		// TODO: This won't work if the spectator already thinks it's the same mode
		Spectator.instance.SetMode(SpectateMode.Viewpoint, spectateMode);
	}

	public virtual void OnNetworkSpawn(Connection connection)
	{
		if (connection.IsHost)
		{
			local = this;
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

	protected override void OnUpdate()
	{
		if (IsProxy)
			return;

		var activateMode = ActivateMode.Manual;
		if (UserPrefs.voipMode == VOIPMode.PushToTalk)
		{
			activateMode = ActivateMode.PushToTalk;
		}
		else if (UserPrefs.voipMode == VOIPMode.Open)
		{
			activateMode = ActivateMode.AlwaysOn;
		}

		if (!IsFullyValid(GMFVoiceProximity.local))
		{
			if (IsFullyValid(GMFVoiceProximity.local))
			{
				GMFVoiceGlobal.local.Mode = activateMode;
			}
			return;
		}

		if (IsFullyValid(character?.body))
		{
			GMFVoiceProximity.local.Mode = activateMode;
			GMFVoiceGlobal.local.Mode = ActivateMode.Manual;
		}
		else
		{
			GMFVoiceProximity.local.Mode = ActivateMode.Manual;
			GMFVoiceGlobal.local.Mode = activateMode;
		}
	}

	public virtual bool CanHearVoiceGlobal(PlayerInfo playerInfo)
	{
		if (IsFullyValid(playerInfo))
			return false;

		if (playerInfo.isLocal)
			return false;

		if (GameMode.instance.modeState != ModeState.ActiveRound)
			return true;

		if (isDead && !playerInfo.isDead)
			return false;

		return true;
	}

	public virtual bool CanHearVoiceProximity(PlayerInfo playerInfo)
	{
		if (IsFullyValid(playerInfo))
			return false;

		if (playerInfo.isLocal)
			return false;

		if (GameMode.instance.modeState != ModeState.ActiveRound)
			return false;

		if (isDead)
			return false;

		return true;
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

		if (!IsProxy)
		{
			Sandbox.Services.Stats.Increment(Stat.DEATHS, 1);
		}
		else
		{
			Log.Warning($"Tried to increment death stat but was on proxy!");
		}

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

	public virtual void SetSpectateMode(SpectateMode newSpectateMode)
	{
		spectateMode = newSpectateMode;
	}

	public static bool TryFromSteamID(ulong steamID, out PlayerInfo playerInfo) => TryFromSteamID<PlayerInfo>(steamID, out playerInfo);
	public static bool TryFromSteamID<T>(ulong steamID, out T playerInfo) where T : PlayerInfo
	{
		playerInfo = FromSteamID<T>(steamID);
		return IsFullyValid(playerInfo);
	}

	public static PlayerInfo FromSteamID(ulong steamID) => FromSteamID<PlayerInfo>(steamID);
	public static T FromSteamID<T>(ulong steamID) where T : PlayerInfo
	{
		foreach (var playerInfo in all)
		{
			if (!IsFullyValid(playerInfo))
				continue;

			if (playerInfo.steamID != steamID)
				continue;

			return playerInfo as T;
		}

		return null;
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

			if (playerInfo.networkID != connection.Id)
				continue;

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
