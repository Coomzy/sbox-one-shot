
using Sandbox.Services;

public class RankInfo
{
	[Property] public string name { get; set; }
	[Property] public int xpRequirement { get; set; }
	[Property] public string icon { get; set; }
}

[GameResource("Rank Settings", "rs", "Rank Settings")]
public class RankSettings : ProjectSetting<RankSettings>
{
	[Property] public bool toggle { get; set; }
	[Group("Ranks"), Property, InlineEditor] public List<RankInfo> rankInfos { get; set; } = new ();

	public static string GetRankName(PlayerInfo player)
	{
		var rankInfo = GetRankInfo(player);
		if (rankInfo == null)
		{
			return "";
		}

		return rankInfo.name;
	}

	public static string GetRankIcon(PlayerInfo player)
	{
		var rankInfo = GetRankInfo(player);
		return rankInfo != null ? rankInfo.icon : "";
	}

	public static string GetRankIcon(int rank)
	{
		var rankInfo = GetRankInfo(rank);
		return rankInfo != null ? rankInfo.icon : "";
	}

	public static int GetRankXP(int rank)
	{
		var rankInfo = GetRankInfo(rank);
		return rankInfo != null ? rankInfo.xpRequirement : 0;
	}

	public static int GetRankIndex(int xp)
	{
		for (int i = 0; i < instance.rankInfos.Count; i++)
		{
			if (xp < instance.rankInfos[i].xpRequirement)
			{
				return i - 1;
			}

			if (i >= instance.rankInfos.Count-1)
			{
				return i;
			}
		}

		return 0;
	}

	public static RankInfo GetRankInfo(PlayerInfo player)
	{
		int rankIndex = 0;
		if (player is OSPlayerInfo osPlayerInfo)
		{
			rankIndex = osPlayerInfo.rank;
		}
		return GetRankInfo(rankIndex);
	}

	public static RankInfo GetRankInfo(int rankIndex)
	{
		if (!instance.rankInfos.Any())
		{
			return null;
		}
		rankIndex = MathY.Clamp(rankIndex, instance.rankInfos.Count - 1);
		return instance.rankInfos[rankIndex];
	}
}
