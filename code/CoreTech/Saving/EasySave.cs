using System;
using Sandbox;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Linq;
using Sandbox.Internal;
using static Sandbox.Gizmo;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;


public abstract class EasySave<T> where T : EasySave<T>
{
	static string fileName => $"{typeof(T).ToSimpleString(false)}.json";

#pragma warning disable SB3000 // Hotloading not supported
	static T _instance;
#pragma warning restore SB3000 // Hotloading not supported
	public static T instance
	{
		get
		{
			if (_instance != null)
			{
				return _instance;
			}

			if (FileSystem.Data.FileExists(fileName))
			{
				var jsonString = FileSystem.Data.ReadAllText(fileName);
				if (!string.IsNullOrWhiteSpace(jsonString))
				{
					var fileInst = JsonSerializer.Deserialize<T>(jsonString, jsonSerializerOptions);
					if (fileInst != null)
					{
						_instance = fileInst;
						_instance.OnLoad();
						return _instance;
					}
				}
			}

			T newInst = Activator.CreateInstance<T>();
			_instance = newInst;
			_instance.OnLoad();
			_instance.Save();
			return newInst;
		}
	}

	public static JsonSerializerOptions jsonSerializerOptions => new JsonSerializerOptions
	{
		IncludeFields = true,
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.Never
	};

	public void Save()
	{
		OnPreSave();
		string jsonString = JsonSerializer.Serialize(this, jsonSerializerOptions);
		FileSystem.Data.WriteAllText(fileName, jsonString);
		OnPostSave();
	}

	protected virtual void OnPreSave(){}
	protected virtual void OnPostSave(){}
	protected virtual void OnLoad(){}
}