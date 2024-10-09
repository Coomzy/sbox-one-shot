
using System.Text.Json.Serialization;

[Group("GMF")]
public class GMFVoice : Voice, IGameModeEvents
{
	public static GMFVoice local { get; private set; }

	[Group("Runtime"), Order(100), Property, Sync, Change("OnRep_isGlobalVoice")] public bool isGlobalVoice { get; set; } = true;

	[Group("Setup"), Order(-100), Property] public PlayerInfo owner { get; set; }
	[Group("Runtime"), Order(110), Property] public List<Connection> excludedPlayers { get; set; } = new();

	[Group("Runtime"), Order(110), Property, JsonIgnore] public bool isListening => IsListening;

	[ConVar] public static bool debug_voice { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

		if (!IsProxy)
		{
			local = this;
		}

		OnRep_isGlobalVoice();
	}

	protected override void OnDestroy()
	{
		if (local == this)
		{
			local = null;
		}
		base.OnDestroy();
	}

	void OnRep_isGlobalVoice()
	{
		// TODO: The voice stuff seems broken and I can't be bothered try and work around it because testing is so fucking annoying
		//WorldspacePlayback = !isGlobalVoice;
		WorldspacePlayback = false;
		Volume = WorldspacePlayback ? 3.0f : 1.0f;
	}

	public void ModeStateChange(ModeState oldValue, ModeState newValue)
	{
		if (IsProxy)
			return;

		isGlobalVoice = newValue != ModeState.ActiveRound;
	}

	public void Update()
	{		
		Renderer = owner?.character?.body?.bodyRenderer;
		if (IsFullyValid(owner?.character?.body?.bodyRenderer))
		{
			WorldPosition = owner.character.body.bodyRenderer.WorldPosition;
			WorldRotation = owner.character.body.bodyRenderer.WorldRotation;
		}

		if (IsProxy)
		{
			Mode = ActivateMode.Manual;
		}
		else
		{
			Mode = VOIPModeToActivateMode(UserPrefs.voipMode);
		}

		if (debug_voice)
		{
			Debuggin.ToScreen($"{owner?.displayName} Voice Mode: {Mode}");
			Debuggin.ToScreen($"isGlobalVoice: {isGlobalVoice}, WorldspacePlayback: {WorldspacePlayback}");
			Debuggin.ToScreen($"IsRecording: {IsRecording}");
			Debuggin.ToScreen($"LastPlayed: {LastPlayed.Relative}");
			Debuggin.ToScreen($"LaughterScore: {LaughterScore}");
			Debuggin.ToScreen($"Renderer: {Renderer}");
			Debuggin.ToScreen($"Excluded Players");
			foreach (var playerInfo in excludedPlayers)
			{
				Debuggin.ToScreen($"player: {playerInfo?.DisplayName}");
			}
			Debuggin.ToScreen($"WorldPosition: {WorldPosition}");
			Debuggin.ToScreen($"");
		}
	}

	protected override IEnumerable<Connection> ExcludeFilter()
	{
		excludedPlayers.Clear();
		if (!IsFullyValid(owner))
		{
			return base.ExcludeFilter();
		}

		foreach (var playerInfo in PlayerInfo.allActive)
		{
			if (!IsFullyValid(playerInfo))
				continue;

			if (isGlobalVoice)
			{
				if (owner.CanHearVoiceGlobal(playerInfo))
					continue;
			}
			else
			{
				if (owner.CanHearVoiceProximity(playerInfo))
					continue;
			}

			excludedPlayers.Add(playerInfo.connection);
		}

		return excludedPlayers;
	}

	public Voice.ActivateMode VOIPModeToActivateMode(VOIPMode mode)
	{
		switch (mode)
		{
			case VOIPMode.PushToTalk:
				return ActivateMode.PushToTalk;
			case VOIPMode.Open:
				return ActivateMode.AlwaysOn;
		}
		return ActivateMode.Manual;
	}
}