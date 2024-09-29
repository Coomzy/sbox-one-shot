using System;
using System.Reflection;

public static class EditorBridgeEditor
{
	[EditorEvent.Hotload]
	public static void OpenMyMenu()
	{
		EditorBridgeReflection.OnIsValid = OnIsValid;
	}

	public static bool OnIsValid(Type type, string memberName, BindingFlags bindingFlags)
	{
		if (type == null || string.IsNullOrWhiteSpace(memberName))
		{
			Log.Warning($"Passed bad type of member name '{type}.{memberName}'");
			return false;
		}

		var property = type.GetProperty(memberName, bindingFlags);
		if (property != null)
		{
			var activeValue = property.GetValue(null);
			return activeValue != null;
		}

		var field = type.GetField(memberName, bindingFlags);
		if (field != null)
		{
			var activeValue = field.GetValue(null);
			return activeValue != null;
		}

		Log.Warning($"Could not find '{type}.{memberName}' with bindings {bindingFlags}");

		return false;
	}

	public static bool IsGizmoValid()
	{
		var activeProperty = typeof(Gizmo).GetProperty("Active", BindingFlags.Static | BindingFlags.NonPublic);

		if (activeProperty != null)
		{
			var activeValue = activeProperty.GetValue(null);
			return activeValue != null;
		}
		else
		{
			Log.Info("Could not find 'Active' property in the Gizmo class.");
		}

		return true;
	}
}
