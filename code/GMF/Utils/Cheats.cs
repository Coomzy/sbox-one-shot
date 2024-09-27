
using System.Diagnostics;
using System;

public enum CheatFlags
{
	None = 0,
	Broadcast = 1,
	AllowInPackaged = 1,
}

[CodeGenerator(CodeGeneratorFlags.WrapMethod | CodeGeneratorFlags.Static, "CheatAttribute.OnCheatInvoked")]
public class CheatAttribute : Attribute
{
	public CheatFlags flags { get; set; }

	public CheatAttribute(CheatFlags flags = CheatFlags.None)
	{
		this.flags = flags;
	}

	public static void OnCheatInvoked(WrappedMethod method, params object[] args)
	{		
		var cheatAttribute = method.Attributes.OfType<CheatAttribute>().FirstOrDefault();
		if (cheatAttribute == null)
		{
			Log.Warning($"CheatAttribute::OnCheatInvoked() called but CheatAttribute was missing? method: {method.MethodName}");
			return;
		}

		// NB: This doesn't work https://github.com/Facepunch/sbox-issues/issues/6511
		if (!Application.IsDebug)
		{
			if (!cheatAttribute.flags.Contains(CheatFlags.AllowInPackaged))
			{
				Log.Warning($"Cannot use '{method.MethodName}' in a packaged game");
				return;
			}
		}

		if (cheatAttribute.flags.Contains(CheatFlags.Broadcast))
		{
			Sandbox.Rpc.OnStaticBroadcast(method, args);
			return;
		}

		if (cheatAttribute.flags.Contains(CheatFlags.Broadcast))
		{
			Sandbox.Rpc.OnStaticBroadcast(method, args);
			return;
		}

		method.Resume();
	}
}

public static class Cheats
{
	[Cheat(CheatFlags.AllowInPackaged), ConCmd("log_isdebug")]
	public static void LogIsDebug(float timescale = 1.0f)
	{
		Log.Info($"IsDebug: {Application.IsDebug}");
	}

	[Cheat(CheatFlags.Broadcast), ConCmd("timescale")]
	public static void SetTimescale(float timescale = 1.0f)
	{
		if (Game.ActiveScene == null)
			return;

		Game.ActiveScene.TimeScale = timescale;
	}

	[Cheat, ConCmd("suicide")]
	public static void Suicide()
	{
		if (!IsFullyValid(PlayerInfo.local.character))
			return;

		DamageInfo damageInfo = new DamageInfo();
		damageInfo.instigator = PlayerInfo.local;
		damageInfo.damageCauser = PlayerInfo.local.character.equippedItem;
		PlayerInfo.local.character.Die(damageInfo);
	}

	[Cheat, ConCmd("teleport_players")]
	public static void TeleportPlayers()
	{
		var teleportPoint = PlayerCamera.instance.GetPointInFront(150.0f);
		foreach (var playerInfo in PlayerInfo.all)
		{
			if (!IsFullyValid(playerInfo?.character))
				continue;

			if (playerInfo.isLocal)
				continue;

			playerInfo?.character.Teleport(teleportPoint);
		}
	}
}
