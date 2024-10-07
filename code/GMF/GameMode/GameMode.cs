using static Sandbox.Gizmo;
using System;
using Sandbox.Diagnostics;
using Sandbox.Network;
using Sandbox;
using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.VisualBasic;
using Sandbox.UI;
using badandbest.Sprays;

public enum ModeState
{
	PreMatch,
	PreRound,
	WaitingForPlayers,
	ReadyPhase,
	ActiveRound,
	PostRound,
	PostMatch,
	PostMatchResults
}

public class GameMode : Component, Component.INetworkListener, IHotloadManaged
{
	public static GameMode instance { get; private set; }

	[Group("Config - Delays"), Order(1), Property] public float initalPreMatchDelay { get; set; } = 2.0f;
	[Group("Config - Delays"), Order(1), Property] public float preMatchDelay { get; set; } = 1.0f;
	[Group("Config - Delays"), Order(1), Property] public float preRoundDelay { get; set; } = 3.0f;
	[Group("Config - Delays"), Order(1), Property] public float waitingForPlayersDelay { get; set; } = 3.0f;
	[Group("Config - Delays"), Order(1), Property] public float readyPhaseDelay { get; set; } = 3.0f;
	[Group("Config - Delays"), Order(1), Property] public float roundConditionMetDelay { get; set; } = 1.0f;
	[Group("Config - Delays"), Order(1), Property] public float postRoundDelay { get; set; } = 3.0f;
	[Group("Config - Delays"), Order(1), Property] public float postMatchDelay { get; set; } = 3.0f;
	[Group("Config - Delays"), Order(1), Property] public float postMatchResultsDelay { get; set; } = 7.5f;

	[Group("Config"), Order(2), Property, HostSync] public int requiredPlayerCount { get; set; } = 2;
	[Group("Config"), Order(2), Property, HostSync] public int maxMatchRounds { get; set; } = 10;
	[Group("Config"), Order(2), Property, HostSync] public float? roundTime { get; set; } = 180.0f;
	[Group("Config"), Order(2), Property, HostSync] public bool oneLifeOnly { get; set; } = false;
	[Group("Config"), Order(2), Property, HostSync] public float defaultRespawnTime { get; set; } = 3.0f;

	[Group("Runtime"), Order(100), Property, HostSync] public int roundCount { get; private set; }
	[Group("Runtime"), Order(100), Property, HostSync] public int matchCount { get; private set; }
	[Group("Runtime"), Order(100), Property, HostSync, Change("OnRep_modeState")] public ModeState modeState { get; private set; }
	[Group("Runtime"), Order(100), Property, HostSync] public TimeSince stateTime { get; private set; }

	[Group("Runtime"), Order(100), HostSync, Property] public PlayerInfo lastWinner { get; set; }

	[Group("Runtime"), Order(100), Property] public TimeSince metPlayerReqTime { get; private set; }

	[Group("Runtime"), Order(100), Property] public TimeSince? delayedRoundConditionMet { get; set; } = null;

	[ConVar] public static bool debug_gamemode_state { get; set; }
	[ConVar] public static bool debug_gamemode_connections { get; set; }

	[Group("Runtime"), Order(100), Property] 
	public float remainingStateTime
	{
		get
		{
			var maxTime = 0.0f;

			switch (modeState)
			{
				case ModeState.ReadyPhase:
					maxTime = readyPhaseDelay;
					break;
				case ModeState.ActiveRound:
					if (roundTime.HasValue)
					{
						maxTime = roundTime.Value;
					}
					break;
			}

			var remainingTime = maxTime - stateTime;
			remainingTime = MathY.Max(remainingTime, 0);
			return remainingTime;
		}
	}

	void OnRep_modeState(ModeState oldValue, ModeState newValue)
	{
		IGameModeEvents.Post(x => x.ModeStateChange(oldValue, newValue));
	}

	protected override void OnAwake()
	{
		instance = this;
	}

	protected override void OnStart()
	{
		if (IsProxy)
			return;

		stateTime = 0.0f;
		PreMatchStart();
	}

