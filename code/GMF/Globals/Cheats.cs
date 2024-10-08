
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
	[Cheat(CheatFlags.Broadcast), ConCmd]
	public static void slomo(float timescale = 1.0f)
	{
		if (Game.ActiveScene == null)
			return;

		Game.ActiveScene.TimeScale = timescale;
	}

	[Cheat(role = Role.None), ConCmd]
	public static void suicide()
	{
		if (!IsFullyValid(PlayerInfo.local?.character?.body))
			return;

		DamageInfo damageInfo = new DamageInfo();
		damageInfo.instigator = PlayerInfo.local;
		damageInfo.damageCauser = null;
		PlayerInfo.local.character.body.TakeDamage(damageInfo);
	}

	[Cheat, ConCmd]
	public static void teleport()
	{
		if (!IsFullyValid(PlayerInfo.local?.character?.GameObject))
		{
			return;
		}

		var start = PlayerCamera.instance.GetPointInFront(0.0f);
		var end = PlayerCamera.instance.GetPointInFront(9999.0f);
		var trace = Game.ActiveScene.Trace
			.Ray(start, end)
			.IgnoreGameObjectHierarchy(PlayerInfo.local.character.GameObject)
			.WithoutTags(Tag.TRIGGER, Tag.CHARACTER_BODY, Tag.CHARACTER_BODY_REMOTE);//, Tag.PLAYER_CLIP, Tag.SKY);
		var result = trace.Run();

		var teleportPoint = result.Hit ? result.HitPosition : end;

		var radius = PlayerInfo.local.character.controller.Radius;
		//var height = PlayerInfo.local.character.controller.Height;

		teleportPoint -= (end - start).Normal * radius;

		PlayerInfo.local.character.Teleport(teleportPoint);
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
			Log.Info($"{player.displayName} - steamID: {player.steamID}");
		}
	}

	[Cheat, ConCmd(Help = "use dump_players for SteamIDs and Role: None, Tester, Privileged, Moderator, Administrator, Developer")]
	public static void set_player_role(ulong steamID, Role newRole)
	{
		if (!PlayerInfo.TryFromSteamID(steamID, out var playerInfo))
		{
			Log.Info($"Failed to find player with steamID: {steamID}");
			return;
		}

		using (Rpc.FilterInclude(c => c.SteamId == steamID))
		{
			set_player_role_local(newRole);
		}
	}

	[Broadcast]
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

	[Cheat, ConCmd]
	public static void reset_character_move_vel()
	{
		if (!IsFullyValid(PlayerInfo.local?.character?.movement))
			return;

		PlayerInfo.local.character.movement.heighestSlideVel = 0;
	}

	[Cheat(role = Role.None), ConCmd]
	public static void kill(ulong steamID)
	{
		if (!PlayerInfo.TryFromSteamID(steamID, out var playerInfo))
		{
			Log.Info($"Failed to find player with steamID: {steamID}");
			return;
		}

		if (!IsFullyValid(playerInfo?.character?.body))
			return;

		DamageInfo damageInfo = new DamageInfo();
		damageInfo.instigator = playerInfo;
		damageInfo.damageCauser = playerInfo.character.equippedItem;
		playerInfo.character.body.TakeDamage(damageInfo);
	}

	[Cheat, ConCmd(Help = "SpectateMode: None, Viewpoint, CharacterDeath, ThirdPerson, FreeCam")]
	public static void set_spectatemode(SpectateMode spectateMode)
	{
		PlayerInfo.local.SetSpectateMode(spectateMode);
	}

	[Cheat(role = Role.None), ConCmd]
	public static void dump_scene()
	{
		List<string> hierarchyLines = new List<string>();

		foreach (var rootGameObject in Game.ActiveScene.Children)
		{
			PrintGameObjectHierarchy(hierarchyLines, rootGameObject, 0);
		}

		string timestamp = DateTime.UtcNow.ToString("yy_MM_dd-HH_mm_ss");
		string directory = $"logs/scene_dumps/";
		string outputPath = $"{directory}{timestamp}.txt";
		FileSystem.Data.CreateDirectory(directory);
		FileSystem.Data.WriteAllText(outputPath, string.Join("\n", hierarchyLines));
	}

	static void PrintGameObjectHierarchy(List<string> lines, GameObject obj, int level)
	{
		lines.Add(new string(' ', level * 4) + "- " + $"{obj.Name} [Owner = {obj.Network.Owner?.DisplayName}] [NetworkMode = {obj.NetworkMode}] [IsProxy = {obj.IsProxy}]");

		foreach (var child in obj.Children)
		{
			PrintGameObjectHierarchy(lines, child, level + 1);
		}
	}
}
