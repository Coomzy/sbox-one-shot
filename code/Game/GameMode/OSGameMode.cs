
using badandbest.Sprays;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class OSGameMode : GameMode, Component.INetworkListener, IHotloadManaged
{
	[Group("Runtime"), Property] public bool announcedTenSecsRemaining { get; set; }
	[Group("Runtime"), Property] public bool announcedThirtySecsRemaining { get; set; }
	[Group("Runtime"), Property] public bool announcedOneMinRemaining { get; set; }

	protected override void ReadyPhaseStart()
	{
		base.ReadyPhaseStart();

		/*var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();
		spawnPoints.Shuffle();
		int playerCount = 0;
		foreach (PlayerInfo playerInfo in PlayerInfo.all)
		{
			CreatePawn(playerInfo, spawnPoints[playerCount]);
			playerCount++;
		}*/
	}

	public override void OnPlayerDisconnected(PlayerInfo playerInfo)
	{
		base.OnPlayerDisconnected(playerInfo);
	}

	protected override void ActiveRoundStart()
	{
		announcedTenSecsRemaining = false;
		announcedThirtySecsRemaining = false;
		announcedOneMinRemaining = false;

		base.ActiveRoundStart();
	}

	protected override void ActiveRoundUpdate()
	{
		base.ActiveRoundUpdate();

		// Round can end from base call
		if (modeState != ModeState.ActiveRound || delayedRoundConditionMet.HasValue)
		{
			return;
		}

		CheckRemainingTimeAnnouncements();
	}

	public override void RoundOver()
	{
		CheckForAce();
		base.RoundOver();
	}

	protected void CheckForAce()
	{
		if (PlayerInfo.allAlive.Count != 1)
		{
			return;
		}

		var osPlayerInfo = PlayerInfo.allAlive[0] as OSPlayerInfo;
		if (!IsFullyValid(osPlayerInfo))
			return;

		if (osPlayerInfo.killsRound < 5)
			return;

		osPlayerInfo.ScoreAce();
	}

	void CheckRemainingTimeAnnouncements()
	{
		if (remainingStateTime > 60)
		{
			return;
		}

		if (!announcedOneMinRemaining)
		{
			announcedOneMinRemaining = true;
			AnnouncerSystem.QueueSound("announcer.remaining.onemin");
		}

		if (remainingStateTime > 30)
		{
			return;
		}

		if (!announcedThirtySecsRemaining)
		{
			announcedThirtySecsRemaining = true;
			AnnouncerSystem.QueueSound("announcer.remaining.thirtysecs");
		}

		if (remainingStateTime > 10)
		{
			return;
		}

		if (!announcedTenSecsRemaining)
		{
			announcedTenSecsRemaining = true;
			AnnouncerSystem.QueueSound("announcer.remaining.tensecs");
		}
	}

	public override bool ShouldShowScoreboard()
	{
		if (modeState == ModeState.PostMatchResults)
		{
			return false;
		}
		return base.ShouldShowScoreboard();
	}

	// TODO: PICK BETTER SPAWNS
	// I might do this for sensible "deathmatch" spawns in the base
	public override (Vector3 spawnPos, Rotation spawnRot) GetSpawnFor(PlayerInfo playerInfo)
	{
		return (Vector3.Zero, Rotation.Identity);
	}

	public override void CleanupRoundInstances()
	{
		base.CleanupRoundInstances();

		foreach (var spray in Game.ActiveScene.GetAllComponents<SprayRenderer>())
		{
			spray.DestroyGameObject();
		}
	}
}
