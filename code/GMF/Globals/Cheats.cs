
using System.Diagnostics;
using System;
using Sandbox;

public enum CheatFlags
{
	None = 0,
	Broadcast = 1,
	//AllowInPackaged = 1,
}

[CodeGenerator(CodeGeneratorFlags.WrapMethod | CodeGeneratorFlags.Static, "CheatAttribute.OnCheatInvoked")]
public class CheatAttribute : Attribute
{
	public CheatFlags flags { get; set; }
	public Role role { get; set; } = Role.Moderator;

	public CheatAttribute(CheatFlags flags = CheatFlags.None, Role role = Role.Moderator)
	{
		this.flags = flags;
		this.role = role;
	}

	public static void OnCheatInvoked(WrappedMethod method, params object[] args)
	{		
		var cheatAttribute = method.Attributes.OfType<CheatAttribute>().FirstOrDefault();
		if (cheatAttribute == null)
		{
			Log.Warning($"CheatAttribute::OnCheatInvoked() called but CheatAttribute was missing? method: {method.MethodName}");
			return;
		}

		var role = IsFullyValid(PlayerInfo.local) ? PlayerInfo.local.role : Role.None;
		if (role < cheatAttribute.role)
		{
			Log.Warning($"Cannot use '{method.MethodName}' because it requires the role '{cheatAttribute.role}'");
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

public static partial class Cheats
{
	[Cheat(CheatFlags.Broadcast), ConCmd("timescale")]
	public static void SetTimescale(float timescale = 1.0f)
	{
		if (Game.ActiveScene == null)
			return;

		Game.ActiveScene.TimeScale = timescale;
	}

	[Cheat(role = Role.None), ConCmd("suicide")]
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
		foreach (var playerInfo in PlayerInfo.allActive)
		{
			if (!IsFullyValid(playerInfo?.character))
				continue;

			if (playerInfo.isLocal)
				continue;

			playerInfo?.character.Teleport(teleportPoint);
		}
	}

	[Cheat(role = Role.None), ConCmd("enable_voip")]
	public static void EnableVOIP(bool enabled = false)
	{
		PlayerInfo.local.voice.Enabled = enabled;
	}
}
