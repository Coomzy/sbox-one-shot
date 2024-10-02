using Sandbox;
namespace badandbest.Sprays;

public static partial class Spray
{
	[ConVar( "spraydebug", Help = "Renders who placed a spray." ), Change( nameof( OnDirty ) )]
	internal static bool EnableDebug { get; set; }

	[ConVar( "spraydisable", Help = "Disables player sprays. Good for streamers.", Saved = true ), Change( nameof( OnDirty ) )]
	internal static bool DisableRendering { get; set; }

	private static void OnDirty( object oldValue, object newValue )
	{
		foreach ( var spray in Game.ActiveScene.GetAllComponents<SprayRenderer>() )
		{
			spray.UpdateObject();
		}
	}
}
