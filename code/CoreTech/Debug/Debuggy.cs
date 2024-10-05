
using Sandbox;
using Sandbox.Internal;
using Sandbox.Services;
using System;
using System.Numerics;
using System.Reflection;

public static class Debuggy
{
	[ConVar] public static bool debug_log_connections { get; set; }

	public static void PrintConnections()
	{
		if (!debug_log_connections)
		{
			return;
		}

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

	[ConCmd]
	public static void dump_achievements()
	{
		foreach (var achievement in Achievements.All)
		{
			Log.Info($"achievement '{achievement.Name}' IsUnlocked: {achievement.IsUnlocked} HasProgression: {achievement.HasProgression} ProgressionFraction: {achievement.ProgressionFraction}");
		}
	}
}