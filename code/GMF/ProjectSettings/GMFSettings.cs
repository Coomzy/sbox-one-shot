
using Sandbox.Services;
using System;

[GameResource("GMF Settings", "gmfs", "GMF Settings")]
public class GMFSettings : ProjectSetting<GMFSettings>
{
	[Property] public bool refresh { get; set; }
	[Property] public Dictionary<string, Role> userSteamIdToRole { get; set; } = new();

	public Role GetRoleFromID(ulong steamId)
	{
		var steamIdString = steamId.ToString();
		if (userSteamIdToRole.TryGetValue(steamIdString, out Role role))
		{
			return role;
		}

		return Role.None;
	}

	// TODO: For some reason GameObjects in a gameresource are throwing a deserialize error in this project
	//[Group("Prefabs")] public GameObject playerInfoPrefab { get; set; }

	//[Group("Prefabs"), Property] public GameObject playerInfoPrefab { get; set; }
	//[Group("Prefabs"), Property] public GameObject pawnPrefab { get; set; }

	//[Group("Prefabs"), Property] public GameObject spectatorPrefab { get; set; }
	//[Group("Prefabs"), Property] public GameObject spectatorVRPrefab { get; set; }

	// I used to think I'd need to spawn this, but now I'm not sure I do
	//[Group("Prefabs"), Property] public GameObject gameModePrefab { get; set; }
}