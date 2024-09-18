
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Network;
using System;
using static Sandbox.Gizmo;

public class WorldSettings : SingletonComponent<WorldSettings>, Component.INetworkListener
{
	[Property] public GameObject playerInfoPrefab { get; set; }
	[Property] public GameObject spectatorPrefab { get; set; }
	[Property] public GameObject spectatorVRPrefab { get; set; }
	[Property] public MapCollider mapCollider { get; set; }

	[Property] public float killZ { get; set; } = -500.0f;

	[Property, Range(0,1)] public float sliderRange { get; set; }
	[Property] public float sliderValue => PlayerSettings.instance.characterMovementConfig.slideFalloffCurve.Evaluate(sliderRange);

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