	protected override void OnDestroy()
	{
		instance = null;
	}

	public virtual void OnPlayerConnected(PlayerInfo playerInfo)
	{
		// Assign team and all the bollocks
		if (debug_gamemode_connections)
		{
			Log.Info($"GameMode::OnPlayerConnected() player: {playerInfo?.displayName}");
		}

		playerInfo.SetSpectateMode(GetSpectateModeForModeState());

		//Log.Info($"GameMode::OnPlayerConnected() modeState: {modeState} player: {playerInfo.spectateMode}");
	}

	public virtual void OnPlayerRejoined(PlayerInfo playerInfo)
	{
		if (debug_gamemode_connections)
		{
			Log.Info($"GameMode::OnPlayerRejoined() player: {playerInfo?.displayName}");
		}

		playerInfo.SetSpectateMode(GetSpectateModeForModeState());

		//Log.Info($"GameMode::OnPlayerRejoined() modeState: {modeState} player: {playerInfo.spectateMode}");
	}

	public virtual void OnPlayerDisconnected(PlayerInfo playerInfo)
	{
		if (debug_gamemode_connections)
		{
			Log.Info($"GameMode::OnPlayerDisconnected() player: {playerInfo?.displayName}");
		}
	}

	// TODO: I renamed Pawn to Character, but I want to go back to Pawn at some point
	public virtual Character CreatePawn(PlayerInfo playerInfo, GMFSpawnPoint spawnPoint)
	{
		spawnPoint.lastUsed = 0;
		spawnPoint.lastUsedBy = playerInfo;
		return CreatePawn(playerInfo, spawnPoint.WorldPosition, spawnPoint.WorldRotation);
	}

	public virtual Character CreatePawn(PlayerInfo playerInfo, Vector3 spawnPos, Rotation spawnRot)
	{
		var pawnPrefab = GetPawnPrefabForPlayer(playerInfo);
		var pawn = pawnPrefab.Clone(spawnPos, spawnRot).BreakPrefab().Components.Get<Character>();
		pawn.GameObject.NetworkSpawn(playerInfo.Network.Owner);
		playerInfo.Possess(pawn);
		pawn.GameObject.Name = $"Pawn ({playerInfo.displayName})";

		return pawn;
	}

	protected virtual GameObject GetPawnPrefabForPlayer(PlayerInfo playerInfo)
	{
		return WorldInfo.instance.pawnPrefab;

		// NOTE: GameObject in GameResources have deserialization errors in this project...
		//return GMFSettings.instance.pawnPrefab;
	}

	// Pre Match
	protected virtual void PreMatchStart()
	{
		matchCount++;
		roundCount = 1;

		IGameModeEvents.Post(x => x.MatchStart());
	}

	protected virtual void PreMatchUpdate()
	{
		var delay = matchCount > 1 ? preMatchDelay : initalPreMatchDelay;
		if (stateTime < delay)
		{
			return;
		}

		SetModeState(ModeState.PreRound);
	}

	// Pre Round
	protected virtual void PreRoundStart()
	{
		RoundCleanup();
	}

