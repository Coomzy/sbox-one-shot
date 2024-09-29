
using Sandbox;
using System.Numerics;

public class DebugConnections : Component
{
	protected override void OnUpdate()
	{
		Debuggin.ToScreen($"Connection.All: {Connection.All.Count}");
		foreach (var connection in Connection.All)
		{
			Debuggin.ToScreen($"connection: {connection.DisplayName} - {connection.Id}");
		}
		Debuggin.ToScreen("");
		Debuggin.ToScreen($"PlayerInfo.all: {PlayerInfo.all.Count}");
		foreach (var playerInfo in PlayerInfo.all)
		{
			Debuggin.ToScreen($"playerInfo: {playerInfo.displayName}");
		}
		Debuggin.ToScreen("");
		Debuggin.ToScreen($"PlayerInfo.allActive: {PlayerInfo.allActive.Count}");
		foreach (var playerInfo in PlayerInfo.allActive)
		{
			Debuggin.ToScreen($"playerInfo: {playerInfo.displayName}");
		}
		Debuggin.ToScreen("");
		Debuggin.ToScreen($"PlayerInfo.allInactive: {PlayerInfo.allInactive.Count}");
		foreach (var playerInfo in PlayerInfo.allInactive)
		{
			Debuggin.ToScreen($"playerInfo: {playerInfo.displayName}");
		}
		Debuggin.ToScreen("");
		Debuggin.ToScreen($"PlayerInfo.allAlive: {PlayerInfo.allAlive.Count}");
		foreach (var playerInfo in PlayerInfo.allAlive)
		{
			Debuggin.ToScreen($"playerInfo: {playerInfo.displayName}");
		}
		Debuggin.ToScreen("");
		Debuggin.ToScreen($"PlayerInfo.allDead: {PlayerInfo.allDead.Count}");
		foreach (var playerInfo in PlayerInfo.allDead)
		{
			Debuggin.ToScreen($"playerInfo: {playerInfo.displayName}");
		}
		Debuggin.ToScreen("");
	}
}