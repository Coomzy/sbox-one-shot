using static Sandbox.Gizmo;
using System;
using Sandbox.Diagnostics;
using Sandbox.Network;
using Sandbox;
using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.VisualBasic;
using Sandbox.UI;

public enum ModeState
{
	PreGame,
	WaitingForPlayers,
	PreRound,
	ActiveRound,
	PostRound,
	PostGame
}

public class GameMode : Component, Component.INetworkListener, IHotloadManaged
{
	public static GameMode instance { get; private set; }
	[Group("Setup"), Order(-100), Property] public GameObject pawnPrefab { get; set; }

	[Group("Config - Delays"), Order(1), Property] public float preGameDelay { get; set; } = 3.0f;
	[Group("Config - Delays"), Order(1), Property] public float waitingForPlayersDelay { get; set; } = 3.0f;
	[Group("Config - Delays"), Order(1), Property] public float preRoundDelay { get; set; } = 1.0f;
	[Group("Config - Delays"), Order(1), Property] public float postRoundDelay { get; set; } = 3.0f;
	[Group("Config - Delays"), Order(1), Property] public float postGameDelay { get; set; } = 3.0f;

	[Group("Config"), Order(2), Property] public int requiredPlayerCount { get; set; } = 2;
	[Group("Config"), Order(2), Property] public int maxGameRounds { get; set; } = 10;
	[Group("Config"), Order(2), Property] public float? roundTime { get; set; } = 100.0f;
	[Group("Config"), Order(2), Property] public bool oneLifeOnly { get; set; } = false;
	[Group("Config"), Order(2), Property] public float defaultRespawnTime { get; set; } = 3.0f;

	public Dictionary<SpawnPoint, float> spawnPointToLastUsedTime = new Dictionary<SpawnPoint, float>();

	[Group("Runtime"), Order(100), HostSync, Property] public int roundCount { get; set; }
	[Group("Runtime"), Order(100), HostSync, Property] public ModeState modeState { get; set; }
	[Group("Runtime"), Order(100), Property] public TimeSince stateTime { get; set; }

	[Group("Runtime"), Order(100), Property] public TimeSince metPlayerReqTime { get; set; }

	[Group("Runtime"), Order(100), Property] SpawnPoint[] allSpawnPoints { get; set; }

	[Group("Runtime"), Order(100), Property] int preGameSpawnIndex { get; set; } = 0;

	protected override void OnAwake()
	{
		instance = this;

		stateTime = 0.0f;
	}

	protected override void OnStart()
	{
		var mapInstance = WorldSettings.instance?.mapInstance;
		if (mapInstance != null)
		{
			mapInstance.OnMapLoaded += OnMapLoaded;

			if (mapInstance.IsLoaded)
			{
				OnMapLoaded();
			}
		}
	}

	protected override void OnDestroy()
	{
		instance = null;

		var mapInstance = WorldSettings.instance?.mapInstance;
		if (mapInstance != null)
		{
			mapInstance.OnMapLoaded -= OnMapLoaded;
		}
	}

	protected virtual void OnMapLoaded()
	{
		allSpawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();
		Debuggin.ToScreen($"OnMapLoaded() allSpawnPoints: {allSpawnPoints.Length}", 15.0f);
	}

	public virtual void OnPlayerConnected(OSPlayerInfo playerInfo, Connection channel)
	{

	}

	public virtual void OnPlayerDisconnected(OSPlayerInfo playerInfo)
	{

	}

	// TODO: I renamed Pawn to Character, but I want to go back to Pawn at some point
	public virtual Character CreatePawn(PlayerInfo playerInfo, SpawnPoint spawnPoint)
	{
		return CreatePawn(playerInfo, spawnPoint.Transform.Position, spawnPoint.Transform.Rotation);
	}

	public virtual Character CreatePawn(PlayerInfo playerInfo, Vector3 spawnPos, Rotation spawnRot)
	{
		var pawnPrefab = GetPawnPrefabForPlayer(playerInfo);
		var pawn = pawnPrefab.Clone(spawnPos, spawnRot).BreakPrefab().Components.Get<OSCharacter>();
		pawn.GameObject.NetworkSpawn(playerInfo.Network.Owner);
		playerInfo.Possess(pawn);
		pawn.GameObject.Name = $"Pawn ({playerInfo.displayName})";

		return pawn;
	}

	protected virtual GameObject GetPawnPrefabForPlayer(PlayerInfo playerInfo)
	{
		return pawnPrefab;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		Debuggin.ToScreen($"modeState: {modeState}", color: Color.Red);

		if (IsProxy)
		{
			return;
		}

		switch (modeState)
		{
			case ModeState.PreGame:
				PreGameUpdate();
				break;
			case ModeState.WaitingForPlayers:
				WaitingForPlayersUpdate();
				break;
			case ModeState.PreRound:
				PreRoundUpdate();
				break;
			case ModeState.ActiveRound:
				ActiveRoundUpdate();
				break;
			case ModeState.PostRound:
				PostRoundUpdate();
				break;
			case ModeState.PostGame:
				PostGameUpdate();
				break; 
			default:
				Log.Error($"What state is this? modeState: {modeState}");
				break;
		}
	}

	// Pre Game
	protected virtual void PreGameStart()
	{
		preGameSpawnIndex = 0;
	}

	protected virtual void PreGameUpdate()
	{
		if (stateTime < preGameDelay)
		{
			return;
		}

		if (HasMetRequiredPlayerCount())
		{
			SetModeState(ModeState.PreRound);
		}
		else
		{
			SetModeState(ModeState.WaitingForPlayers);
		}
	}

