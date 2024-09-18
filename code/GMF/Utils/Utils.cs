
public static class Utils
{
	public static GameObject FindInChildren(GameObject gameObject, System.Predicate<GameObject> predicate)
	{
		if (gameObject == null)
			return null;

		if (predicate.Invoke(gameObject))
		{
			return gameObject;
		}

		foreach (var child in gameObject.Children)
		{
			var matchedGO = FindInChildren(child, predicate);
			if (matchedGO != null)
			{
				return matchedGO;
			}
		}

		return null;
	}
}
