
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Network;
using System;
using static Sandbox.Gizmo;

public class NetworkManager : Component, Component.INetworkListener
{
	public static NetworkManager instance { get; private set; }

	public List<PlayerInfo> playerInfos { get; set; } = new List<PlayerInfo>();
	public List<PlayerInfo> inactivePlayerInfos { get; set; } = new List<PlayerInfo>();

	protected override void OnAwake()
	{
		instance = this;
		PlayerInfo.all.Clear();
	}

	protected override void OnDestroy()
	{
		instance = null;
		base.OnDestroy();
	}

	protected override void OnStart()
	{
		if (!Networking.IsActive)
		{
			Networking.CreateLobby();
		}
	}

	protected override void OnUpdate()
	{
		Debuggin.ToScreen($"Connection.All: {Connection.All.Count}");
		foreach (var connection in Connection.All)
		{
			Debuggin.ToScreen($"connection: {connection.DisplayName} - {connection.Id}");
		}
	}

	public void OnActive(Connection connection)
	{
		Log.Info($"Player '{connection.DisplayName}' is becoming active");

		//Log.Info($"NetworkManager::OnActive() 1 connection: {connection} connection avatar: {connection.GetUserData("avatar")}");

		var playerInfo = FindExistingPlayerInfo(connection);
		bool rejoined = playerInfo != null;
		if (!rejoined)
		{
			playerInfo = CreatePlayerInfo(connection);
		}

		if (!playerInfo.IsValid())
		{
			throw new Exception($"Something went wrong when trying to create PlayerInfo for {connection.DisplayName}");
		}

		// Either spawn over network, or claim ownership
		if (!playerInfo.Network.Active)
		{
			playerInfo.GameObject.NetworkSpawn(connection);

		}
		else
		{
			playerInfo.Network.AssignOwnership(connection);
		}

		if (inactivePlayerInfos.Contains(playerInfo))
		{
			inactivePlayerInfos.Remove(playerInfo);
		}

		playerInfos.Add(playerInfo);

		if (rejoined)
		{
			playerInfo.Rejoined();
		}

		//GameMode.instance.OnPlayerConnected(playerInfo, channel);
	}

	public void OnConnected(Connection connection)
	{
		Log.Info($"Player '{connection.DisplayName}' connected");
	}

	public void OnDisconnected(Connection connection)
	{		
		Log.Info($"Player '{connection.DisplayName}' disconnected");
		PlayerInfo pi = PlayerInfo.FromConnection(connection);
		if (!PlayerInfo.TryFromConnection(connection, out var leavingPlayerInfo))
		{
			return;
		}

		leavingPlayerInfo.Diconnected();
		playerInfos.Remove(leavingPlayerInfo);
		inactivePlayerInfos.Remove(leavingPlayerInfo);
	}

	public void OnBecameHost(Connection previousHost)
	{
		Log.Info($"You are now host, player '{previousHost.DisplayName}' disconnected");
		PlayerInfo.local.OnBecameHost(PlayerInfo.FromConnection(previousHost));
	}

	PlayerInfo FindExistingPlayerInfo(Connection channel = null )
	{
		var possiblePlayerInfo = inactivePlayerInfos.FirstOrDefault( x =>
		{
			// A candidate player state has no owner.
			return x.Network.Owner == null && x.steamId == channel.SteamId;
		} );

		if (possiblePlayerInfo.IsValid())
		{
			//Log.Warning( $"Found existing player state for {channel.SteamId} that we can re-use. {possiblePlayerInfo}" );			
			return possiblePlayerInfo;
		}

		return null;
	}

	PlayerInfo CreatePlayerInfo(Connection channel = null)
	{
		var prefab = WorldSettings.instance.playerInfoPrefab;
		Assert.True(prefab.IsValid(), "Could not spawn player as no prefab assigned.");

		var playerInfo = prefab.Clone().BreakPrefab().Components.Get<PlayerInfo>();
		if (!playerInfo.IsValid())
			return null;

		playerInfo.steamId = channel.SteamId;
		playerInfo.GameObject.Name = $"PlayerInfo ({channel.DisplayName})";
		playerInfo.GameObject.Network.SetOrphanedMode( NetworkOrphaned.ClearOwner );

		return playerInfo;
	}

	[Button]
	public void CreateLobby()
	{
		Log.Info($"CreateLobby() IsActive: {Networking.IsActive}");
		
		if (!Networking.IsActive)
		{
			Networking.CreateLobby();
		}
	}

	[Button]
	public void DisconnectFromLobby()
	{
		if (Networking.IsActive)
		{
			Networking.Disconnect();
		}
	}
}
