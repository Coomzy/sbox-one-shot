
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Network;
using System;
using static Sandbox.Gizmo;

public class WorldInfo : Component, Component.INetworkListener
{
	public static WorldInfo instance { get; private set; }

	[Group("Setup"), Order(-100), Property] public MapInstance mapInstance { get; set; }
	[Group("Setup"), Order(-100), Property] public MapCollider mapCollider { get; set; }

	[Group("Setup - Prefabs"), Order(-90), Property] public GameObject playerInfoPrefab { get; set; }
	[Group("Setup - Prefabs"), Order(-90), Property] public GameObject pawnPrefab { get; set; }
	[Group("Setup - Prefabs"), Order(-90), Property] public GameObject spectatorPrefab { get; set; }
	[Group("Setup - Prefabs"), Order(-90), Property] public GameObject spectatorVRPrefab { get; set; }

	[Group("Config"), Property] public bool wrapOriginalSpawnPointsWithGMF { get; set; }
	[Group("Config"), Property] public float killZ { get; set; } = -500.0f;

	[Group("Runtime"), Order(100), Property] public SpectateViewpoint[] spectateViewpoints { get; set; }
	[Group("Runtime"), Order(100), Property] public SpawnPoint[] spawnPoints { get; set; }
	[Group("Runtime"), Order(100), Property] public GMFSpawnPoint[] spawnPointsGMF { get; set; }

	protected override void OnAwake()
	{
		instance = this;

		var newSpectatorPrefab = Game.IsRunningInVR ? spectatorVRPrefab : spectatorPrefab;
		var spectatorInst = newSpectatorPrefab.Clone();
		spectatorInst.Name = $"Spectator";

		if (!IsFullyValid(mapInstance))
		{
			mapInstance = Scene.GetComponentInChildren<MapInstance>(true);
		}

		if (IsFullyValid(mapInstance))
		{
			mapInstance.OnMapLoaded += OnMapLoaded;

			if (mapInstance.IsLoaded)
			{
				OnMapLoaded();
			}
		}
		else
		{
			CaptureData();
		}
	}

	protected override void OnDestroy()
	{
		instance = null;

		if (IsFullyValid(mapInstance))
		{
			mapInstance.OnMapLoaded -= OnMapLoaded;
		}

		base.OnDestroy();
	}

	[Group("Buttons"), Order(-1000), Button]
	public virtual void CaptureData()
	{
		spectateViewpoints = Scene.GetComponentsInChildren<SpectateViewpoint>(true).ToArray();
		spawnPoints = Scene.GetComponentsInChildren<SpawnPoint>(true).ToArray();

		if (wrapOriginalSpawnPointsWithGMF)
		{
			foreach (var spawnPoint in spawnPoints)
			{
				spawnPoint.AddComponent<GMFSpawnPoint>();
			}
		}
		spawnPointsGMF = Scene.GetComponentsInChildren<GMFSpawnPoint>(true).ToArray();

		if (spawnPointsGMF.Length < 1)
		{
			Log.Warning($"No GMFSpawnPoint's found! This is not good, use wrapOriginalSpawnPointsWithGMF if you want to use spawns from a map instance");
		}

		var mapObjects = Scene.GetComponentsInChildren<MapObjectComponent>(true).ToArray();
		foreach (var mapObject in mapObjects)
		{
			ParseMapObject(mapObject);
		}
	}

	protected virtual void ParseMapObject(MapObjectComponent mapObject)
	{
		if (mapObject.GameObject.Name.ToLower().Contains("jumppad"))
		{
			mapObject.GetOrAddComponent<JumpPad>();
		}
	}

	protected virtual void OnMapLoaded()
	{
		CaptureData();
	}
}
