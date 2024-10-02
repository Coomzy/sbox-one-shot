using Sandbox.Services;

[Group("OS")]
public class OSPlayerInfo : PlayerInfo, Component.INetworkSpawn
{
	[Group("Runtime"), Property, Sync] public int xp { get; private set; }
	[Group("Runtime"), Property, Sync] public int rank { get; private set; }

	[Group("Runtime"), Property] public bool isPromptingPayToWin { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

		if (IsProxy)
			return;

		xp = Stats.LocalPlayer.GetValue(Stat.XP);
		rank = RankSettings.GetRankIndex(xp);
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
		if (isPromptingPayToWin)
			return;

		isPromptingPayToWin = true;

		if (await Monetization.Purchase(GameSettings.instance.payToWinGamePass))
		{
			Achievements.Unlock("pay_to_win");
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
	}

	public override void OnScoreRoundWin_Client()
	{
		base.OnScoreRoundWin_Client();

		GainXP(GameSettings.instance.xpPerRoundWin);
	}

	public override void OnScoreGameWin_Client()
	{
		base.OnScoreGameWin_Client();

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
			UIManager.instance.rankUpWidget.Show();
		}
	}
}
