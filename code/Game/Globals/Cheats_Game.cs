
using System.Diagnostics;
using System;
using Sandbox;

public static partial class Cheats
{
	[Cheat(role = Role.Developer), ConCmd("reload_gun")]
	public static void Reload_Gun(bool enabled = false)
	{
		var gun = PlayerInfo.local?.character?.equippedItem as HarpoonGun;

		if (!IsFullyValid(gun))
			return;

		gun.Reload();
	}

	[Cheat(role = Role.Developer), ConCmd("givexp")]
	public static void GiveXP(int amount)
	{
		var osPlayerInfo = PlayerInfo.local as OSPlayerInfo;
		if (!IsFullyValid(osPlayerInfo))
			return;

		osPlayerInfo.GainXP(amount);
	}
}
