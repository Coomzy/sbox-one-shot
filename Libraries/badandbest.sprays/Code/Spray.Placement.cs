using Sandbox;
using Sandbox.Utility;
using System.Collections.Generic;
namespace badandbest.Sprays;

public static partial class Spray
{
	public static GameObject LocalSpray;

	/// <summary>
	/// Places an image on a surface.
	/// </summary>
	public static void Place()
	{
		const float RANGE = 128;// Range in GMOD.

		var ray = Game.ActiveScene.Camera.Transform.World.ForwardRay;
		var trace = Game.SceneTrace.Ray( ray, RANGE );

		Place( trace );
	}

	/// <summary>
	/// Places an image on a surface.
	/// </summary>
	/// <param name="trace">The trace to use.</param>
	public static void Place( SceneTrace trace )
	{
		// We only want to hit static bodies. ( maps, etc )
		if ( trace.Run() is not { Body.BodyType: PhysicsBodyType.Static } tr )
			return;

		var config = new CloneConfig
		{
			Name = $"Spray - {Steam.PersonaName}",
			Transform = new Transform( tr.HitPosition, Rotation.LookAt( tr.Normal ) ),
			PrefabVariables = new Dictionary<string, object>
			{
				{ "Image", Cookie.Get( "spray.url", "materials/decals/default.png" ) },
				{ "Placer", Steam.PersonaName },
			}
		};

		LocalSpray?.Destroy();
		LocalSpray = GameObject.Clone( "prefabs/spray.prefab", config );

		LocalSpray.NetworkSpawn(); // NetworkSpawn breaks the prefab
		LocalSpray.SetPrefabSource( "prefabs/spray.prefab" );
	}
}

[Title( "Spray Renderer" ), Icon( "imagesearch_roller" )]
internal class SprayRenderer : Renderer
{
	[Property] internal DecalRenderer _decal { get; set; }
	[Property] internal TextRenderer _text { get; set; }

	[Property, ImageAssetPath]
	public string Image { get; set; }

	[Button( "Remove Spray", Icon = "clear" )]
	public void Remove() => GameObject.Destroy();
	
	public virtual void UpdateObject()
	{
		_decal.Enabled = !Spray.DisableRendering;
		_text.Enabled = Spray.EnableDebug;
	}

	protected override async void OnAwake()
	{
		UpdateObject();

		var texture = await Texture.LoadAsync( FileSystem.Mounted, Image );

		_decal.Material = Material.Load( "materials/spray.vmat" ).CreateCopy();
		_decal.Material.Set( "g_tColor", texture );
	}
}
