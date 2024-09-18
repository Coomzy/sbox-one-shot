using System;
using System.Reflection;
using Editor;
using Sandbox;
using System.Linq;
using System.Collections.Generic;
using static Editor.EditorEvent;
using Sandbox.Internal;
using System.IO;

public static class ProjectSettingCreator
{
	[Hotload(Priority = -10)]
	public static void CreateProjectSettingFiles()
	{
		var types = GlobalGameNamespace.TypeLibrary.GetTypes<ProjectSettingNonGenericBase>();

		foreach(var type in types)
		{
			if (type.IsAbstract)
				continue;

			var targetType = type.TargetType;
			var instance = Activator.CreateInstance(targetType) as GameResource;

			Type constructedType = typeof(ProjectSetting<>).MakeGenericType(targetType);
			var filePathProperty = constructedType.GetProperty("fullFilePathWithoutExtension", BindingFlags.Static | BindingFlags.Public);
			var fileExtensionProperty = constructedType.GetProperty("fileExtension", BindingFlags.Static | BindingFlags.Public);

			string filePath = filePathProperty != null ? filePathProperty.GetValue(null) as string : null;
			string fileExtension = fileExtensionProperty != null ? fileExtensionProperty.GetValue(null) as string : null;

			if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(fileExtension))
			{
				Log.Error($"Could not retrieve filePath or fileExtension for '{targetType}'");
				continue;
			}

			var targetDirectory = Path.GetDirectoryName(filePath);
			if (!Directory.Exists(targetDirectory))
			{
				Directory.CreateDirectory(targetDirectory);
			}
	
			AssetSystem.CreateResource(fileExtension, filePath);
			AssetSystem.RegisterFile($"{filePath}.{fileExtension}");
		}
	}
}
