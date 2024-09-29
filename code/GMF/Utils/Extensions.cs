
using System;

public static class Extensions
{
	public static GameObject BreakPrefab(this GameObject inst)
	{
		inst.BreakFromPrefab();
		return inst;
	}

	public static T Random<T>(this List<T> list)
	{
		var index = System.Random.Shared.Next(list.Count);
		return list[index];
	}

	public static void Shuffle<T>(this IList<T> list)
	{
		int n = list.Count;
		while (n > 1)
		{
			n--;
			int k = System.Random.Shared.Next(n + 1);
			T value = list[k];
			list[k] = list[n];
			list[n] = value;
		}
	}

	public static void AddUnique<T>(this List<T> list, T item)
	{
		if (!list.Contains(item))
		{
			list.Add(item);
		}
	}

	// Not sure if this is needed, and if it is, maybe it should be in PlayerInfo
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