	// Waiting For Players
	protected virtual void WaitingForPlayersStart()
	{

	}

	protected virtual void WaitingForPlayersUpdate()
	{
		if (!HasMetRequiredPlayerCount())
		{
			metPlayerReqTime = 0;
		}

		if (metPlayerReqTime < waitingForPlayersDelay)
		{
			TryRespawnPlayers();
			return;
		}

		SetModeState(ModeState.PreRound);
	}

	// Pre Round
	protected virtual void PreRoundStart()
	{
		foreach (var playerInfo in PlayerInfo.all)
		{
			playerInfo.DestroyPawn();
		}

		CleanupRoundInstances();
		TeleportSpectatorsToStartingPoint();
	}

	protected virtual void PreRoundUpdate()
	{
		if (stateTime < preRoundDelay)
		{
			return;
		}

		SetModeState(ModeState.ActiveRound);
	}

	// Active Round
	protected virtual void ActiveRoundStart()
	{

	}

	protected virtual void ActiveRoundUpdate()
	{
		if (RoundEndCondition())
		{
			RoundOver();
			return;
		}

		TryRespawnPlayers();
	}

	// Post Round
	protected virtual void PostRoundStart()
	{

	}

	protected virtual void PostRoundUpdate()
	{
		if (stateTime < postRoundDelay)
		{
			return;
		}

		if (HasMetRequiredPlayerCount())
		{
			SetModeState(ModeState.PreRound);
		}
		else
		{
			SetModeState(ModeState.WaitingForPlayers);
		}
	}

	// Post Game
	protected virtual void PostGameStart()
	{

	}

	protected virtual void PostGameUpdate()
	{

	}

	void SetModeState(ModeState state)
	{
		if (modeState == state)
			return;

		modeState = state;
		stateTime = 0.0f;

		switch ( modeState )
		{
			case ModeState.PreGame:
				PreGameStart();
				break;
			case ModeState.PreRound:
				PreRoundStart();
				break;
			case ModeState.ActiveRound:
				ActiveRoundStart();
				break;
			case ModeState.PostRound:
				PostRoundStart();
				break;
			case ModeState.PostGame:
				PostGameStart();
				break;
		}
	}

	public virtual void TryRespawnPlayers()
	{
		// TODO: Make this way less shitty, it doesn't effect one shot though
		foreach (PlayerInfo playerInfo in NetworkManager.instance.playerInfos)
		{
			if (!CanRespawn(playerInfo))
				continue;

			var randomSpawn = Game.Random.FromArray(allSpawnPoints);
			if (randomSpawn == null)
			{
				continue;
			}
			var slasherPawn = CreatePawn(playerInfo, randomSpawn);
		}
	}

	public virtual bool RoundEndCondition()
	{
		if (oneLifeOnly)
		{
			int alivePlayerCount = 0;
			foreach (var playerInfo in PlayerInfo.all)
			{
				if (playerInfo.isDead)
					continue;
				alivePlayerCount++;
			}
			return alivePlayerCount < 2;
		}

		if (roundTime.HasValue && stateTime >= roundTime.Value)
		{
			return stateTime >= roundTime.Value;
		}

		return false;
	}

	public virtual void RoundOver()
	{
		if (modeState != ModeState.ActiveRound)
		{
			Log.Warning($"Don't try and end a round that isn't active!");
			return;
		}

		if (roundCount < maxGameRounds)
		{
			SetModeState(ModeState.PostRound);
		}
		else
		{
			SetModeState(ModeState.PostGame);
		}
	}

	public virtual float GetClosestSpawnDistance(SpawnPoint[] spawnPoints, SpawnPoint spawnPoint)
	{
		float currentClosestSpawn = float.MaxValue;

		foreach (var otherSpawnPoint in spawnPoints)
		{
			if (spawnPoint == otherSpawnPoint)
			{
				continue;
			}

			float distCurrentSpawn = Vector3.DistanceBetween(spawnPoint.Transform.Position, otherSpawnPoint.Transform.Position);

			if (distCurrentSpawn >= currentClosestSpawn)
			{
				continue;
			}

			currentClosestSpawn = distCurrentSpawn;
		}

		return currentClosestSpawn;
	}

	public virtual bool HasMetRequiredPlayerCount()
	{
		if (PlayerInfo.all.Count < requiredPlayerCount)
		{
			return false;
		}

		return true;
	}

	public virtual bool CanRespawn(PlayerInfo playerInfo)
	{
		// I think this checks if the player is still in the game?
		if (playerInfo.Network.Owner != null)
		{

		}

		if (modeState == ModeState.WaitingForPlayers)
		{
			if (HasMetRequiredPlayerCount())
			{
				return false;
			}

			if (!playerInfo.isDead || playerInfo.deadTime < defaultRespawnTime)
			{
				return false;
			}
			return true;
		}

		if (modeState != ModeState.ActiveRound)
		{
			return false;
		}

		if (oneLifeOnly)
		{
			return false;
		}

		if (!playerInfo.isDead || playerInfo.deadTime < defaultRespawnTime)
		{
			return false;
		}

		return true;
	}

	[Broadcast]
	public virtual void TeleportSpectatorsToStartingPoint()
	{
		Spectator.TeleportToStartingPoint();
	}

	[Broadcast]
	public virtual void CleanupRoundInstances()
	{
		//Scene.RunEvent<IRoundInstance>(x => x.Cleanup());
		//IRoundInstance.Post(x => x.Cleanup());
		var restartables = Scene.GetAllComponents<IRoundInstance>();
		foreach (var restartable in restartables)
		{
			if (restartable == null)
				continue;

			restartable.Cleanup();
		}
	}
}
