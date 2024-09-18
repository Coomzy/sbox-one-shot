
using Sandbox.Citizen;

public class DebugNetworkStart : Component, Component.INetworkSpawn
{
	bool hasNetworkSpawned = false;

	protected override void OnAwake()
	{
		Log.Info($"DebugNetworkStart::OnAwake()             IsOwner: {GameObject.Network.IsOwner}, IsCreator: {GameObject.Network.IsCreator}, OwnerConnection: {GameObject.Network.OwnerConnection}, IsHost: {Networking.IsHost} for '{GameObject.Name}' hasNetworkSpawned: {hasNetworkSpawned}");
	}

	protected override void OnStart()
	{
		Log.Info($"DebugNetworkStart::OnStart()             IsOwner: {GameObject.Network.IsOwner}, IsCreator: {GameObject.Network.IsCreator}, OwnerConnection: {GameObject.Network.OwnerConnection}, IsHost: {Networking.IsHost} for '{GameObject.Name}' hasNetworkSpawned: {hasNetworkSpawned}");
	}

	public async virtual void OnNetworkSpawn(Connection connection)
	{
		hasNetworkSpawned = true;

		Log.Info($"CharacterVisual::OnNetworkSpawn() PRE  IsOwner: {GameObject.Network.IsOwner}, IsCreator: {GameObject.Network.IsCreator}, OwnerConnection: {GameObject.Network.OwnerConnection}, IsHost: {Networking.IsHost} for '{GameObject.Name}' connection: {connection}");
;
		await Task.Frame();

		Log.Info($"CharacterVisual::OnNetworkSpawn() POST IsOwner: {GameObject.Network.IsOwner}, IsCreator: {GameObject.Network.IsCreator}, OwnerConnection: {GameObject.Network.OwnerConnection}, IsHost: {Networking.IsHost} for '{GameObject.Name}' connection: {connection}");
	}
}
