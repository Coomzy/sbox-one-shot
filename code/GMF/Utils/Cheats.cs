
public static class Cheats
{
	[ConCmd("set_timescale")]
	public static void SetTimescale(float timescale = 1.0f)
	{
		if (Game.ActiveScene == null)
			return;

		Game.ActiveScene.TimeScale = timescale;
	}
}
