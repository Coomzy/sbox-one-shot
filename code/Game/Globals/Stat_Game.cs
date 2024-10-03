
public static partial class Stat
{
	public const string XP = "xp";
	public const string FURTHEST_KILL = "furthest-kill";
	public const string ACES = "aces";

	public const string DOUBLE_KILLS = "double-kills";	// 2
	public const string TRIPLE_KILLS = "triple-kills";	// 3
	public const string OVERKILLS = "overkills";		// 4
	public const string KILLTACULARS = "killtaculars";	// 5
	public const string KILLTROCITY = "killtrocity";	// 6
	public const string KILLIMANJARO = "killimanjaro";	// 7

	public static string KillCountToMedal(int killCount)
	{
		switch (killCount)
		{
			case 2:
				return DOUBLE_KILLS;
			case 3:
				return TRIPLE_KILLS;
			case 4:
				return OVERKILLS;
			case 5:
				return KILLTACULARS;
			case 6:
				return KILLTROCITY;
			case 7:
				return KILLIMANJARO;
		}

		return null;
	}
}
