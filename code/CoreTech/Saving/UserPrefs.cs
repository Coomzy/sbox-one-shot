using Sandbox.Audio;

public enum VOIPMode
{
	Off,
	PushToTalk,
	Open
}

public static /*partial*/ class UserPrefs
{
	public static List<UITab> tabs { get; set; } = new();

	// Audio
	[ConVar, Change] public static float announcer_volume { get; set; } = 0.8f;

	[ConVar] public static VOIPMode voipMode { get; set; } = VOIPMode.PushToTalk;

	[StaticInitialize]
	static void Onannouncer_volumeChanged()
	{
		var mixerAnnouncer = Mixer.FindMixerByName("Announcer");
		if (mixerAnnouncer != null)
		{
			mixerAnnouncer.Volume = announcer_volume;
		}
	}

	[StaticInitialize(10)]
	static void BuildUI()
	{
		tabs = DefaultOptions();
	}

	public static List<UITab> DefaultOptions()
	{
		var tabs = new List<UITab>();

		// Tab Name - UI
		var audioTab = new UITab("Audio", 2);

		// Add Group Toggles
		//var groupToggles = tab.AddGroup("Toggles");

		//groupToggles.AddToggle("Mute Music", () => muteMusic, (value) => muteMusic = value);


		var groupVolumes = audioTab.AddGroup("Volume");
		groupVolumes.AddSlider("Announcer Volume", () => announcer_volume, (value) => announcer_volume = value, step: 0.05f);

		var groupVOIP = audioTab.AddGroup("VOIP");
		groupVOIP.AddCycler("VOIP Mode", () => voipMode, (value) => voipMode = value);

		tabs.Add(audioTab);

		return tabs;
	}
}