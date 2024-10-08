using System;

public class StaticInitializeAttribute : Attribute
{
	public int priority { get; set; } = 0;

	public StaticInitializeAttribute(int priority = 0)
	{
		this.priority = priority;
	}
}

public class StaticInitializeSystem : GameObjectSystem, ISceneStartup
{
	public StaticInitializeSystem(Scene scene) : base(scene)
	{
	}

	void ISceneStartup.OnClientInitialize()
	{
		Log.Info($"OnClientInitialize() Game.IsPlaying: {Game.IsPlaying}");
		// GameObjectSystem's load with the project
		if (!Game.IsPlaying)
		{
			return;
		}

		var staticInitializeMethods = TypeLibrary.GetMethodsWithAttribute<StaticInitializeAttribute>().OrderBy(x => x.Attribute.priority);
		foreach (var methodDescription in staticInitializeMethods)
		{
			methodDescription.Method.Invoke(null, null);
		}
	}
}