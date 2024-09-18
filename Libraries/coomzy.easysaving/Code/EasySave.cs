using System;
using Sandbox;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Linq;
using Sandbox.Internal;


public abstract class EasySave<T> : EasySaveNonGenericBase, IHotloadManaged where T : EasySave<T>
{
	static string fileName => $"{typeof(T).ToSimpleString(false)}.json";

	public static T instance => typeToInst.ContainsKey(typeof(T)) ? (T)typeToInst[typeof(T)] : null;

	public override void Save()
	{
		OnPreSave();
		FileSystem.Data.WriteJson<T>(fileName, this as T);
		OnPostSave();
	}

	protected override void Load()
	{
		uiTab = OnBuildUI();
		OnLoad();
	}

	protected virtual void OnPreSave(){}
	protected virtual void OnPostSave(){}
	protected virtual void OnLoad(){}

	protected virtual UITab OnBuildUI() => null;

	/*void IHotloadManaged.Created(IReadOnlyDictionary<string, object> state)
	{
		_instance = null;
	}

	void IHotloadManaged.Destroyed(Dictionary<string, object> state)
	{
		_instance = null;
	}*/

	/// <summary>
	/// This clears the instance, you probably only want this if you're doing editor shit
	/// </summary>
	public void Clear()
	{
		if (typeToInst.ContainsKey(typeof(T)))
		{
			typeToInst.Remove(typeof(T));
		}
	}

	/// <summary>
	/// This method is only here for a workaround for generics/reflection, don't call it or Load will be triggered twice
	/// </summary>
	public override void Init()
	{
		// Statics stick around, if we've already created an instance call load on it so logic doesn't break in editor
		if (instance != null)
		{
			instance.Load();
			return;
		}

		if (typeToInst.ContainsKey(typeof(T)))
		{
			Log.Error($"{typeof(T)} has a typeToInst entry but was null?");
		}

		if (FileSystem.Data.FileExists(fileName))
		{
			var fileInst = FileSystem.Data.ReadJson<T>(fileName);

			if (fileInst != null)
			{
				typeToInst[typeof(T)] = fileInst;
				fileInst.Load();
				return;
			}
		}

		T newInst = Activator.CreateInstance<T>();
		typeToInst[typeof(T)] = newInst;
		newInst.SetDefaultValues();
		newInst.Load();
		newInst.Save();
	}
}

/// <summary>
/// Generic workaround for EasySave&lt;&gt;, use that not this!
/// </summary>
public abstract class EasySaveNonGenericBase
{
	public static Dictionary<Type, EasySaveNonGenericBase> typeToInst { get; private set; } = new Dictionary<Type, EasySaveNonGenericBase>();

	[JsonIgnore] public UITab uiTab { get; protected set; }

	/// <summary>
	/// Use this to set default values the first time a class is created, because there is issues with setting default values
	/// </summary>
	protected virtual void SetDefaultValues(){}

	public abstract void Init();
	public abstract void Save();
	protected abstract void Load();

	public static void SaveAll()
	{
		foreach (var inst in typeToInst.Values)
		{
			inst.Save();
		}
	}
}

public class EasySaveSystem : GameObjectSystem
{
	public EasySaveSystem(Scene scene) : base(scene)
	{
		var types = GlobalGameNamespace.TypeLibrary.GetTypes<EasySaveNonGenericBase>();

		foreach (var type in types)
		{
			if (type.IsAbstract)
				continue;

			var inst = GlobalGameNamespace.TypeLibrary.Create(type.ClassName, type.TargetType);
			var easySaveBaseInst = inst as EasySaveNonGenericBase;

			if (easySaveBaseInst != null)
			{
				easySaveBaseInst.Init();
			}
		}
	}
}
