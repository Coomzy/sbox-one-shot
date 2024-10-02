global using static Check;
using Sandbox.Diagnostics;

// Add AssertIsValid?
//Sandbox.Diagnostics.Assert.IsNull(SteamAudioSource);

public static class Check
{
	public static bool IsFullyValid(Component component)
	{
		if (component == null || !component.IsValid)
			return false;

		return IsFullyValid(component?.GameObject);
	}

	public static bool IsFullyValid(GameObject gameObject)
	{
		if (gameObject == null || !gameObject.IsValid)
			return false;

		return true;
	}

	public static bool IsFullyValid(params object[] objects)
	{
		foreach (object obj in objects)
		{
			if (obj == null)
				return false;

			if (obj is GameObject go)
			{
				if (!IsFullyValid(go))
					return false;
				continue;
			}
			if (obj is Component comp)
			{
				if (!IsFullyValid(comp))
					return false;
				continue;
			}
			if (obj is IValid isValidInterface)
			{
				if (!isValidInterface.IsValid)
					return false;
				continue;
			}

			Log.Warning($"IDK what this is! obj: {obj}");
		}

		return true;
	}
}
