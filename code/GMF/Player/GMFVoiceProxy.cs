
public class GMFVoiceProxy : Voice
{
	[Property] public GMFVoice owner { get; set; }

	protected override IEnumerable<Connection> ExcludeFilter()
	{
		if (!IsFullyValid(owner))
		{
			return base.ExcludeFilter();
		}

		return owner.excludedPlayers;
	}
}
