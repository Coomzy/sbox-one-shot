
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
}
