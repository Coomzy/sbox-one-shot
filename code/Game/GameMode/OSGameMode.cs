
public class OSGameMode : GameMode, Component.INetworkListener, IHotloadManaged
{
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
}
