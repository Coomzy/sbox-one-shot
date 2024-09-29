
public class GMFVoice : Voice
{
	[Property] public PlayerInfo owner { get; set; }

	[Property] public List<Connection> excludedPlayers { get; set; } = new ();

	protected override IEnumerable<Connection> ExcludeFilter()
	{
		excludedPlayers.Clear();

		if (owner.isDead)
		{
			foreach (var playerInfo in PlayerInfo.allAlive)
			{
				// TODO: Still haven't verified this works on clients
				excludedPlayers.Add(playerInfo.Network.Owner);
			}
		}

		return excludedPlayers;
	}
}
