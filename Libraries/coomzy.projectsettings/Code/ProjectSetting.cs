using System;
using Sandbox;
using System.Collections.Generic;

public abstract partial class ProjectSetting<T> : ProjectSettingNonGenericBase, IHotloadManaged where T : ProjectSetting<T>
{
	public static string fileName => $@"{typeof(T).ToSimpleString(false)}";
	public static string filePath => $@"{filePathWithoutExtension}.{fileExtension}";
	public static string filePathWithoutExtension => $@"ProjectSettings\{fileName}";
	public static string fullFilePath => $@"{Project.Current.GetAssetsPath()}\{filePath}";
	public static string fullFilePathWithoutExtension => $@"{Project.Current.GetAssetsPath()}\{filePathWithoutExtension}";

	public static string fileExtension
	{
		get
		{
			var gameResourceAttribute = (GameResourceAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(GameResourceAttribute));
			if (gameResourceAttribute == null)
			{
				Log.Error($"Type '{typeof(T)}' does not have the required GameResourceAttribute on it!");
				return "";
			}

			return gameResourceAttribute.Extension;
		}
	}

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

			Type type = typeof(T);

			if (ResourceLibrary.TryGet(filePath, out T inst))
			{
				_instance = inst;
				_instance.Load();
				return _instance;
			}

			T newInst = Activator.CreateInstance<T>();
			newInst.Load();
			_instance = newInst;

			if (Game.IsEditor)
			{
				Log.Error($"ProjectSetting '{type.ToSimpleString(false)}' need a file in /ProjectSettings/");
				return newInst;
			}

			return newInst;
		}
	}

	void Load()
	{
		OnLoad();
	}

	/// <summary>
	/// This currently only gets called once in editor
	/// </summary>
	protected virtual void OnLoad(){}

	/// <summary>
	/// This clears the instance, you probably don't want to do this unless you're doing editor code
	/// </summary>
	public void Clear()
	{
		_instance = null;
	}
}

/// <summary>
/// Generic workaround for ProjectSetting&lt;&gt;, use that not this!
/// </summary>
public abstract class ProjectSettingNonGenericBase : GameResource
{
	public static Dictionary<Type, ProjectSettingNonGenericBase> typeToInst { get; private set; } = new Dictionary<Type, ProjectSettingNonGenericBase>();
}