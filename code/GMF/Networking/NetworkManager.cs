
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Network;
using System;
using static Sandbox.Gizmo;

public class NetworkManager : SingletonComponent<NetworkManager>, Component.INetworkListener
{
	public List<PlayerInfo> playerInfos { get; set; } = new List<PlayerInfo>();

	protected override void OnStart()
	{
		if (!GameNetworkSystem.IsActive)
		{
			GameNetworkSystem.CreateLobby();
		}
	}

	public void OnActive(Connection channel)
	{
		Log.Info($"Player '{channel.DisplayName}' is becoming active");

		var playerInfo = FindExistingPlayerInfo(channel);
		bool rejoined = playerInfo != null;
		if (!rejoined)
		{
			playerInfo = CreatePlayerInfo(channel);
		}

		if (!playerInfo.IsValid())
		{
			throw new Exception($"Something went wrong when trying to create PlayerInfo for {channel.DisplayName}");
		}

		// Either spawn over network, or claim ownership
		if (!playerInfo.Network.Active)
		{
			playerInfo.GameObject.NetworkSpawn(channel);

		}
		else
		{
			playerInfo.Network.AssignOwnership(channel);
		}

		playerInfos.Add(playerInfo);

		if (rejoined)
		{
			playerInfo.Rejoined();
		}

		//GameMode.instance.OnPlayerConnected(playerInfo, channel);
		//OnPlayerJoined( PlayerInfo, channel );
	}

	public void OnPlayerJoined(PlayerInfo playerInfo, Connection channel)
	{
		// Dunno if we need both of these events anymore? But I'll keep them for now.
		//Scene.Dispatch( new PlayerConnectedEvent( playerInfo ) );

		// Either spawn over network, or claim ownership
		if (!playerInfo.Network.Active)
		{
			playerInfo.GameObject.NetworkSpawn( channel );
		}
		else
		{
			playerInfo.Network.AssignOwnership( channel );
		}

		//playerInfo.HostInit();
		//playerInfo.ClientInit();

		//Scene.Dispatch( new PlayerJoinedEvent( playerInfo ) );
	}

	PlayerInfo FindExistingPlayerInfo(Connection channel = null )
	{
		var possiblePlayerInfo = playerInfos.FirstOrDefault( x =>
		{
			// A candidate player state has no owner.
			return x.Network.OwnerConnection == null && x.steamId == channel.SteamId;
		} );

		if (possiblePlayerInfo.IsValid())
		{
			Log.Warning( $"Found existing player state for {channel.SteamId} that we can re-use. {possiblePlayerInfo}" );			
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
}
