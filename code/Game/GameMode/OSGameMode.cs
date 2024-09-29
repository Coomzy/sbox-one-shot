
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

	// TODO: PICK BETTER SPAWNS
	// I might do this for sensible "deathmatch" spawns in the base
	public override (Vector3 spawnPos, Rotation spawnRot) GetSpawnFor(PlayerInfo playerInfo)
	{
		return (Vector3.Zero, Rotation.Identity);
	}
}
