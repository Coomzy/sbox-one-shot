
using System;

[Group("GMF")]
public class PlayerInfo : Component, Component.INetworkSpawn
{
	public static PlayerInfo local { get; private set; }
	static Dictionary<Guid, PlayerInfo> connectionToPlayerInfos = new Dictionary<Guid, PlayerInfo>();

	[HostSync, Property] public string displayName { get; set; }

	public ulong steamId { get; set; }
	[HostSync, Property] public Character character { get; set; }
	[Property] public Spectator spectator { get; set; }

	[HostSync, Property] public bool isKnifer { get; set; }
	[HostSync, Property] public bool isDead { get; set; } = false;

	protected override void OnStart()
	{
		connectionToPlayerInfos[Network.OwnerConnection.Id] = this;
		displayName = Network.OwnerConnection.DisplayName;

		//local = this;
	}

	public void OnNetworkSpawn(Connection owner)
	{
		local = this;
		CreateSpectatorPawn();
	}

	public void Rejoined()
	{
		CreateSpectatorPawn();
	}

	void CreateSpectatorPawn()
	{
		Log.Info($"CreateSpectatorPawn()");
		var spectatorPrefab = Game.IsRunningInVR ? WorldSettings.instance.spectatorVRPrefab : WorldSettings.instance.spectatorPrefab;
		var spectatorInst = spectatorPrefab.Clone().BreakPrefab().Components.Get<Spectator>();

		spectator = spectatorInst;
		spectator.GameObject.Name = $"Spectator Pawn";

		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();
		spectator.Transform.Position = spawnPoints[0].Transform.Position + (Vector3.Up * 50.0f);
		spectator.Transform.Rotation = spawnPoints[0].Transform.Rotation;
	}

	public static PlayerInfo ConnectionToPlayerInfo(Connection connection)
	{
		if (connectionToPlayerInfos.TryGetValue(connection.Id, out PlayerInfo playerInfo))
		{
			return playerInfo;
		}

		return null;
	}

	public static T ConnectionToPlayerInfo<T>(Connection connection) where T : PlayerInfo
	{
		if (connectionToPlayerInfos.TryGetValue(connection.Id, out PlayerInfo playerInfo))
		{
			return playerInfo as T;
		}

		return null;
	}
}
