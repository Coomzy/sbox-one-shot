using Sandbox.Audio;
using System;

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

	public VOIPMode voipMode { get; set; }

	protected override void SetDefaultValues()
	{
		gameVolume = 1.0f;
		musicVolume = 0.35f;
		uiVolume = 0.8f;
		voipMode = VOIPMode.PushToTalk;
	}

	public void ApplyVolumesToMixers()
	{
		var mixerGame = Mixer.FindMixerByName("Game");
		var mixerMusic = Mixer.FindMixerByName("Music");
		var mixerUI = Mixer.FindMixerByName("UI");

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
	}

	void SetGameVolumeValue(float value)
	{
		gameVolume = value;
		ApplyVolumesToMixers();
	}

	void SetMusicVolumeValue(float value)
	{
		musicVolume = value;
		ApplyVolumesToMixers();
	}

	void SetUIVolumeValue(float value)
	{
		uiVolume = value;
		ApplyVolumesToMixers();
	}

	protected override UITab OnBuildUI()
	{
		// Tab Name - UI
		var tab = new UITab("Audio", 2);

		// Add Group Toggles
		var groupToggles = tab.AddGroup("Toggles");

		// Add Toggle 'Mute Music'
		groupToggles.AddToggle("Mute Music", () => muteMusic, (value) => muteMusic = value);

		// Add Group Volumes
		var groupVolumes = tab.AddGroup("Volumes");

		// Add Slider 'Game Volume'
		groupVolumes.AddSlider("Game Volume", () => gameVolume, SetGameVolumeValue);
		// Add Slider 'Music Volume'
		groupVolumes.AddSlider("Music Volume", () => musicVolume, SetMusicVolumeValue);
		// Add Slider 'UI Volume'
		groupVolumes.AddSlider("UI Volume", () => uiVolume, SetUIVolumeValue);

		// Add Group VOIP
		var groupVOIP = tab.AddGroup("VOIP");

		// Add Cycle Selection 'VOIP'
		groupVOIP.AddCycler("VOIP Mode", () => voipMode, (value) => voipMode = value);

		return tab;
	}
}