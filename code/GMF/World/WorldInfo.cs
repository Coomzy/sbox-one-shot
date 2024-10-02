
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Network;
using System;
using static Sandbox.Gizmo;

public class WorldInfo : Component, Component.INetworkListener
{
	public static WorldInfo instance { get; private set; }

	[Group("Prefabs"), Property] public GameObject playerInfoPrefab { get; set; }
	[Group("Prefabs"), Property] public GameObject pawnPrefab { get; set; }

	[Group("Prefabs"), Property] public GameObject spectatorPrefab { get; set; }

	[Property] public MapInstance mapInstance { get; set; }
	[Property] public MapCollider mapCollider { get; set; }

	[Property] public float killZ { get; set; } = -500.0f;

	protected override void OnAwake()
	{
		instance = this;
	}

	protected override void OnDestroy()
	{
		instance = null;
		base.OnDestroy();
	}

	protected override void OnStart()
	{
		base.OnStart();

		GetJumpPads();
	}

	[Button]
	public void GetJumpPads()
	{ 
		var mapObjects = Scene.GetAllComponents<MapObjectComponent>();

		foreach(var mapObject in mapObjects)
		{
			if (mapObject.GameObject.Name.ToLower().Contains("jumppad"))
			{
				mapObject.GameObject.Components.Create<JumpPad>();
			}
		}
	}
}
