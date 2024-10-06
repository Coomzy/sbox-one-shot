using System;

public static class ReflectionUtils
{
	public static object GetStaticPropertyValue(string typeName, string propertyName)
	{
		if (string.IsNullOrWhiteSpace(typeName) || string.IsNullOrWhiteSpace(propertyName))
		{
			Log.Warning($"Tried to get static property but something was null or empty: typeName: '{typeName}', propertyName: '{propertyName}'");
			return null;
		}

		if (TypeLibrary == null)
		{
			Log.Warning("TypeLibrary was null");
			return null;
		}

		var typeDesc = TypeLibrary.GetType(typeName);
		if (typeDesc == null)
		{
			Log.Warning($"Failed to get type descriptor for typeName: '{typeName}'");
			return null;
		}

		if (typeDesc.Properties == null || !typeDesc.Properties.Any())
		{
			Log.Warning($"No properties found for type '{typeName}'");
			return null;
		}

		var prop = typeDesc.Properties.FirstOrDefault(x => x.IsStatic && x.IsNamed(propertyName));
		if (prop == null)
		{
			Log.Warning($"No static property found with name '{propertyName}' in type '{typeName}'");
			return null;
		}

		try
		{
			var currentValueObject = prop.GetValue(null);
			return currentValueObject;
		}
		catch (Exception ex)
		{
			Log.Warning($"Exception encountered while getting value for property '{propertyName}' in type '{typeName}': {ex}");
			return null;
		}
	}

}