	protected virtual void PreRoundUpdate()
	{
		if (stateTime < preRoundDelay)
		{
			return;
		}

		if (HasMetRequiredPlayerCount())
		{
			SetModeState(ModeState.ReadyPhase);
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

	// Ready Phase
	protected virtual void ReadyPhaseStart()
	{
		var validSpawnPoints = WorldInfo.instance.spawnPointsGMF.Where(x => x.Enabled);
		var validSpawnPointsList = validSpawnPoints.ToList();
		validSpawnPointsList.Shuffle();

		int playerCount = 0;
		foreach (var playerInfo in PlayerInfo.allActive)
		{
			CreatePawn(playerInfo, validSpawnPointsList[playerCount]);
			playerCount++;
		}
	}

	protected virtual void ReadyPhaseUpdate()
	{
		if (!HasMetRequiredPlayerCount())
		{
			SetModeState(ModeState.WaitingForPlayers);	
			return;
		}

		if (stateTime < readyPhaseDelay)
		{
			return;
		}

		SetModeState(ModeState.ActiveRound);
	}
	

	// Active Round
	protected virtual void ActiveRoundStart()
	{
		delayedRoundConditionMet = null;
	}

	protected virtual void ActiveRoundUpdate()
	{
		//TryRespawnPlayers();
		//if (PlayerInfo.allAlive.Count == 1)
		//return;

		if (RoundEndCondition())
		{
			RoundOver();
			return;
		}

		if (delayedRoundConditionMet.HasValue)
		{
			return;
		}

		TryRespawnPlayers();
	}

	// Post Round
	protected virtual void PostRoundStart()
	{
		AnnouncerSystem.BroadcastQueueOverrideSound("announcer.round.over");
		RoundOverClient();
	}

	protected virtual void PostRoundUpdate()
	{
		if (stateTime < postRoundDelay)
		{
			return;
		}

		SetModeState(ModeState.PreRound);
	}

	// Post Match
	protected virtual void PostMatchStart()
	{
		AnnouncerSystem.BroadcastQueueOverrideSound("announcer.game.over");
		RoundOverClient();
		MatchOverClient();
		CheckMatchWinners();
	}

	protected virtual void PostMatchUpdate()
	{
		if (stateTime < postMatchDelay)
		{
			return;
		}

		SetModeState(ModeState.PostMatchResults);
	}

	// Post Match Results
	protected virtual void PostMatchResultsStart()
	{
		RoundCleanup();
	}

	protected virtual void PostMatchResultsUpdate()
	{
		if (stateTime < postMatchResultsDelay)
		{
			return;
		}

		SetModeState(ModeState.PreMatch);
	}

	public virtual void TryRespawnPlayers()
	{
		foreach (var playerInfo in PlayerInfo.allActive)
		{
			if (!CanRespawn(playerInfo))
				continue;

			var spawnPoint = PickSpawnForPlayer(playerInfo);
			if (spawnPoint == null)
			{
				continue;
			}

			CreatePawn(playerInfo, spawnPoint);
		}
	}

	public virtual GMFSpawnPoint PickSpawnForPlayer(PlayerInfo playerInfo)
	{
		if (!IsFullyValid(WorldInfo.instance?.spawnPointsGMF))
			return null;

		var weightedSpawns = new WeightedRandom<GMFSpawnPoint>();

		foreach (var spawnPoint in WorldInfo.instance.spawnPointsGMF)
		{
			var rating = GetSpawnRatingForPlayer(spawnPoint, playerInfo);
			weightedSpawns.Add(spawnPoint, rating);
		}

		var randomSpawn = weightedSpawns.Random();
		return randomSpawn;
	}

	public virtual int GetSpawnRatingForPlayer(GMFSpawnPoint spawnPoint, PlayerInfo playerInfo)
	{
		if (!IsFullyValid(spawnPoint) || !spawnPoint.Enabled)
			return 0;

		var lowestDistanceFromSpawn = 99999.9f;
		foreach (var checkingPlayerInfo in PlayerInfo.allAlive)
		{
			if (!IsFullyValid(checkingPlayerInfo?.character))
				continue;

			if (checkingPlayerInfo == playerInfo)
				continue;

			var distance = Vector3.DistanceBetween(spawnPoint.WorldPosition, checkingPlayerInfo.character.WorldPosition);

			if (distance >= lowestDistanceFromSpawn)
				continue;

			lowestDistanceFromSpawn = distance;
		}

		return MathX.CeilToInt(lowestDistanceFromSpawn);
	}

	public virtual bool RoundEndCondition()
	{
		if (delayedRoundConditionMet.HasValue && delayedRoundConditionMet.Value >= roundConditionMetDelay)
		{
			return true;
		}

		if (oneLifeOnly)
		{
			if (!delayedRoundConditionMet.HasValue)
			{
				if (PlayerInfo.allAlive.Count < 2)
				{
					delayedRoundConditionMet = 0;
				}
			}
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
		roundCount++;

		if (roundCount < maxMatchRounds)
		{
			SetModeState(ModeState.PostRound);
		}
		else
		{
			SetModeState(ModeState.PostMatch);
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
			if (PlayerInfo.allAlive.Count == 1)
			{
				winner = PlayerInfo.allAlive[0];
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

	[Broadcast]
	protected virtual void RoundOverClient()
	{
		Sandbox.Services.Stats.Increment(Stat.PLAYED_ROUNDS, 1);
	}

	[Broadcast]
	protected virtual void MatchOverClient()
	{
		Sandbox.Services.Stats.Increment(Stat.PLAYED_MATCHES, 1);
	}

	public virtual void CheckMatchWinners()
	{
		var winnerPlayerInfo = PickMatchWinner();

		if (IsFullyValid(winnerPlayerInfo))
		{
			winnerPlayerInfo.OnScoreMatchWin();
			lastWinner = winnerPlayerInfo;
		}
		else
		{
			lastWinner = null;
		}
	}

	public virtual PlayerInfo PickMatchWinner()
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

			float distCurrentSpawn = Vector3.DistanceBetween(spawnPoint.WorldPosition, otherSpawnPoint.WorldPosition);

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

	public virtual bool ShouldShowScoreboard()
	{
		if (modeState == ModeState.PostMatchResults)
		{
			return true;
		}

		if (roundCount > 1)
		{
			if (modeState == ModeState.PreRound)
			{
				return true;
			}
		}

		return false;
	}

	protected virtual void RoundCleanup()
	{
		foreach (var playerInfo in PlayerInfo.all)
		{
			playerInfo.DestroyPawn();
		}

		CleanupRoundInstances();
		TeleportSpectatorsToStartingPoint();
	}

	public virtual SpectateMode GetSpectateModeForModeState()
	{
		switch (modeState)
		{
			case ModeState.PreMatch:
			case ModeState.PreRound:
			//case ModeState.WaitingForPlayers:
			case ModeState.PostRound:
			case ModeState.PostMatch:
			case ModeState.PostMatchResults:
				return SpectateMode.Viewpoint;

			case ModeState.ActiveRound:
			case ModeState.ReadyPhase:
			case ModeState.WaitingForPlayers:
				return SpectateMode.ThirdPerson;
		}

		return SpectateMode.Viewpoint;
	}

	[Broadcast]
	public virtual void TeleportSpectatorsToStartingPoint()
	{
		PlayerInfo.local.SetSpectateMode(SpectateMode.Viewpoint);
		//Spectator.instance.SetMode(SpectateMode.Viewpoint);
		//Spectator.TeleportToStartingPoint();
	}

	[Broadcast]
	public virtual void CleanupRoundInstances()
	{
		//Scene.RunEvent<IGameModeEvents>(x => x.Cleanup());
		IGameModeEvents.Post(x => x.RoundCleanup());
		/*var restartables = Scene.GetAllComponents<IGameModeEvents>();
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
			case ModeState.PreMatch:
				PreMatchStart();
				break;
			case ModeState.PreRound:
				PreRoundStart();
				break; 
			case ModeState.WaitingForPlayers:
				WaitingForPlayersStart();
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
			case ModeState.PostMatch:
				PostMatchStart();
				break;
			case ModeState.PostMatchResults:
				PostMatchResultsStart();
				break;
			default:
				Log.Warning($"modeState '{modeState}' is missing a case statement in SetModeState()");
				break;
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (debug_gamemode_state)
		{
			Debuggin.ToScreen($"modeState: {modeState}", color: Color.Red);
			Debuggin.ToScreen($"stateTime: {stateTime}", color: Color.Red);
		}

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
			case ModeState.PreMatch:
				PreMatchUpdate();
				break;
			case ModeState.PreRound:
				PreRoundUpdate();
				break;
			case ModeState.WaitingForPlayers:
				WaitingForPlayersUpdate();
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
			case ModeState.PostMatch:
				PostMatchUpdate();
				break;
			case ModeState.PostMatchResults:
				PostMatchResultsUpdate();
				break;
			default:
				Log.Error($"What state is this? modeState: {modeState}");
				break;
		}
	}
}
