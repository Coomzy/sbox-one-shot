
using System.Diagnostics;

public static class Cheats
{
	[Conditional("DEBUG"), ConCmd("set_timescale")]
	public static void SetTimescale(float timescale = 1.0f)
	{
		if (Game.ActiveScene == null)
			return;

		Game.ActiveScene.TimeScale = timescale;
	}

	[ConCmd("suicide")]
	public static void Suicide()
	{
		if (!Check.IsFullyValid(PlayerInfo.local.character))
			return;

		DamageInfo damageInfo = new DamageInfo();
		damageInfo.instigator = PlayerInfo.local;
		damageInfo.damageCauser = PlayerInfo.local.character.equippedItem;
		PlayerInfo.local.character.Die(damageInfo);
	}

	[ConCmd("teleport_players")]
	public static void TeleportPlayers()
	{
		var teleportPoint = PlayerCamera.instance.GetPointInFront(150.0f);
		foreach (var playerInfo in PlayerInfo.all)
		{
			if (!Check.IsFullyValid(playerInfo?.character))
				continue;

			if (playerInfo.isLocal)
				continue;

			playerInfo?.character.Teleport(teleportPoint);
		}
	}
}
