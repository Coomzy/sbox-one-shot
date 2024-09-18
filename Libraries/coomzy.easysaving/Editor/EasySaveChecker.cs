using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using static Editor.EditorEvent;
using Sandbox.Internal;
using System.Text.Json.Serialization;

public static class EasySaveChecker
{
	[Hotload]
	public static void CheckEasySaves()
	{
		var types = GlobalGameNamespace.TypeLibrary.GetTypes<EasySaveNonGenericBase>();

		List<Type> easySaveSubclasses = new List<Type>();
		foreach (var type in types)
		{
			if (type.IsAbstract)
				continue;

			easySaveSubclasses.Add(type.TargetType);
		}

		foreach(var easySaveSubclass in easySaveSubclasses)
		{
			//Log.Info($"easySaveSubclass = {easySaveSubclass.Name}");

			var fields = easySaveSubclass.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			foreach (var field in fields)
			{
				if (field.Name.Contains("k__BackingField"))
				{
					var propertyName = field.Name.Replace("k__BackingField", string.Empty).Trim('<', '>');
					var property = easySaveSubclass.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

					if (property != null)
					{
						var setMethod = property.GetSetMethod(nonPublic: true);
						if (setMethod != null && setMethod.IsPrivate)
						{
							Log.Error($"{easySaveSubclass.Name} contains a property '{field.Name}' with a private setter which will not be serialized by JSON. If this is intentional add the JsonIgnore attribute");
						}
					}
					continue;
				}

				if (field.IsDefined(typeof(JsonIgnoreAttribute), false))
				{
					continue;
				}

				Log.Error($"{easySaveSubclass.Name} contains a field '{field.Name}' which will not be serialized by JSON. If this is intentional add the JsonIgnore attribute");
			}
		}
	}
}
