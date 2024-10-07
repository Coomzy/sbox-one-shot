using Sandbox;
using Sandbox.Diagnostics;

[Group("GMF")]
public class GMFVoice : Component
{
	[Group("Setup"), Property] public GMFVoiceProxy proxy { get; set; }
	[Group("Runtime"), Property, Sync] public PlayerInfo owner { get; set; }
	[Group("Runtime"), Property, Sync, Change("OnRep_worldSpacePlayback")] public bool worldSpacePlayback { get; set; }

	[Group("Runtime"), Property] public List<Connection> excludedPlayers { get; set; } = new ();

	protected override void OnAwake()
	{
		Assert.NotNull(proxy, "You need a proxy");
	}

	protected void OnRep_worldSpacePlayback()
	{
		proxy.WorldspacePlayback = worldSpacePlayback;
	}

	protected override void OnUpdate()
	{
		if (!IsFullyValid(owner, proxy))
			return;

		UpdateVoice();

		if (IsProxy)
			return;

		excludedPlayers.Clear();

		if (GameMode.instance.modeState == ModeState.ActiveRound)
		{
			if (owner.isDead)
			{
				foreach (var playerInfo in PlayerInfo.allAlive)
				{
					// TODO: Still haven't verified this works on clients
					excludedPlayers.Add(playerInfo.connection);
					//Debuggin.ToScreen($"'{playerInfo.displayName}' owner: {playerInfo.Network.Owner}");
				}
			}
		}
	}

	protected virtual void UpdateVoice()
	{
		Debuggin.ToScreen($"'{owner?.displayName}' body: {owner?.character?.body}");
		if (IsFullyValid(owner?.character?.body))
		{
			proxy.Renderer = owner.character.body.bodyRenderer;
			WorldPosition = owner.character.body.voipSocket.WorldPosition;
			WorldRotation = owner.character.body.voipSocket.WorldRotation;
		}
		else
		{
			proxy.Renderer = null;
		}

		if (IsProxy)
		{
			proxy.Mode = Voice.ActivateMode.Manual;
			return;
		}

		proxy.Mode = AudioPreferences.instance.voipMode == VOIPMode.PushToTalk ? Voice.ActivateMode.PushToTalk : Voice.ActivateMode.AlwaysOn;
	}
}
