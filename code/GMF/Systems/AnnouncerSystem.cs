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

	public static void QueueSound(string soundName)
	{
		soundsToAnnounce.Add(soundName);
	}

	public static void QueueOverrideSound(string soundName)
	{
		soundsToAnnounce.Clear();
		if (IsFullyValid(soundHandle))
		{
			soundHandle.Stop();
		}
		soundsToAnnounce.Add(soundName);
	}

	[Broadcast]
	public static void BroadcastQueueSound(string soundName)
	{
		QueueSound(soundName);
	}

	[Broadcast]
	public static void BroadcastQueueOverrideSound(string soundName)
	{
		QueueOverrideSound(soundName);
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
		PlaySound(soundName);
	}

	static void PlaySound(string soundName)
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