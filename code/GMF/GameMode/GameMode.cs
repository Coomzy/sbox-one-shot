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
	ReadyPhase,
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
	[Group("Config - Delays"), Order(1), Property] public float readyPhaseDelay { get; set; } = 3.0f;
	[Group("Config - Delays"), Order(1), Property] public float roundConditionMetDelay { get; set; } = 1.0f;
	[Group("Config - Delays"), Order(1), Property] public float postRoundDelay { get; set; } = 2.0f;
	[Group("Config - Delays"), Order(1), Property] public float postGameDelay { get; set; } = 3.0f;

	[Group("Config"), Order(2), Property] public int requiredPlayerCount { get; set; } = 2;
	[Group("Config"), Order(2), Property] public int maxGameRounds { get; set; } = 10;
	[Group("Config"), Order(2), Property] public float? roundTime { get; set; } = 100.0f;
	[Group("Config"), Order(2), Property] public bool oneLifeOnly { get; set; } = false;
	[Group("Config"), Order(2), Property] public float defaultRespawnTime { get; set; } = 3.0f;

	public Dictionary<SpawnPoint, float> spawnPointToLastUsedTime = new Dictionary<SpawnPoint, float>();

	[Group("Runtime"), Order(100), HostSync, Property] public int roundCount { get; private set; }
	[Group("Runtime"), Order(100), HostSync, Property] public ModeState modeState { get; private set; }
	[Group("Runtime"), Order(100), HostSync, Property] public TimeSince stateTime { get; private set; }

	[Group("Runtime"), Order(100), HostSync, Property] public PlayerInfo lastWinner { get; set; }

	[Group("Runtime"), Order(100), Property] public TimeSince metPlayerReqTime { get; private set; }

	[Group("Runtime"), Order(100), Property] SpawnPoint[] allSpawnPoints { get; set; }

	[Group("Runtime"), Order(100), Property] TimeSince? timeSinceRoundConditionMet { get; set; } = null;
	[Group("Runtime"), Order(100), Property] int preGameSpawnIndex { get; set; } = 0;

	[Group("Runtime"), Order(100), Property] public float remainingStateTime
	{
		get
		{			
			return readyPhaseDelay - stateTime;
			//return 0.0f;
		}
	}

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

	public virtual (Vector3 spawnPos, Rotation spawnRot) GetSpawnFor(PlayerInfo playerInfo)
	{
		return (Vector3.Zero, Rotation.Identity);
	}

	protected virtual GameObject GetPawnPrefabForPlayer(PlayerInfo playerInfo)
	{
		return pawnPrefab;
	}

	// Pre Game
	protected virtual void PreGameStart()
	{
		preGameSpawnIndex = 0;
		roundCount = 0;

		IGameEvents.Post(x => x.GameStart());
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
		roundCount++;
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

		SetModeState(ModeState.ReadyPhase);
	}

	// Ready Phase
	protected virtual void ReadyPhaseStart()
	{
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();
		spawnPoints.Shuffle();
		int playerCount = 0;
		foreach (var playerInfo in PlayerInfo.allActive)
		{
			CreatePawn(playerInfo, spawnPoints[playerCount]);
			playerCount++;
		}
	}

	protected virtual void ReadyPhaseUpdate()
	{
		if (stateTime < readyPhaseDelay)
		{
			return;
		}

		SetModeState(ModeState.ActiveRound);
	}
	

	// Active Round
	protected virtual void ActiveRoundStart()
	{
		timeSinceRoundConditionMet = null;
	}

	protected virtual void ActiveRoundUpdate()
	{
		if (timeSinceRoundConditionMet.HasValue)
		{
			if (timeSinceRoundConditionMet.Value >= roundConditionMetDelay)
			{
				RoundOver();
			}
			return;
		}

		if (RoundEndCondition())
		{
			timeSinceRoundConditionMet = 0;
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
		CheckGameWinners();
	}

	protected virtual void PostGameUpdate()
	{
		if (stateTime < postGameDelay)
		{
			return;
		}

		SetModeState(ModeState.PreGame);
	}

	public virtual void TryRespawnPlayers()
	{
		// TODO: Make this way less shitty, it doesn't effect one shot though
		foreach (var playerInfo in PlayerInfo.allActive)
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
			return PlayerInfo.allAlive.Count < 2;
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

		CheckRoundWinners();

		if (roundCount < maxGameRounds)
		{
			SetModeState(ModeState.PostRound);
		}
		else
		{
			SetModeState(ModeState.PostGame);
		}
	}

	public virtual void CheckRoundWinners()
	{
		var winnerPlayerInfo = PickRoundWinner();

		if (IsFullyValid(winnerPlayerInfo))
		{
			winnerPlayerInfo.OnScoreRoundWin();
		}
	}

	public virtual PlayerInfo PickRoundWinner()
	{
		PlayerInfo winner = null;
		if (oneLifeOnly)
		{
			foreach (var playerInfo in PlayerInfo.allActive)
			{
				if (playerInfo.isDead)
					continue;

				// There can only be one winner
				if (winner != null)
				{
					winner = null;
					break;
				}

				winner = playerInfo;
			}
		}
		else
		{
			int bestPlayerKills = -1;
			int bestPlayerDeaths = 0;
			foreach (var playerInfo in PlayerInfo.allActive)
			{
				if (playerInfo.killsRound < bestPlayerKills)
					continue;

				if (playerInfo.killsRound == bestPlayerKills && playerInfo.deathsRound < bestPlayerDeaths)
					continue;

				winner = playerInfo;
				bestPlayerKills = playerInfo.killsRound;
				bestPlayerDeaths = playerInfo.deathsRound;
			}
		}

		return winner;
	}

	public virtual void CheckGameWinners()
	{
		var winnerPlayerInfo = PickGameWinner();

		if (IsFullyValid(winnerPlayerInfo))
		{
			winnerPlayerInfo.OnScoreRoundWin();
			lastWinner = winnerPlayerInfo;
		}
		else
		{
			lastWinner = null;
		}
	}

	public virtual PlayerInfo PickGameWinner()
	{
		var sortedPlayers = SortPlayersByWinning(PlayerInfo.allActive);

		if (sortedPlayers.Count <= 0)
			return null;

		return sortedPlayers[0];
	}

	public virtual List<PlayerInfo> SortPlayersByWinning(List<PlayerInfo> players)
	{
		return players
			.OrderByDescending(player => player.wins)
			.ThenByDescending(player => player.kills)
			.ThenBy(player => player.deaths)
			.ToList();
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
		if (PlayerInfo.allActive.Count < requiredPlayerCount)
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

		if (modeState != ModeState.ActiveRound && modeState != ModeState.ReadyPhase)
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
		//Scene.RunEvent<IRoundEvents>(x => x.Cleanup());
		IRoundEvents.Post(x => x.RoundCleanup());
		/*var restartables = Scene.GetAllComponents<IRoundEvents>();
		foreach (var restartable in restartables)
		{
			if (restartable == null)
				continue;

			restartable.RoundCleanup();
		}*/
	}

	void SetModeState(ModeState state)
	{
		if (modeState == state)
			return;

		modeState = state;
		stateTime = 0.0f;

		switch (modeState)
		{
			case ModeState.PreGame:
				PreGameStart();
				break;
			case ModeState.PreRound:
				PreRoundStart();
				break;
			case ModeState.ReadyPhase:
				ReadyPhaseStart();
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

	protected override void OnUpdate()
	{
		base.OnUpdate();

		Debuggin.ToScreen($"modeState: {modeState}", color: Color.Red);
		Debuggin.ToScreen($"stateTime: {stateTime}", color: Color.Red);

		UpdateModeState();
	}

	protected virtual void UpdateModeState()
	{
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
			case ModeState.ReadyPhase:
				ReadyPhaseUpdate();
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
}
