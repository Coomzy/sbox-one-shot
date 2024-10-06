
using System.Diagnostics;
using System;
using Sandbox;
using Sandbox.Services;
using System.Numerics;
using Sandbox.Utility;
using Sandbox.Diagnostics;

public enum CheatFlags
{
	None = 0,
	Broadcast = 1,
	//AllowInPackaged = 1,
}

[CodeGenerator(CodeGeneratorFlags.WrapMethod | CodeGeneratorFlags.Static, "CheatAttribute.Wrapper_Cheat_Method")]
//[CodeGenerator(CodeGeneratorFlags.WrapPropertySet | CodeGeneratorFlags.Static, "CheatAttribute.Wrapper_Cheat_Property", -1)]
public class CheatAttribute : Attribute
{
	public CheatFlags flags { get; set; }
	public Role role { get; set; } = Role.Moderator;

	public CheatAttribute(CheatFlags flags = CheatFlags.None, Role role = Role.Moderator)
	{
		this.flags = flags;
		this.role = role;
	}

	public static void Wrapper_Cheat_Method(WrappedMethod method, params object[] args)
	{		
		var cheatAttribute = method.Attributes.OfType<CheatAttribute>().FirstOrDefault();
		if (cheatAttribute == null)
		{
			Log.Warning($"CheatAttribute::Wrapper_Cheat_Method() called but CheatAttribute was missing? method: {method.MethodName}");
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

	// This doesn't work and I'm not sure it should
	public static void Wrapper_Cheat_Property<T>(WrappedPropertySet<T> property)
	{
		var cheatAttribute = property.Attributes.OfType<CheatAttribute>().FirstOrDefault();
		if (cheatAttribute == null)
		{
			Log.Warning($"CheatAttribute::Wrapper_Cheat_Property() called but CheatAttribute was missing? property: {property.PropertyName} type: {property.TypeName}");
			
			property.Setter(property.Value);
			return;
		}

		var role = IsFullyValid(PlayerInfo.local) ? PlayerInfo.local.role : Role.None;
		if (role < cheatAttribute.role)
		{
			Log.Warning($"Cannot use '{property.PropertyName}' because it requires the role '{cheatAttribute.role}'");

			var currentValueObject = ReflectionUtils.GetStaticPropertyValue(property.TypeName, property.PropertyName);
			T currentValue = property.Value;

			try
			{
				currentValue = (T)currentValueObject;
			}
			catch (Exception exception)
			{
				Log.Warning($"Failed to cast '{property.PropertyName}' exception: {exception}");
			}

			Log.Warning($"'{property.PropertyName}' currentValue: {currentValue}");
			property.Setter(currentValue);
			return;
		}

		property.Setter(property.Value);
	}
}

public static partial class Cheats
{
	[Cheat(CheatFlags.Broadcast), ConCmd("timescale")]
	public static void timescale(float timescale = 1.0f)
	{
		if (Game.ActiveScene == null)
			return;

		Game.ActiveScene.TimeScale = timescale;
	}

	[Cheat(role = Role.None), ConCmd("suicide")]
	public static void suicide()
	{
		if (!IsFullyValid(PlayerInfo.local.character))
			return;

		DamageInfo damageInfo = new DamageInfo();
		damageInfo.instigator = PlayerInfo.local;
		damageInfo.damageCauser = PlayerInfo.local.character.equippedItem;
		PlayerInfo.local.character.Die(damageInfo);
	}

	[Cheat, ConCmd]
	public static void teleport_players()
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
	public static void enable_voip(bool enabled = false)
	{
		PlayerInfo.local.voice.Enabled = enabled;
	}

	[Cheat(role = Role.Developer), ConCmd]
	public static void unlock_achievement(string achievementName)
	{
		Achievements.Unlock(achievementName);
	}

	[Cheat, ConCmd]
	public static void dump_players()
	{
		foreach (var player in PlayerInfo.all)
		{
			Log.Info($"{player.displayName} - steamID: {player.steamId}");
		}
	}

	[Cheat, ConCmd(Help = "use dump_players for SteamIDs and Role: None, Tester, Privileged, Moderator, Administrator, Developer")]
	public static void set_player_role(ulong steamId, Role newRole)
	{
		PlayerInfo targetPlayer = null;
		foreach (var player in PlayerInfo.all)
		{
			if (player.steamId != steamId)
				continue;

			targetPlayer = player;
			break;
		}

		if (!IsFullyValid(targetPlayer))
		{
			Log.Info($"steamID: {steamId}, newRole: {newRole}");
			return;
		}

		using (Rpc.FilterInclude(c => c.SteamId == steamId))
		{
			set_player_role_local(newRole);
		}
	}

	static void set_player_role_local(Role newRole)
	{
		PlayerInfo.local.role = newRole;
		IUIEvents.Post(x => x.AddAdminText($"New Role: {newRole}"));
	}

	[Cheat, ConCmd]
	public static void remove_slide_vel_cap(bool remove = true)
	{
		CharacterMovement.cheat_remove_slide_vel_cap = remove;
	}

	[Cheat(role = Role.None), ConCmd("suicide")]
	public static void Kill(ulong steamId)
	{
		if (!IsFullyValid(PlayerInfo.local.character))
			return;

		DamageInfo damageInfo = new DamageInfo();
		damageInfo.instigator = PlayerInfo.local;
		damageInfo.damageCauser = PlayerInfo.local.character.equippedItem;
		PlayerInfo.local.character.Die(damageInfo);
	}
}
