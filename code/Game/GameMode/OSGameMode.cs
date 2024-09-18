using static Sandbox.Gizmo;
using System;
using Sandbox.Diagnostics;
using Sandbox.Network;
using Sandbox;
using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.VisualBasic;

public enum ModeState
{
	PreGame,
	PreRound,
	Playing,
	PostRound
}

public class OSGameMode : SingletonComponent<OSGameMode>, Component.INetworkListener, IHotloadManaged
{
	[Property] public GameObject osPawnPrefab { get; set; }

	public float preGameDelay = 3.0f;
	public float preRoundDelay = 3.0f;
	public float postRoundDelay = 3.0f;

	public SpawnPoint slasherSpawnPoint;
	public List<SpawnPoint> orderedSpawnPoints = new List<SpawnPoint>();

	[HostSync, Property] public int roundCount { get; set; }
	[HostSync, Property] public ModeState modeState { get; set; }
	public TimeSince stateTimeSince;

	int preGameSpawnIndex = 0;

	protected override void OnStart()
	{
		base.OnStart();

		if (IsProxy)
		{
			return;
		}

		FindSlasherSpawn();
		FindRunnerSpawns();

		/*Log.Info($"slasherSpawnPoint: '{slasherSpawnPoint.GameObject.Name}'");

		for (int i = 0; i < orderedSpawnPoints.Count; i++)
		{
			Log.Info( $"orderedSpawnPoints[{i}]: '{orderedSpawnPoints[i].GameObject.Name}' dist from knifer {Vector3.DistanceBetween(orderedSpawnPoints[i].Transform.Position, slasherSpawnPoint.Transform.Position)}" );
		}*/

		stateTimeSince = 0.0f;
	}

	public void OnPlayerConnected(OSPlayerInfo playerInfo, Connection channel)
	{

	}

	public void OnPlayerDisconnected(OSPlayerInfo playerInfo)
	{

	}

	OSCharacter CreatePawn(PlayerInfo playerInfo, SpawnPoint spawnPoint)
	{
		var pawnPrefab = GetPawnPrefabForPlayer(playerInfo);
		var osPawn = pawnPrefab.Clone(spawnPoint.Transform.Position, spawnPoint.Transform.Rotation).BreakPrefab().Components.Get<OSCharacter>();
		osPawn.GameObject.NetworkSpawn(playerInfo.Network.OwnerConnection);
		playerInfo.character = osPawn;
		osPawn.GameObject.Name = $"OSPawn ({playerInfo.displayName})";

		return osPawn;
	}

	GameObject GetPawnPrefabForPlayer(PlayerInfo playerInfo)
	{
		return osPawnPrefab;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (IsProxy)
		{
			return;
		}

		switch (modeState)
		{
			case ModeState.PreGame:
				PreGameUpdate();
				break;
			case ModeState.PreRound:
				PreRoundUpdate();
				break;
			case ModeState.Playing:
				PlayingUpdate();
				break;
			case ModeState.PostRound:
				PostRoundUpdate();
				break;
			default:
				Log.Error($"What state is this? modeState: {modeState}");
				break;
		}
	}

	// Mode State Starts
	void PreGameStart()
	{
		preGameSpawnIndex = 0;
	}

	void PreRoundStart()
	{
		var pawns = Scene.GetAllComponents<OSCharacter>();

		Log.Info($"PreRoundStart() osPawns: {pawns.Count()}");

		foreach (var pawn in pawns)
		{ 			
			pawn.GameObject.Destroy();
		}
	}

	void PlayingStart()
	{
		var newSlasherPlayerInfo = PickSlasher();
		roundCount++;

		//newSlasherPlayerInfo.lastRoundAsSlasher = roundCount;
		//newSlasherPlayerInfo.timesBeenSlasher++;

		var slasher = CreatePawn(newSlasherPlayerInfo, slasherSpawnPoint);
		//slasher.BecomeSlasher();

		var runnerplayerInfos = new List<PlayerInfo>(NetworkManager.instance.playerInfos);
		runnerplayerInfos.Shuffle();

		int spawnIndex = 0;
		foreach (OSPlayerInfo SLPI in runnerplayerInfos)
		{
			if (newSlasherPlayerInfo == SLPI)
				continue;

			var slasherPawn = CreatePawn(SLPI, orderedSpawnPoints[spawnIndex]);
			spawnIndex = (spawnIndex + 1) % orderedSpawnPoints.Count;
		}
	}

