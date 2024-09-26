
public class OSGameMode : GameMode, Component.INetworkListener, IHotloadManaged
{
	protected override void ActiveRoundStart()
	{
		base.ActiveRoundStart();

		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();
		spawnPoints.Shuffle();
		int playerCount = 0;
		foreach (PlayerInfo playerInfo in PlayerInfo.all)
		{
			CreatePawn(playerInfo, spawnPoints[playerCount]);
			playerCount++;
		}
	}
}
