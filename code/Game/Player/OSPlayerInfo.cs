using Sandbox.Services;

[Group("OS")]
public class OSPlayerInfo : PlayerInfo, Component.INetworkSpawn
{
	[Group("Runtime"), Property, Sync] public int xp { get; private set; }
	[Group("Runtime"), Property, Sync] public int rank { get; private set; }

	[Group("Runtime"), Property, HostSync, Sync] public bool hasSixDegreesAchievement { get; set; }

	[Group("Runtime"), Property] public bool isPromptingPayToWin { get; set; }

	[Group("Runtime"), Property] public int multiKillStreak { get; set; }
	[Group("Runtime"), Property] public TimeSince timeSinceLastKill { get; set; } = 9999.9f;

	protected override void OnStart()
	{
		base.OnStart();

		if (IsProxy)
			return;

		xp = Stats.LocalPlayer.GetValue(Stat.XP);
		rank = RankSettings.GetRankIndex(xp);

		foreach (var achievement in Sandbox.Services.Achievements.All)
		{
			if (achievement.Name != Achievement.SIX_DEGREES_OF_SEPARATION)
				continue;

			if (!achievement.IsUnlocked)
				continue;

			hasSixDegreesAchievement = true;
			BroadcastSixDegreesAchievement();
		}

		if (!hasSixDegreesAchievement)
		{
			foreach (var playerInfo in PlayerInfo.allActive)
			{
				var osPlayerInfo = playerInfo as OSPlayerInfo;

				if (!IsFullyValid(osPlayerInfo))
					continue;

				if (!osPlayerInfo.hasSixDegreesAchievement)
					continue;

				hasSixDegreesAchievement = true;
				Achievements.Unlock(Achievement.SIX_DEGREES_OF_SEPARATION);
				break;
			}
		}
	}

	[Broadcast]
	public void BroadcastSixDegreesAchievement()
	{
		if (IsProxy)
			return;

		Achievements.Unlock(Achievement.SIX_DEGREES_OF_SEPARATION);
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (IsProxy)
			return;

		if (Input.Pressed(Inputs.sbux_menu))
		{
			PromptPayToWinGamePass();
		}
	}

	async void PromptPayToWinGamePass()
	{
		if (GameSettings.instance.payToWinGamePass.Has())
		{
			IUIEvents.Post(x => x.AddSystemText($"You already own the game pass"));
			return;
		}

		if (isPromptingPayToWin)
			return;

		isPromptingPayToWin = true;

		if (await Monetization.Purchase(GameSettings.instance.payToWinGamePass))
		{
			Achievements.Unlock(Achievement.PAY_TO_WIN);

			var nextRankXP = RankSettings.GetRankXP(rank + 1);
			var xpToNextRank = nextRankXP - xp;
			GainXP(xpToNextRank);
		}
		else
		{
			Sound.Play("achievement.paytowin.fail");
		}

		isPromptingPayToWin = false;
	}

	public override void OnScoreKill_Client()
	{
		base.OnScoreKill_Client();		

		GainXP(GameSettings.instance.xpPerKill);

		if (timeSinceLastKill > 4)
		{
			multiKillStreak = 0;
		}
		multiKillStreak++;
		Stat.ProcessMultiKill(multiKillStreak);
		timeSinceLastKill = 0;
	}

	public override void OnScoreRoundWin_Client()
	{
		base.OnScoreRoundWin_Client();

		GainXP(GameSettings.instance.xpPerRoundWin);
	}

	public override void OnScoreMatchWin_Client()
	{
		base.OnScoreMatchWin_Client();

		GainXP(GameSettings.instance.xpPerGameWin);
	}

	[Authority]
	public void GainXP(int amount)
	{
		int originalXP = xp;
		xp += amount;

		Stats.Increment(Stat.XP, amount);

		int newRank = RankSettings.GetRankIndex(xp);

		if (rank != newRank)
		{
			rank = newRank;

			IUIEvents.Post(x => x.OnRankUp());
		}
	}

	[Authority]
	public void ScoreAce()
	{
		Stats.Increment(Stat.ACES, 1);
		IUIEvents.Post(x => x.AddMedalEntry("ui/ace.png"));
	}
}
