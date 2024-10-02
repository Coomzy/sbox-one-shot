
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Network;
using System;
using static Sandbox.Gizmo;

public class NetworkManager : Component, Component.INetworkListener
{
	public static NetworkManager instance { get; private set; }

	protected override void OnAwake()
	{
		instance = this;

		// Yikes, does this cause any issues?
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

		if (!playerInfo.Network.Active)
		{
			playerInfo.GameObject.NetworkSpawn(connection);

		}
		else
		{
			playerInfo.Network.AssignOwnership(connection);
		}

		if (rejoined)
		{
			playerInfo.Rejoined();
			GameMode.instance.OnPlayerRejoined(playerInfo);
		}
		else
		{
			GameMode.instance.OnPlayerConnected(playerInfo);
		}
	}

	public void OnConnected(Connection connection)
	{
		Log.Info($"Player '{connection.DisplayName}' connected");
	}

	public void OnDisconnected(Connection connection)
	{
		Log.Info($"Player '{connection.DisplayName}' disconnected");

		if (!PlayerInfo.TryFromConnection(connection, out var leavingPlayerInfo))
		{
			Log.Warning($"Could not find PlayerInfo for'{connection.DisplayName}' when they disconnected");
			return;
		}

		leavingPlayerInfo.Disconnected();

		GameMode.instance.OnPlayerDisconnected(leavingPlayerInfo);
	}

	public void OnBecameHost(Connection previousHost)
	{
		Log.Info($"You are now host, player '{previousHost.DisplayName}' disconnected");
		//PlayerInfo.local.OnBecameHost(PlayerInfo.FromConnection(previousHost));
	}

	PlayerInfo CreatePlayerInfo(Connection connection = null)
	{
		//var prefab = GMFSettings.instance.playerInfoPrefab;
		var prefab = WorldInfo.instance.playerInfoPrefab;
		Assert.True(IsFullyValid(prefab), "Could not spawn player as no prefab assigned.");

		var playerInfo = prefab.Clone().BreakPrefab().Components.Get<PlayerInfo>();
		if (!IsFullyValid(playerInfo))
			return null;

		playerInfo.steamId = connection.SteamId;
		playerInfo.role = GMFSettings.instance.GetRoleFromID(connection.SteamId);
		playerInfo.GameObject.Name = $"PlayerInfo ({connection.DisplayName})";
		playerInfo.GameObject.Network.SetOrphanedMode(NetworkOrphaned.ClearOwner);

		return playerInfo;
	}

	PlayerInfo FindExistingPlayerInfo(Connection connection = null)
	{
		var possiblePlayerInfo = PlayerInfo.allInactive.FirstOrDefault(x =>
		{
			return IsFullyValid(x) && x.Network.Owner == null && x.steamId == connection.SteamId;
		});

		return possiblePlayerInfo;
	}

	[Button]
	public void CreateLobby()
	{		
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
