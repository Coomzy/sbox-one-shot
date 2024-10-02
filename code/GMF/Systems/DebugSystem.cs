using System;
using Sandbox;
using System.Linq;
using Sandbox.Network;
using Sandbox.Audio;
using System.ComponentModel.Design;
using static Sandbox.Component;

// TODO: Fuck it, ditch GameObjectSystem for this and move to a component :(
public class DebugSystem : GameObjectSystem, Component.INetworkListener, Component.INetworkSnapshot
{
	//DebugSystem.isEnabled
	public static bool isEnabled { get; set; }

	public DebugSystem(Scene scene) : base(scene)
	{
		isEnabled = Application.IsEditor;

		var newGO = new GameObject(true, "Debug Component");
		newGO.AddComponent<DebugComponent>();
		newGO.Flags = GameObjectFlags.DontDestroyOnLoad | GameObjectFlags.Hidden;
		newGO.NetworkMode = NetworkMode.Snapshot;
	}

	void INetworkSnapshot.WriteSnapshot(ref ByteStream writer)
	{
		Log.Info($"INetworkSnapshot.WriteSnapshot() isEnabled: {isEnabled}, Application.IsEditor: {Application.IsEditor}");
		if (!Application.IsEditor)
		{
			writer.Write(false);
			return;
		}

		writer.Write(isEnabled);
	}

	void INetworkSnapshot.ReadSnapshot(ref ByteStream reader)
	{
		isEnabled = reader.Read<bool>();
		Log.Info($"INetworkSnapshot.ReadSnapshot() isEnabled: {isEnabled}, Application.IsEditor: {Application.IsEditor}");
	}

	public void OnActive(Connection channel)
	{
		return;
		if (!Application.IsEditor)
		{
			return;
		}

		Log.Info($"OnActive() channel: {channel.DisplayName}");
		if (channel == null || channel.IsHost)
		{
			return;
		}

		using (Rpc.FilterInclude(c => c == channel))
		{
			Log.Info($"Rpc.FilterInclude(c => c == channel)");
			SetDebugMode();
		}
	}

	[Broadcast]
	static void SetDebugMode()
	{
		Log.Info($"SetDebugMode()");
		isEnabled = true;
	}

	[ConCmd("debugmode")]
	public static void EnableDebugMode(bool enabled = true)
	{
		Log.Info($"EnableDebugMode() enabled: {enabled}");
		isEnabled = enabled;
	}
}

public class DebugComponent : Component, Component.INetworkListener, Component.INetworkSnapshot
{
	public void OnActive(Connection channel)
	{
		if (channel == null || !channel.IsHost)
		{
			return;
		}

		ReconnecterSystem.OnStartedHosting();
	}

	void INetworkSnapshot.WriteSnapshot(ref ByteStream writer)
	{
		Log.Info($"INetworkSnapshot.WriteSnapshot() isEnabled: {DebugSystem.isEnabled}, Application.IsEditor: {Application.IsEditor}");
		if (!Application.IsEditor)
		{
			writer.Write(false);
			return;
		}

		writer.Write(DebugSystem.isEnabled);
	}

	void INetworkSnapshot.ReadSnapshot(ref ByteStream reader)
	{
		DebugSystem.isEnabled = reader.Read<bool>();
		Log.Info($"INetworkSnapshot.ReadSnapshot() isEnabled: {DebugSystem.isEnabled}, Application.IsEditor: {Application.IsEditor}");
	}
}