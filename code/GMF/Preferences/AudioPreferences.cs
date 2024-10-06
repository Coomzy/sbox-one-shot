using Sandbox.Audio;

public enum VOIPMode
{
	Off,
	PushToTalk,
	Open
}

public class AudioPreferences : EasySave<AudioPreferences>
{
	public bool muteMusic { get; set; }

	public float gameVolume { get; set; }
	public float musicVolume { get; set; }
	public float uiVolume { get; set; }
	public float announcerVolume { get; set; }

	public VOIPMode voipMode { get; set; }

	protected override void SetDefaultValues()
	{
		gameVolume = 1.0f;
		musicVolume = 0.35f;
		uiVolume = 0.8f;
		announcerVolume = 0.8f;
		voipMode = VOIPMode.PushToTalk;
	}

	protected override void OnLoad()
	{		
		ApplyVolumesToMixers();
	}

	public void ApplyVolumesToMixers()
	{
		var mixerGame = Mixer.FindMixerByName("Game");
		var mixerMusic = Mixer.FindMixerByName("Music");
		var mixerUI = Mixer.FindMixerByName("UI");
		var mixerAnnouncer = Mixer.FindMixerByName("Announcer");

		if (mixerGame != null)
		{
			mixerGame.Volume = gameVolume;
		}
		if (mixerMusic != null)
		{
			mixerMusic.Volume = muteMusic ? 0.0f : musicVolume;
		}
		if (mixerUI != null)
		{
			mixerUI.Volume = uiVolume;
		}
		if (mixerAnnouncer != null)
		{
			mixerAnnouncer.Volume = announcerVolume;
		}
	}

	public void MuteMusic(bool mute)
	{
		SetMuteMusic(mute);
		Save();
	}

	public void ToggleMusic()
	{
		MuteMusic(!muteMusic);
	}

	void SetMuteMusic(bool value)
	{
		muteMusic = value;
		ApplyVolumesToMixers();
		Save();
	}

	void SetGameVolumeValue(float value)
	{
		gameVolume = value;
		ApplyVolumesToMixers();
		Save();
	}

	void SetMusicVolumeValue(float value)
	{
		musicVolume = value;
		ApplyVolumesToMixers();
		Save();
	}

	void SetUIVolumeValue(float value)
	{
		uiVolume = value;
		ApplyVolumesToMixers();
		Save();
	}

	void SetAnnouncerVolumeValue(float value)
	{
		announcerVolume = value;
		ApplyVolumesToMixers();
		Save();
	}

	protected override UITab OnBuildUI()
	{
		// Tab Name - UI
		var tab = new UITab("Audio", 2);

		// Add Group Toggles
		//var groupToggles = tab.AddGroup("Toggles");

		//groupToggles.AddToggle("Mute Music", () => muteMusic, (value) => muteMusic = value);

		// Add Group Volumes
		var groupVolumes = tab.AddGroup("Volumes");

		groupVolumes.AddSlider("Game Volume", () => gameVolume, SetGameVolumeValue, step: 0.05f);
		//groupVolumes.AddSlider("Music Volume", () => musicVolume, SetMusicVolumeValue, step: 0.05f);
		groupVolumes.AddSlider("UI Volume", () => uiVolume, SetUIVolumeValue, step: 0.05f);
		groupVolumes.AddSlider("Announcer Volume", () => announcerVolume, SetAnnouncerVolumeValue, step: 0.05f);

		// Add Group VOIP
		var groupVOIP = tab.AddGroup("VOIP");

		// Add Cycle Selection 'VOIP'
		groupVOIP.AddCycler("VOIP Mode", () => voipMode, (value) => voipMode = value);

		return tab;
	}
}