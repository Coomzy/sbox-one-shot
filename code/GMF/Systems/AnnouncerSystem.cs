using System;
using Sandbox;
using System.Linq;
using Sandbox.Network;
using Sandbox.Audio;

public class AnnouncerSystem : GameObjectSystem
{
	public static List<string> soundsToAnnounce { get; private set; } = new ();
	public static SoundHandle soundHandle { get; private set; }
	public static TimeSince lastActiveSound { get; private set; }
	public const float timeBetweenSounds = 1.0f;

	public AnnouncerSystem(Scene scene) : base(scene)
	{
		soundsToAnnounce.Clear();
		soundHandle = null;
		lastActiveSound = -10;

		Listen(Stage.FinishUpdate, 0, FinishUpdate, "Announcer.FinishUpdate");
	}

	[Authority]
	public static void QueueSound(string soundName)
	{
		soundsToAnnounce.Add(soundName);
	}

	void FinishUpdate()
	{
		if (soundHandle == null || !soundHandle.IsPlaying)
		{
			if (lastActiveSound > timeBetweenSounds)
			{				
				PlayNextSound();
			}
			return;
		}

		lastActiveSound = 0;
	}

	void PlayNextSound()
	{
		if (!soundsToAnnounce.Any())
		{
			return;
		}

		var soundName = soundsToAnnounce[0];
		soundsToAnnounce.RemoveAt(0);
		BroadcastSound(soundName);
	}

	[Broadcast]
	static void BroadcastSound(string soundName)
	{
		var mixer = Mixer.FindMixerByName("Announcer");
		soundHandle = Sound.Play(soundName, mixer);
		if (soundHandle != null)
		{
			soundHandle.SpacialBlend = 0.0f;
		}
		lastActiveSound = 0;
	}
}