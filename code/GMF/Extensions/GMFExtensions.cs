
using System;

public static class GMFExtensions
{
	public static PlayerInfo GetOwningPlayerInfo(this GameObject gameObject)
	{
		if (gameObject == null || !gameObject.IsValid)
			return null;

		if (!gameObject.Network.Active || gameObject.Network.OwnerId == Guid.Empty)
			return null;

		foreach (var playerInfo in PlayerInfo.all)
		{
			if (playerInfo == null || !playerInfo.IsValid || playerInfo?.GameObject == null)
				return null;

			if (!playerInfo.Network.Active || playerInfo.Network.OwnerId == Guid.Empty)
				return null;

			if (gameObject.Network.OwnerId == playerInfo.Network.OwnerId)
				return playerInfo;
		}

		return null;
	}
}
