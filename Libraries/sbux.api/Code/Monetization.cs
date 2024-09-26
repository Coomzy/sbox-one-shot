using Sandbox.Diagnostics;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
namespace Sandbox.Services;

public static class Monetization
{
	[ConVar( "sbux", Help = "Your s&bux balance.", Saved = true )]
	private static string _balance { get; set; }

	private static List<string> _gamePass { get; set; }

	private const string URL = "https://sbux.party/";

	private static async Task<string> Identification() => $"?steamid={Game.SteamId}&token={await Auth.GetToken( "sbux" )}&ident={Game.Ident}&balance={_balance}";

	private static readonly Task Loading;

	private static async Task Refresh()
	{
		try
		{
			var response = await Http.RequestAsync( URL + await Identification() );

			if ( response.IsSuccessStatusCode )
			{
				var result = JsonNode.Parse( await response.Content.ReadAsStringAsync() );

				_balance = result?["balance"].Deserialize<string>() ?? "0";
				_gamePass = result?["gamepass"].Deserialize<List<string>>() ?? new List<string>();
			}
		}
		catch ( Exception e )
		{
			Log.Warning( $"Something went wrong when trying to update monetization values. {e}" );
		}
	}

	static Monetization()
	{
		Loading = Refresh();
	}

	/// <summary>
	/// It may take a second to get a response from the backend. Use this to ensure everything is loaded.
	/// </summary>
	public static Task WaitForLoad() => Loading;

	/// <summary>
	/// If the player owns the game pass.
	/// </summary>
	public static bool Has( this GamePass gamePass )
	{
		Assert.NotNull( gamePass );

		return _gamePass.Contains( gamePass.Ident );
	}

	/// <summary>Prompts the player to purchase the game pass.</summary>
	/// <returns>True if the game pass was bought.</returns>
	/// <remarks>A game pass can be purchased multiple times.</remarks>
	public static async Task<bool> Purchase( this GamePass gamePass )
	{
		try
		{
			Assert.NotNull( gamePass );

			var prompt = new Prompt( URL + gamePass.Serialize() + await Identification() );

			if ( await prompt.Purchased.Task )
			{
				await Refresh();

				return true;
			}
		}
		catch ( Exception e )
		{
			Log.Info( e );
		}

		return false;
	}

	/// <summary>
	/// If the player owns the game pass, if not - prompt a purchase.
	/// </summary>
	public static async Task<bool> HasOrPurchase( this GamePass gamePass )
	{
		return gamePass.Has() || await gamePass.Purchase();
	}
}
