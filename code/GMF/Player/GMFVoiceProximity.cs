using System.Text.Json.Serialization;

[Group("GMF")]
public class GMFVoiceProximity : Voice
{
	public static GMFVoiceProximity local { get; private set; }

	[Group("Setup"), Order(-100), Property] public CharacterBody body { get; set; }
	[Group("Runtime"), Order(100), Property] public List<Connection> excludedPlayers { get; set; } = new();

	[Group("Runtime"), Order(100), Property, JsonIgnore] public bool isListening => IsListening;

	protected override void OnStart()
	{
		base.OnStart();

		if (!IsProxy)
		{
			local = this;
		}
	}

	protected override void OnDestroy()
	{
		if (local == this)
		{
			local = null;
		}
		base.OnDestroy();
	}

	protected override IEnumerable<Connection> ExcludeFilter()
	{
		excludedPlayers.Clear();
		if (!IsFullyValid(body?.owner?.owner))
		{
			return base.ExcludeFilter();
		}

		foreach (var playerInfo in PlayerInfo.allActive)
		{
			if (!IsFullyValid(playerInfo))
				continue;

			if (body.owner.owner.CanHearVoiceProximity(playerInfo))
				continue;

			excludedPlayers.Add(playerInfo.connection);
		}

		return excludedPlayers;
	}
}