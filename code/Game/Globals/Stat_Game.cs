
using Sandbox;
using Sandbox.Services;

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

	// This multikill stuff ballooning out in the stat class is weird, but oh well
	public static void IncrementMultiKill(int killCount)
	{
		switch (killCount)
		{
			case 2:
				Stats.Increment(DOUBLE_KILLS, 1);
				break;
			case 3:
				Stats.Increment(TRIPLE_KILLS, 1);
				break;
			case 4:
				Stats.Increment(OVERKILLS, 1);
				break;
			case 5:
				Stats.Increment(KILLTACULARS, 1);
				break;
			case 6:
				Stats.Increment(KILLTROCITY, 1);
				break;
			case 7:
				Stats.Increment(KILLIMANJARO, 1);
				break;
		}
	}

	public static void AnnounceMultiKill(int killCount)
	{
		switch (killCount)
		{
			case 2:
				AnnouncerSystem.QueueSound(DOUBLE_KILLS);
				break;
			case 3:
				AnnouncerSystem.QueueSound(TRIPLE_KILLS);
				break;
			case 4:
				AnnouncerSystem.QueueSound(OVERKILLS);
				break;
			case 5:
				AnnouncerSystem.QueueSound(KILLTACULARS);
				break;
			case 6:
				AnnouncerSystem.QueueSound(KILLTROCITY);
				break;
			case 7:
				AnnouncerSystem.QueueSound(KILLIMANJARO);
				break;
		}
	}

	public static void DisplayMultiKill(int killCount)
	{
		switch (killCount)
		{
			case 2:
				IUIEvents.Post(x => x.AddMedalEntry($"ui/multikills/{DOUBLE_KILLS}.png"));
				break;
			case 3:
				IUIEvents.Post(x => x.AddMedalEntry($"ui/multikills/{TRIPLE_KILLS}.png"));
				break;
			case 4:
				IUIEvents.Post(x => x.AddMedalEntry($"ui/multikills/{OVERKILLS}.png"));
				break;
			case 5:;
				IUIEvents.Post(x => x.AddMedalEntry($"ui/multikills/{KILLTACULARS}.png"));
				break;
			case 6:
				IUIEvents.Post(x => x.AddMedalEntry($"ui/multikills/{KILLTROCITY}.png"));
				break;
			case 7:
				IUIEvents.Post(x => x.AddMedalEntry($"ui/multikills/{KILLIMANJARO}.png"));
				break;
		}
	}

	public static void ProcessMultiKill(int killCount)
	{
		IncrementMultiKill(killCount);
		AnnounceMultiKill(killCount);
		DisplayMultiKill(killCount);
	}	
}
