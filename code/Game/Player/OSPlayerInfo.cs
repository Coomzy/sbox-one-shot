
[Group("OS")]
public class OSPlayerInfo : PlayerInfo, Component.INetworkSpawn
{
	[Group("Runtime"), Property] public bool isPromptingPayToWin { get; set; }

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (IsProxy)
			return;

		if (Input.Pressed("pay_to_win"))
		{
			PromptPayToWinGamePass();
		}
	}

	async void PromptPayToWinGamePass()
	{
		if (isPromptingPayToWin)
			return;

		isPromptingPayToWin = true;

		if (await Sandbox.Services.Monetization.Purchase(GameSettings.instance.payToWinGamePass))
		{
			Sandbox.Services.Achievements.Unlock("pay_to_win");
		}
		else
		{
			Sound.Play("achievement.paytowin.fail");
		}

		isPromptingPayToWin = false;
	}
}