	OSPlayerInfo PickSlasher()
	{
		int highestRoundSinceSlasher = -1;

		foreach (OSPlayerInfo slasherPI in NetworkManager.instance.playerInfos)
		{
			int roundsSinceSlasher = 0;

			if (roundsSinceSlasher <= highestRoundSinceSlasher)
				continue;

			highestRoundSinceSlasher = roundsSinceSlasher;
		}

		List<OSPlayerInfo> slasherCandidates = new List<OSPlayerInfo>();

		int roundsSinceSlashRequirement = highestRoundSinceSlasher;

		foreach (OSPlayerInfo slasherPI in NetworkManager.instance.playerInfos)
		{
			int roundsSinceSlasher = roundCount - 0;

			if (roundsSinceSlasher < roundsSinceSlashRequirement)
				continue;

			slasherCandidates.Add(slasherPI);
		}

		return slasherCandidates.Random();
	}

	void PostRoundStart()
	{

	}

	// Mode State Updates
	void PreGameUpdate()
	{
		int playerCount = 0;
		foreach (OSPlayerInfo slasherPI in NetworkManager.instance.playerInfos)
		{
			if (slasherPI.character == null)
			{
				var slasherPawn = CreatePawn(slasherPI, orderedSpawnPoints[preGameSpawnIndex]);
				preGameSpawnIndex = (preGameSpawnIndex + 1) % orderedSpawnPoints.Count;
			}

			// I think this checks if the player is still in the game?
			if (slasherPI.Network.OwnerConnection != null)
			{
				playerCount++;
			}
		}

		if (stateTimeSince < preGameDelay)
		{
			return;
		}

		if (playerCount > 1) 
		{
			//SetModeState(ModeState.PreRound);
		}

	}

	void PreRoundUpdate()
	{
		if (stateTimeSince < preRoundDelay)
		{
			return;
		}

		SetModeState(ModeState.Playing);
	}

	void PlayingUpdate()
	{

	}

	void PostRoundUpdate()
	{

	}

	void SetModeState(ModeState state)
	{
		if (modeState == state)
			return;

		modeState = state;
		stateTimeSince = 0.0f;

		switch ( modeState )
		{
			case ModeState.PreGame:
				PreGameStart();
				break;
			case ModeState.PreRound:
				PreRoundStart();
				break;
			case ModeState.Playing:
				PlayingStart();
				break;
			case ModeState.PostRound:
				PostRoundStart();
				break;
		}
	}

	/*protected override void OnEnabled()
	{
		base.OnEnabled();

		if ( Components.TryGet<MapInstance>( out var mapInstance ) )
		{
			mapInstance.OnMapLoaded += RespawnPlayers;

			// already loaded
			if ( mapInstance.IsLoaded )
			{
				RespawnPlayers();
			}
		}
	}

	protected override void OnDisabled()
	{
		if ( Components.TryGet<MapInstance>( out var mapInstance ) )
		{
			mapInstance.OnMapLoaded -= RespawnPlayers;
		}

	}

	void RespawnPlayers()
	{
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();

		foreach ( var player in Scene.GetAllComponents<PlayerController>().ToArray() )
		{
			if ( player.IsProxy )
				continue;

			var randomSpawnPoint = Random.Shared.FromArray( spawnPoints );
			if ( randomSpawnPoint is null ) continue;

			player.Transform.Position = randomSpawnPoint.Transform.Position;

			if ( player.Components.TryGet<PlayerController>( out var pc ) )
			{
				pc.EyeAngles = randomSpawnPoint.Transform.Rotation.Angles();
			}

		}
	}*/

	void FindSlasherSpawn()
	{
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();

		SpawnPoint bestSpawnPoint = null;
		float furthestSpawnDistance = float.MinValue;

		foreach (var spawnPoint in spawnPoints)
		{
			float currentFurthestSpawn = GetClosestSpawnDistance(spawnPoints, spawnPoint);

			if (currentFurthestSpawn <= furthestSpawnDistance)
			{
				continue;
			}

			bestSpawnPoint = spawnPoint;
			furthestSpawnDistance = currentFurthestSpawn;
		}

		slasherSpawnPoint = bestSpawnPoint;
	}

	float GetClosestSpawnDistance(SpawnPoint[] spawnPoints, SpawnPoint spawnPoint)
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

	void FindRunnerSpawns()
	{
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();

		var spawnPointsWithDistance = new List<(SpawnPoint spawnPoint, float distance)>();

		foreach (var spawnPoint in spawnPoints)
		{
			if (spawnPoint == slasherSpawnPoint)
			{
				continue;
			}
			float distFromKnifeSpawn = Vector3.DistanceBetween(spawnPoint.Transform.Position, slasherSpawnPoint.Transform.Position);
			spawnPointsWithDistance.Add((spawnPoint, distFromKnifeSpawn));
		}

		var orderedSpawnPointsWithDistance = spawnPointsWithDistance.OrderByDescending( sp => sp.distance);

		orderedSpawnPoints.Clear();
		foreach (var (spawnPoint, distance) in orderedSpawnPointsWithDistance)
		{
			orderedSpawnPoints.Add(spawnPoint);
		}
	}
}
