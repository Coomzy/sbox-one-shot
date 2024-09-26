using System.Text.Json;
namespace Sandbox.Services;

[GameResource( "Game Pass", "gamepass", "A game pass", Icon = "sbux" )]
public class GamePass : GameResource
{
	/// <summary>
	/// The ident of this game pass. A short name with no special characters.
	/// </summary>
	[Category("Game Pass Setup")]
	public string Ident { get; set; }
	
	/// <summary>
	/// The amount of s&amp;bux this game pass costs.
	/// </summary>
	[Category("Game Pass Setup")]
	public int Cost { get; set; }
	
	/// <summary>
	/// Name of the game pass to show in UI.
	/// </summary>
	[Category("Display Information")]
	public string Title { get; set; }

	/// <summary>
	/// Icon for this game pass. Only works with urls (for now).
	/// </summary>
	[Category( "Display Information" )]
	public string Icon { get; set; }
	
	internal string Serialize() => JsonSerializer.Serialize( new { Ident, Cost, Title, Icon } ).Base64Encode();
}
