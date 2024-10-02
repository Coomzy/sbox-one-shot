using Sandbox;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace badandbest.Sprays;

/// <summary>
/// A library to allow the placement of sprays in the world
/// </summary>
public static partial class Spray
{
	private static bool IsImage( this HttpContent content ) => content.Headers.GetValues( "Content-Type" ).Any( type => type.Contains( "image" ) );

	[ConCmd( "spray", Help = "URL of image. Must be in quotes." )]
	internal static async void SetImage( string imageUrl )
	{
		try
		{
			var uri = new Uri( imageUrl );
			var response = await Http.RequestAsync( uri.AbsoluteUri );
			
			if ( !response.Content.IsImage() )
			{
				throw new FileNotFoundException("Not an image type: Sites like Tenor require you to Right click > Copy image address");
			}
		}
		catch ( Exception e )
		{
			Log.Warning( e );
			imageUrl = "materials/fallback.vtex";
		}
		finally
		{
			Cookie.Set( "spray.url", imageUrl );
		}
	}
}
